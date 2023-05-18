using UnityEngine;
using Zavala.Sim;
using Zavala;
using BeauUtil;
using BeauUtil.Debugger;
using Unity.Collections;
using BeauRoutine;
using System.Collections;

public class PhosphorusSimTest : MonoBehaviour {
    public uint Width;
    public uint Height;
    public GameObject TilePrefab;
    public GameObject FlowPrefab;
    public Transform CameraArm;

    [Header("Phosphorus")]
    public Material PhosphorusMaterial;
    public Mesh PhosphorusMesh;

    private HexGridSize m_HexSize;
    private HexGridWorldSpace m_WorldSpace;
    private unsafe SimBuffer<PhosphorusTileInfo> m_TileInfo;
    private unsafe SimBuffer<PhosphorusTileState>[] m_TileStates;
    private unsafe int m_TileStateBufferIndex;

    private Unsafe.ArenaHandle m_ArenaHandle;
    private Routine m_TickRoutine;
    private float m_AddCooldown;

    private void Start() {
        m_HexSize = new HexGridSize(Width, Height);
        m_ArenaHandle = Unsafe.CreateArena(64 * 1024);

        m_WorldSpace = new HexGridWorldSpace(m_HexSize, Vector3.one, default(Vector3));

        InitializeBuffers();

        m_TickRoutine.Replace(this, TickLoop());
    }

    private unsafe void InitializeBuffers() {
        m_TileInfo = SimBuffer<PhosphorusTileInfo>.Create(m_ArenaHandle, m_HexSize);
        m_TileStates = new SimBuffer<PhosphorusTileState>[2];
        m_TileStates[0] = SimBuffer<PhosphorusTileState>.Create(m_ArenaHandle, m_HexSize);
        m_TileStates[1] = SimBuffer<PhosphorusTileState>.Create(m_ArenaHandle, m_HexSize);

        float offset = RNG.Instance.NextFloat(500);

        SimBuffer<PhosphorusTileState> currentBuffer = m_TileStates[0];

        for(int i = 0; i < m_HexSize.Size; i++) {
            HexVector pos = m_HexSize.FastIndexToPos(i);
            m_TileInfo[i] = new PhosphorusTileInfo() {
                Height = (ushort) ((int) ((10 + 1000 * Mathf.PerlinNoise(offset + pos.X * 0.23f, offset * 0.6f + pos.Y * 0.19f) + RNG.Instance.Next(15, 100)) / 50) * 50),
                RegionId = Tile.InvalidIndex16,
                Flags = 0
            };
            currentBuffer[i] = new PhosphorusTileState() {
                Count = RNG.Instance.Chance(0.2f) ? (ushort) RNG.Instance.Next(1, 3) : (ushort) 0
            };
            Log.Msg("Generated tile {0}[{1},{2}] with height {3}", i, pos.X, pos.Y, m_TileInfo[i].Height);
        }

        using(Profiling.Time("generating flow field")) {
            PhosphorusSim.EvaluateFlowField(m_TileInfo, m_HexSize);
        }

        for(int i = 0; i < m_HexSize.Size; i++) {
            HexVector pos = m_HexSize.FastIndexToPos(i);

            GameObject go = Instantiate(TilePrefab, HexToWorld(pos, m_TileInfo[i].Height), Quaternion.identity);
            PhosphorusTileInfo tileInfo = m_TileInfo[i];
            if (!tileInfo.FlowMask.IsEmpty) {
                Log.Msg("Tile {0}[{1},{2}] has {3} flow vectors", i, pos.X, pos.Y, tileInfo.FlowMask.Count);
                Vector3 flowStartPos = go.transform.position + Vector3.up;
                foreach(var flow in tileInfo.FlowMask) {
                    int targetIdx = m_HexSize.OffsetIndexFrom(i, flow);
                    HexVector targetPos = m_HexSize.FastIndexToPos(targetIdx);
                    Vector3 flowEndPos = HexToWorld(targetPos, m_TileInfo[targetIdx].Height) + Vector3.up;
                }
            }
        }
    }

    private IEnumerator TickLoop() {
        int tickCount = 0;
        while(true) {
            yield return 1;
            Tick();
            tickCount++;
        }
    }

    private unsafe void RandomPhosphorusDrop() {
        SimBuffer<PhosphorusTileState> stateBuffer = m_TileStates[m_TileStateBufferIndex];
        for(int i = 0; i < m_HexSize.Size; i++) {
            if (RNG.Instance.Chance(0.1f)) {
                stateBuffer[i].Count += (ushort) RNG.Instance.Next(16, 24);
            }
        }
    }

    private unsafe void Tick() {
        // double buffering yeag
        using(Profiling.Time("ticking phosphorus simulation")) {
            PhosphorusSim.Tick(m_TileInfo, m_TileStates[m_TileStateBufferIndex], m_TileStates[1 - m_TileStateBufferIndex], m_HexSize, RNG.Instance);
        }
        m_TileStateBufferIndex = 1 - m_TileStateBufferIndex;
    }

    private void LateUpdate() {
        RenderPhosphorus();

        float rotate = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) {
            rotate -= 1;
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            rotate += 1;
        }

        if (Input.GetMouseButton(0)) {
            if (m_AddCooldown > 0) {
                m_AddCooldown -= Time.deltaTime;
            } else {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = 1;
                Ray ray = Camera.main.ScreenPointToRay(mousePos, Camera.MonoOrStereoscopicEye.Mono);
                if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
                    Vector3 center = hitInfo.collider.bounds.center;
                    HexVector vec = WorldToHex(center);
                    if (m_HexSize.IsValidPos(vec)) {
                        unsafe {
                            SimBuffer<PhosphorusTileState> stateBuffer = m_TileStates[m_TileStateBufferIndex];
                            stateBuffer[m_HexSize.FastPosToIndex(vec)].Count += 8;
                        }
                    }
                    m_AddCooldown = 2f / 60;
                }
            }
        } else {
            m_AddCooldown = 0;
        }

        if (Input.GetMouseButtonDown(1)) {
            RandomPhosphorusDrop();
        }

        CameraArm.Rotate(0, rotate * 45 * Time.deltaTime, 0, Space.World);
    }

    private unsafe void RenderPhosphorus() {
        SimBuffer<PhosphorusTileState> stateBuffer = m_TileStates[m_TileStateBufferIndex];
        DefaultInstancingParams* matrixBuffer = stackalloc DefaultInstancingParams[(int) m_HexSize.Size];

        RenderParams phosphorusRenderParams = new RenderParams(PhosphorusMaterial);
        phosphorusRenderParams.camera = Camera.main;
        using(var instanceHelper = new InstancingHelper<DefaultInstancingParams>(matrixBuffer, (int) m_HexSize.Size, phosphorusRenderParams, PhosphorusMesh)) {
            for(int i = 0; i < m_HexSize.Size; i++) {
                PhosphorusTileState state = stateBuffer[i];
                if (state.Count == 0) {
                    continue;
                }

                Vector3 pos = HexToWorld(m_HexSize.FastIndexToPos(i), m_TileInfo[i].Height) + (Vector3.up * 0.5f);
                float size = 0.08f * Mathf.Sqrt(state.Count);
                instanceHelper.Queue(new DefaultInstancingParams() {
                    objectToWorld = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * size)
                });
            }

            instanceHelper.Submit();
        }
    }

    private void OnDestroy() {
        Unsafe.TryDestroyArena(ref m_ArenaHandle);
    }

    private Vector3 HexToWorld(HexVector vec, float height) {
        return vec.ToWorld(height / 200f, m_WorldSpace);
    }

    private HexVector WorldToHex(Vector3 vec) {
        return HexVector.FromWorld(vec, m_WorldSpace);
    }
}