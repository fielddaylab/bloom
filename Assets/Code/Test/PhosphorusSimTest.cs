//using UnityEngine;
//using Zavala.Sim;
//using Zavala;
//using BeauUtil;
//using BeauUtil.Debugger;
//using Unity.Collections;
//using BeauRoutine;
//using System.Collections;
//using FieldDay;
//// using UnityEngine.Rendering.Universal;

//public class PhosphorusSimTest : MonoBehaviour {
//    public uint Width;
//    public uint Height;
//    public GameObject TilePrefab;
//    public GameObject FlowPrefab;
//    public Transform CameraArm;

//    [Header("Phosphorus")]
//    public Material PhosphorusMaterial;
//    public Mesh PhosphorusMesh;

//    private HexGridSize m_HexSize;
//    private HexGridWorldSpace m_WorldSpace;
//    private unsafe SimBuffer<ushort> m_TileHeights;
//    private unsafe SimBuffer<PhosphorusTileInfo> m_TileInfo;
//    private unsafe SimBuffer<PhosphorusTileState>[] m_TileStates;
//    private unsafe int m_TileStateBufferIndex;
//    private PhosphorusChangeBuffer m_PhosphorusChangeBuffer;
//    private PhosphorusRenderState[] m_PhosphorusRendering;

//    private Unsafe.ArenaHandle m_ArenaHandle;
//    private Routine m_TickRoutine;
//    private float m_AddCooldown;

//    private void Start() {
//        m_HexSize = new HexGridSize(Width, Height);
//        m_ArenaHandle = Unsafe.CreateArena(64 * 1024);

//        m_WorldSpace = new HexGridWorldSpace(m_HexSize, Vector3.one, default(Vector3));

//        InitializeBuffers();
//        Frame.CreateAllocator();

//        m_TickRoutine.Replace(this, TickLoop());

//        Application.targetFrameRate = 60;
//    }

//    private unsafe void InitializeBuffers() {
//        m_TileInfo = SimBuffer.Create<PhosphorusTileInfo>(m_ArenaHandle, m_HexSize);
//        m_TileHeights = SimBuffer.Create<ushort>(m_ArenaHandle, m_HexSize);
//        m_TileStates = new SimBuffer<PhosphorusTileState>[2];
//        m_TileStates[0] = SimBuffer.Create<PhosphorusTileState>(m_ArenaHandle, m_HexSize);
//        m_TileStates[1] = SimBuffer.Create<PhosphorusTileState>(m_ArenaHandle, m_HexSize);

//        float offset = RNG.Instance.NextFloat(500);

//        m_PhosphorusChangeBuffer.Add = new RingBuffer<PhosphorusTileAddRemove>(8, RingBufferMode.Expand);
//        m_PhosphorusChangeBuffer.Remove = new RingBuffer<PhosphorusTileAddRemove>(8, RingBufferMode.Expand);
//        m_PhosphorusChangeBuffer.Transfers = new RingBuffer<PhosphorusTileTransfer>(64, RingBufferMode.Expand);

//        //m_PhosphorusRendering.StationaryInstances = new RingBuffer<PhosphorusRenderInstance>(256, RingBufferMode.Expand);
//        //m_PhosphorusRendering.AnimatingInstanes = new RingBuffer<PhosphorusRenderInstance>(64, RingBufferMode.Expand);

//        SimBuffer<PhosphorusTileState> currentBuffer = m_TileStates[0];

//        for(int i = 0; i < m_HexSize.Size; i++) {
//            HexVector pos = m_HexSize.FastIndexToPos(i);
//            ushort height = (ushort) ((int) ((10 + 1000 * Mathf.PerlinNoise(offset + pos.X * 0.23f, offset * 0.6f + pos.Y * 0.19f) + RNG.Instance.Next(15, 100)) / 50) * 50);
//            m_TileHeights[i] = height;
//            m_TileInfo[i] = new PhosphorusTileInfo() {
//                Height = height,
//                RegionIndex = Tile.InvalidIndex16,
//                Flags = 0
//            };
//            currentBuffer[i] = new PhosphorusTileState() {
//                Count = RNG.Instance.Chance(0.2f) ? (ushort) RNG.Instance.Next(2, 25) : (ushort) 0
//            };
//            m_PhosphorusChangeBuffer.Add.PushBack(new PhosphorusTileAddRemove() {
//                TileIdx = i,
//                Amount = currentBuffer[i].Count
//            });
//            // Log.Msg("Generated tile {0}[{1},{2}] with height {3}", i, pos.X, pos.Y, m_TileInfo[i].Height);
//        }

//        using(Profiling.Time("generating flow field")) {
//            PhosphorusSim.EvaluateFlowField(m_TileInfo, m_HexSize);
//        }

//        for(int i = 0; i < m_HexSize.Size; i++) {
//            HexVector pos = m_HexSize.FastIndexToPos(i);

//            GameObject go = Instantiate(TilePrefab, HexToWorld(pos, m_TileInfo[i].Height), Quaternion.identity);
//            PhosphorusTileInfo tileInfo = m_TileInfo[i];
//            if (!tileInfo.FlowMask.IsEmpty) {
//                // Log.Msg("Tile {0}[{1},{2}] has {3} flow vectors", i, pos.X, pos.Y, tileInfo.FlowMask.Count);
//                // Vector3 flowStartPos = go.transform.position + Vector3.up;
//                // foreach(var flow in tileInfo.FlowMask) {
//                //     int targetIdx = m_HexSize.OffsetIndexFrom(i, flow);
//                //     HexVector targetPos = m_HexSize.FastIndexToPos(targetIdx);
//                //     Vector3 flowEndPos = HexToWorld(targetPos, m_TileInfo[targetIdx].Height) + Vector3.up;
//                // }
//            }
//        }
//    }

//    private IEnumerator TickLoop() {
//        int tickCount = 0;
//        while(true) {
//            yield return 1;
//            Tick();
//            tickCount++;
//        }
//    }

//    private unsafe void RandomPhosphorusDrop() {
//        SimBuffer<PhosphorusTileState> stateBuffer = m_TileStates[m_TileStateBufferIndex];
//        for(int i = 0; i < m_HexSize.Size; i++) {
//            if (RNG.Instance.Chance(0.1f)) {
//                int toAdd = RNG.Instance.Next(16, 24);
//                stateBuffer[i].Count += (ushort) toAdd;
//                if (toAdd > 0) {
//                    m_PhosphorusChangeBuffer.Add.PushBack(new PhosphorusTileAddRemove() {
//                        TileIdx = i,
//                        RegionIndex = m_TileInfo[i].RegionIndex,
//                        Amount = (ushort) toAdd
//                    });
//                }
//            }
//        }
//    }

//    private unsafe void Tick() {
//        // double buffering yeag
//        using(Profiling.Time("ticking phosphorus simulation")) {
//            PhosphorusSim.Tick(m_TileInfo, m_TileStates[m_TileStateBufferIndex], m_TileStates[1 - m_TileStateBufferIndex], m_HexSize, RNG.Instance, m_PhosphorusChangeBuffer);
//        }
//        m_TileStateBufferIndex = 1 - m_TileStateBufferIndex;
//    }

//    private void LateUpdate() {
//        Frame.IncrementFrame();
//        Frame.ResetAllocator();

//        float rotate = 0;
//        if (Input.GetKey(KeyCode.LeftArrow)) {
//            rotate -= 1;
//        }
//        if (Input.GetKey(KeyCode.RightArrow)) {
//            rotate += 1;
//        }

//        if (Input.GetMouseButton(0)) {
//            if (m_AddCooldown > 0) {
//                m_AddCooldown -= Time.deltaTime;
//            } else {
//                Vector3 mousePos = Input.mousePosition;
//                mousePos.z = 1;
//                Ray ray = Camera.main.ScreenPointToRay(mousePos, Camera.MonoOrStereoscopicEye.Mono);
//                if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
//                    Vector3 center = hitInfo.collider.bounds.center;
//                    HexVector vec = WorldToHex(center);
//                    if (m_HexSize.IsValidPos(vec)) {
//                        unsafe {
//                            SimBuffer<PhosphorusTileState> stateBuffer = m_TileStates[m_TileStateBufferIndex];
//                            stateBuffer[m_HexSize.FastPosToIndex(vec)].Count += 2;
//                            m_PhosphorusChangeBuffer.Add.PushBack(new PhosphorusTileAddRemove() {
//                                TileIdx = m_HexSize.FastPosToIndex(vec),
//                                RegionIndex = m_TileInfo[m_HexSize.FastPosToIndex(vec)].RegionIndex,
//                                Amount = 2
//                            });
//                        }
//                    }
//                    m_AddCooldown = 2f / 60;
//                }
//            }
//        } else {
//            m_AddCooldown = 0;
//        }

//        if (Input.GetMouseButtonDown(1)) {
//            RandomPhosphorusDrop();
//        }

//        CameraArm.Rotate(0, rotate * -45 * Time.deltaTime, 0, Space.World);

//        //ProcessPhosphorusMovement(Time.deltaTime);
//        //ProcessPhosphorusChanges();
//        //RenderPhosphorus();
//    }

//    private unsafe void ProcessPhosphorusChanges() {
//        PhosphorusRendering.PrepareChangeBuffer(m_PhosphorusChangeBuffer);
//        //PhosphorusRendering.ProcessChanges(m_PhosphorusRendering, m_PhosphorusChangeBuffer, m_TileHeights, m_WorldSpace, RandomPosOnTile, RNG.Instance, Frame.Index8);

//        // clear buffers
//        m_PhosphorusChangeBuffer.Clear();
//    }

//    //private void ProcessPhosphorusMovement(float deltaTime) {
//    //    PhosphorusRendering.ProcessMovement(m_PhosphorusRendering, deltaTime, 0.8f, 0.0025f);
//    //}

//    static private Vector3 RandomPosOnTile(int tileIdx, ushort height, in HexGridWorldSpace worldSpace) {
//        Vector3 pos = HexVector.ToWorld(tileIdx, height / 200f, worldSpace);
//        pos.x += RNG.Instance.NextFloat(-0.5f, 0.5f) * worldSpace.Scale.x;
//        pos.z += RNG.Instance.NextFloat(-0.5f, 0.5f) * worldSpace.Scale.z;
//        pos.y += 0.5f;
//        return pos;
//    }

//    //private unsafe void RenderPhosphorus() {
//    //    DefaultInstancingParams* matrixBuffer = stackalloc DefaultInstancingParams[512];

//    //    PhosphorusRenderState stateBuffer = m_PhosphorusRendering;
//    //    RenderParams phosphorusRenderParams = new RenderParams(PhosphorusMaterial);
//    //    phosphorusRenderParams.camera = Camera.main;
//    //    using(var instanceHelper = new InstancingHelper<DefaultInstancingParams>(matrixBuffer, 512, phosphorusRenderParams, PhosphorusMesh)) {
//    //        //Log.Msg("rendering {0} static phosphorus pips", stateBuffer.StationaryInstances.Count);
//    //        foreach(var renderInst in stateBuffer.StationaryInstances) {
//    //            Vector3 pos = renderInst.Position;
//    //            float size = 0.1f;
//    //            instanceHelper.Queue(new DefaultInstancingParams() {
//    //                objectToWorld = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * size)
//    //            });
//    //        }

//    //        //Log.Msg("rendering {0} animating phosphorus pips", stateBuffer.AnimatingInstances.Count);
//    //        foreach(var renderInst in stateBuffer.AnimatingInstances) {
//    //            Vector3 pos = renderInst.Position;
//    //            float size = 0.1f;
//    //            instanceHelper.Queue(new DefaultInstancingParams() {
//    //                objectToWorld = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * size)
//    //            });
//    //        }

//    //        instanceHelper.Submit();
//    //    }
//    //}

//    private void OnDestroy() {
//        Unsafe.TryDestroyArena(ref m_ArenaHandle);
//    }

//    private Vector3 HexToWorld(HexVector vec, float height) {
//        return vec.ToWorld(height / 200f, m_WorldSpace);
//    }

//    private HexVector WorldToHex(Vector3 vec) {
//        return HexVector.FromWorld(vec, m_WorldSpace);
//    }
//}