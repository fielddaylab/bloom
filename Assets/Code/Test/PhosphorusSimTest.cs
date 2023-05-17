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
    private unsafe PhosphorusTileInfo* m_TileInfo;
    private unsafe PhosphorusTileState*[] m_TileStates;
    private unsafe int m_TileStateBufferIndex;

    private Unsafe.ArenaHandle m_ArenaHandle;
    private Routine m_TickRoutine;

    private void Start() {
        m_HexSize = new HexGridSize(Width, Height);
        m_ArenaHandle = Unsafe.CreateArena(64 * 1024);

        InitializeBuffers();

        m_TickRoutine.Replace(this, TickLoop());
    }

    private unsafe void InitializeBuffers() {
        m_TileInfo = Unsafe.AllocArray<PhosphorusTileInfo>((int) m_HexSize.Size);
        m_TileStates = new PhosphorusTileState*[2];
        m_TileStates[0] = Unsafe.AllocArray<PhosphorusTileState>((int) m_HexSize.Size);
        m_TileStates[1] = Unsafe.AllocArray<PhosphorusTileState>((int) m_HexSize.Size);

        float offset = RNG.Instance.NextFloat(500);

        PhosphorusTileState* currentBuffer = m_TileStates[0];

        for(int i = 0; i < m_HexSize.Size; i++) {
            HexVector pos = m_HexSize.FastIndexToPos(i);
            m_TileInfo[i] = new PhosphorusTileInfo() {
                Height = (ushort) (10 + 300 * Mathf.PerlinNoise(offset + pos.X * 0.45f, offset * 0.6f + pos.Y * 0.39f)),
                RegionId = Tile.InvalidIndex16,
                Flags = 0
            };
            currentBuffer[i] = new PhosphorusTileState() {
                Count = (ushort) RNG.Instance.Next(0, 3)
            };
            Log.Msg("Generated tile {0}[{1},{2}] with height {3}", i, pos.X, pos.Y, m_TileInfo[i].Height);
        }

        PhosphorusSim.EvaluateFlowField(m_TileInfo, (int) m_HexSize.Size, m_HexSize);

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
        PhosphorusTileState* stateBuffer = m_TileStates[m_TileStateBufferIndex];
        for(int i = 0; i < m_HexSize.Size; i++) {
            if (RNG.Instance.Chance(0.3f)) {
                stateBuffer[i].Count += (ushort) RNG.Instance.Next(4, 6);
            }
        }
    }

    private unsafe void Tick() {
        // double buffering yeag
        PhosphorusSim.Tick(m_TileInfo, m_TileStates[m_TileStateBufferIndex], m_TileStates[1 - m_TileStateBufferIndex], (int) m_HexSize.Size, m_HexSize, RNG.Instance);
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

        if (Input.GetMouseButtonDown(0)) {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 1;
            Ray ray = Camera.main.ScreenPointToRay(mousePos, Camera.MonoOrStereoscopicEye.Mono);
            if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
                Vector3 center = hitInfo.collider.bounds.center;
                HexVector vec = WorldToHex(center);
                if (m_HexSize.IsValidPos(vec)) {
                    unsafe {
                        PhosphorusTileState* stateBuffer = m_TileStates[m_TileStateBufferIndex];
                        stateBuffer[m_HexSize.FastPosToIndex(vec)].Count += 8;
                    }
                }
            }
        }

        CameraArm.Rotate(0, rotate * 45 * Time.deltaTime, 0, Space.World);
    }

    private unsafe void RenderPhosphorus() {
        PhosphorusTileState* stateBuffer = m_TileStates[m_TileStateBufferIndex];
        Matrix4x4* matrixBuffer = stackalloc Matrix4x4[(int) m_HexSize.Size];
        using(var instanceHelper = new InstancingHelper<Matrix4x4>(matrixBuffer, (int) m_HexSize.Size)) {
            RenderParams phosphorusRenderParams = new RenderParams(PhosphorusMaterial);
            phosphorusRenderParams.camera = Camera.main;
            for(int i = 0; i < m_HexSize.Size; i++) {
                PhosphorusTileState state = stateBuffer[i];
                if (state.Count == 0) {
                    continue;
                }

                Vector3 pos = HexToWorld(m_HexSize.FastIndexToPos(i), m_TileInfo[i].Height) + (Vector3.up * 0.5f);
                float size = 0.1f * Mathf.Sqrt(state.Count);
                instanceHelper.Queue(Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * size));
            }

            instanceHelper.Submit(phosphorusRenderParams, PhosphorusMesh);
        }
    }

    private void OnDestroy() {
        Unsafe.TryDestroyArena(ref m_ArenaHandle);
    }

    private Vector3 HexToWorld(HexVector vec, float height) {
        return vec.ToWorld(height / 200f, m_HexSize, 1f);
    }

    private HexVector WorldToHex(Vector3 vec) {
        return HexVector.FromWorld(vec, m_HexSize, 1);
    }
}