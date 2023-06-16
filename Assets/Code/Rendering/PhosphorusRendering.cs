using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using UnityEngine;
using Zavala.Sim;

namespace Zavala {
    /// <summary>
    /// Minimum state to render a static phosphorus pip.
    /// </summary>
    public struct PhosphorusRenderInstanceStatic {
        public int GridIndex;
        public Vector3 Position;
        public PhosphorusRenderInstanceAnimation AnimationType;

        public sealed class SortByIndex : IComparer<PhosphorusRenderInstanceStatic> {
            public int Compare(PhosphorusRenderInstanceStatic x, PhosphorusRenderInstanceStatic y) {
                return x.GridIndex - y.GridIndex;
            }

            static public readonly SortByIndex Instance = new SortByIndex();
        }
    }

    /// <summary>
    /// Minimum state to animate a phosphorus pip.
    /// </summary>
    public struct PhosphorusRenderInstanceAnimated {
        public int GridIndex;
        public Vector3 Position;
        public Vector3 StartingPosition;
        public Vector3 TargetPosition;
        public float AnimationDuration;
        public byte LastModifiedFrame;
        public PhosphorusRenderInstanceAnimation AnimationType;

        public sealed class SortByIndex : IComparer<PhosphorusRenderInstanceAnimated> {
            public int Compare(PhosphorusRenderInstanceAnimated x, PhosphorusRenderInstanceAnimated y) {
                return x.GridIndex - y.GridIndex;
            }

            static public readonly SortByIndex Instance = new SortByIndex();
        }
    }

    /// <summary>
    /// Phosphorus pip animation type.
    /// </summary>
    public enum PhosphorusRenderInstanceAnimation : byte {
        Default = 0,
        WaterSpawn,
        LandSpawn,
        LandToWater,
        WaterToLand,
        WaterToWater,
    }

    /// <summary>
    /// Buffers for stationary and animating phosphorus instances
    /// </summary>
    public struct PhosphorusRenderState {
        public RingBuffer<PhosphorusRenderInstanceStatic> StationaryInstances;
        public RingBuffer<PhosphorusRenderInstanceAnimated> AnimatingInstances;

        public void Create() {
            int perTile = (int) Unsafe.AlignUp16((uint) PhosphorusSim.TileSaturationThreshold);
            StationaryInstances = new RingBuffer<PhosphorusRenderInstanceStatic>(perTile * 4, RingBufferMode.Expand);
            AnimatingInstances = new RingBuffer<PhosphorusRenderInstanceAnimated>(perTile * 4, RingBufferMode.Expand);
        }
    }

    static public class PhosphorusRendering {
        public delegate Vector3 RandomTilePositionDelegate(int index, ushort height, in HexGridWorldSpace worldSpace);

        static public void PrepareChangeBuffer(PhosphorusChangeBuffer changeBuffer) {
            changeBuffer.Remove.Sort(PhosphorusTileAddRemove.Sorter.Instance);
            changeBuffer.Add.Sort(PhosphorusTileAddRemove.Sorter.Instance);
            changeBuffer.Transfers.Sort(PhosphorusTileTransfer.Sorter.Instance);
        }

        static public void ProcessChanges(PhosphorusRenderState[] renderStates, int numRenderStates, PhosphorusChangeBuffer changeBuffer, SimBuffer<PhosphorusTileState> currentState, SimBuffer<TerrainTileInfo> terrainInfo, SimBuffer<ushort> heightmap, in HexGridWorldSpace worldSpace, RandomTilePositionDelegate getRandomPos, System.Random randomizer, byte updateIndex) {
            bool hasRemove = changeBuffer.Remove.Count > 0;
            bool hasTransfer = changeBuffer.Transfers.Count > 0;
            bool hasAdd = changeBuffer.Add.Count > 0;

            if (hasRemove || hasTransfer) {
                for(int i = 0; i < numRenderStates; i++) {
                    renderStates[i].StationaryInstances.Sort(PhosphorusRenderInstanceStatic.SortByIndex.Instance);
                    renderStates[i].AnimatingInstances.Sort(PhosphorusRenderInstanceAnimated.SortByIndex.Instance);
                }

                unsafe {
                    int tileIndex = -1;
                    int targetTileIndex = -1;
                    int removeBufferIndex = 0;
                    int nextRemoveIndex = -1;
                    int transferBufferIndex = 0;
                    int nextTransferIndex = -1;
                    PhosphorusTileTransfer transfer;

                    int totalRemoves = changeBuffer.Remove.Count;
                    int totalTransfers = changeBuffer.Transfers.Count;
                    
                    if (totalRemoves > 0) {
                        nextRemoveIndex = changeBuffer.Remove[0].TileIdx;
                    }
                    if (totalTransfers > 0) {
                        nextTransferIndex = changeBuffer.Transfers[0].StartIdx;
                    }

                    PhosphorusRenderInstanceStatic* tempStationary = Frame.AllocArray<PhosphorusRenderInstanceStatic>(PhosphorusSim.MaxPhosphorusPerTile);
                    PhosphorusRenderInstanceAnimated* tempAnimated = Frame.AllocArray<PhosphorusRenderInstanceAnimated>(PhosphorusSim.MaxPhosphorusPerTile);

                    int totalStationary, totalAnimated, toProcess, processIndex;
                    bool shuffled;
                    int regionIndex, targetRegionIndex;
                    bool tileIsWater, targetIsWater, targetSteepDrop;

                    while(removeBufferIndex < totalRemoves || transferBufferIndex < totalTransfers) {
                        int minIndex;
                        if (removeBufferIndex < totalRemoves) {
                            if (transferBufferIndex < totalTransfers) {
                                if (nextRemoveIndex < nextTransferIndex) {
                                    minIndex = nextRemoveIndex;
                                    regionIndex = changeBuffer.Remove[removeBufferIndex].RegionIndex;
                                } else {
                                    minIndex = nextTransferIndex;
                                    regionIndex = changeBuffer.Transfers[transferBufferIndex].StartRegionIndex;
                                }
                            } else {
                                minIndex = nextRemoveIndex;
                                regionIndex = changeBuffer.Remove[removeBufferIndex].RegionIndex;
                            }
                        } else if (transferBufferIndex < totalTransfers) {
                            minIndex = nextTransferIndex;
                            regionIndex = changeBuffer.Transfers[transferBufferIndex].StartRegionIndex;
                        } else {
                            Assert.Fail("somehow looped back around without fulfilling all change requests ({0} remove {1} transfer)", totalRemoves - removeBufferIndex, totalTransfers - transferBufferIndex);
                            minIndex = -1;
                            regionIndex = -1;
                        }

                        ref PhosphorusRenderState renderState = ref renderStates[regionIndex];

                        FastForward(renderState.StationaryInstances, minIndex);
                        FastForward(renderState.AnimatingInstances, minIndex);
                        
                        tileIndex = minIndex;
                        tileIsWater = (terrainInfo[tileIndex].Flags & TerrainFlags.IsWater) != 0;
                        totalStationary = ExtractMatchingInstances(tempStationary, PhosphorusSim.MaxPhosphorusPerTile, renderState.StationaryInstances, tileIndex);
                        totalAnimated = ExtractMatchingInstances(tempAnimated, PhosphorusSim.MaxPhosphorusPerTile, renderState.AnimatingInstances, tileIndex);
                        shuffled = false;

                        while(nextRemoveIndex == tileIndex) {
                            if (!shuffled) {
                                shuffled = true;
                                UnsafeExt.Shuffle(tempStationary, totalStationary, randomizer);
                                UnsafeExt.Shuffle(tempAnimated, totalAnimated, randomizer);
                            }

                            toProcess = changeBuffer.Remove[removeBufferIndex].Amount;
                            processIndex = totalStationary - 1;
                            //Log.Msg("[PhosphorusRendering] Processing remove of {0} from tile {1}", toProcess, tileIndex);
                            while(toProcess > 0 && processIndex >= 0) {
                                processIndex--;
                                totalStationary--;
                                toProcess--;
                            }

                            processIndex = 0;
                            while(toProcess > 0 && processIndex < totalAnimated) {
                                PhosphorusRenderInstanceAnimated inst = tempAnimated[processIndex];
                                if (inst.LastModifiedFrame != updateIndex) {
                                    UnsafeExt.FastRemoveAt(tempAnimated, ref totalAnimated, processIndex);
                                    toProcess--;
                                } else {
                                    processIndex++;
                                }
                            }

                            Assert.True(toProcess == 0, "Pip count for tile {0} was insufficient for handling remove animation - missing {1}", tileIndex, toProcess);

                            removeBufferIndex++;
                            nextRemoveIndex = removeBufferIndex >= totalRemoves ? -1 : changeBuffer.Remove[removeBufferIndex].TileIdx;
                        }

                        while(nextTransferIndex == tileIndex) {
                            if (!shuffled) {
                                shuffled = true;
                                UnsafeExt.Shuffle(tempStationary, totalStationary, randomizer);
                                UnsafeExt.Shuffle(tempAnimated, totalAnimated, randomizer);
                            }

                            transfer = changeBuffer.Transfers[transferBufferIndex];
                            toProcess = transfer.Transfer;
                            targetTileIndex = transfer.EndIdx;
                            targetRegionIndex = transfer.EndRegionIndex;
                            targetIsWater = (terrainInfo[targetTileIndex].Flags & TerrainFlags.IsWater) != 0;
                            //Log.Msg("[PhosphorusRendering] Processing transfer of {0} from tile {1} to tile {2}", toProcess, tileIndex, targetTileIndex);

                            processIndex = totalStationary - 1;
                            while(toProcess > 0 && processIndex >= 0) {
                                PhosphorusRenderInstanceStatic inst = tempStationary[processIndex];
                                totalStationary--;
                                toProcess--;
                                processIndex--;

                                PhosphorusRenderInstanceAnimated animated = ConvertToAnimated(inst, targetTileIndex, getRandomPos(targetTileIndex, heightmap[targetTileIndex], worldSpace), 0, updateIndex);
                                renderStates[targetRegionIndex].AnimatingInstances.PushBack(animated);
                            }
                            processIndex = 0;
                            while(toProcess > 0 && processIndex < totalAnimated) {
                                PhosphorusRenderInstanceAnimated inst = tempAnimated[processIndex];
                                if (inst.LastModifiedFrame != updateIndex) {
                                    UnsafeExt.FastRemoveAt(tempAnimated, ref totalAnimated, processIndex);
                                    toProcess--;

                                    RetargetAnimated(ref inst, targetTileIndex, getRandomPos(targetTileIndex, heightmap[targetTileIndex], worldSpace), 0, updateIndex);
                                    renderStates[targetRegionIndex].AnimatingInstances.PushBack(inst);
                                } else {
                                    processIndex++;
                                }
                            }

                            Assert.True(toProcess == 0, "Pip count for tile {0} was insufficient for handling transfer animation - missing {1}", tileIndex, toProcess);

                            transferBufferIndex++;
                            nextTransferIndex = transferBufferIndex >= totalTransfers ? -1 : changeBuffer.Transfers[transferBufferIndex].StartIdx;
                        }

                        AppendInstances(tempStationary, totalStationary, renderState.StationaryInstances);
                        AppendInstances(tempAnimated, totalAnimated, renderState.AnimatingInstances);
                    }
                }
            }

            // process adds
            if (hasAdd) {
                foreach (var req in changeBuffer.Add) {
                    int amount = req.Amount;
                    var instanceBuffer = renderStates[req.RegionIndex].StationaryInstances;
                    //Log.Msg("[PhosphorusRendering] Processing addition of {0} to tile {1}", amount, req.TileIdx);
                    while (amount-- > 0) {
                        instanceBuffer.PushBack(new PhosphorusRenderInstanceStatic() {
                            GridIndex = req.TileIdx,
                            Position = getRandomPos(req.TileIdx, heightmap[req.TileIdx], worldSpace)
                        });
                    }
                }
            }

            if (hasAdd || hasRemove || hasTransfer) {
                SanityCheckRenderInstancesToCurrentBuffer(renderStates, currentState);
            }
        }

        static public void ProcessMovement(PhosphorusRenderState renderState, float deltaTime, float lerpAmount, float sqrMinDistance) {
            for(int i = renderState.AnimatingInstances.Count - 1; i >= 0; i--) {
                ref PhosphorusRenderInstanceAnimated inst = ref renderState.AnimatingInstances[i];
                bool done = false;
                PhosphorusRenderInstanceAnimation nextAnim = 0;
                switch (inst.AnimationType) {
                    default:
                        inst.Position = Vector3.Lerp(inst.Position, inst.TargetPosition, lerpAmount);
                        if (Vector3.SqrMagnitude(inst.Position - inst.TargetPosition) < sqrMinDistance) {
                        }
                        break;
                }

                if (done) {
                    inst.Position = inst.TargetPosition;
                    renderState.StationaryInstances.PushBack(ConvertToStatic(inst, nextAnim));
                    renderState.AnimatingInstances.FastRemoveAt(i);
                }
            }
        }

        static private PhosphorusRenderInstanceAnimated ConvertToAnimated(in PhosphorusRenderInstanceStatic staticInstance, int tileIndex, Vector3 targetPosition, PhosphorusRenderInstanceAnimation animationType, byte lastModifiedFrame) {
            PhosphorusRenderInstanceAnimated animatedInstance;
            animatedInstance.StartingPosition = animatedInstance.Position = staticInstance.Position;
            animatedInstance.TargetPosition = targetPosition;
            animatedInstance.AnimationDuration = 0;
            animatedInstance.AnimationType = animationType;
            animatedInstance.GridIndex = tileIndex;
            animatedInstance.LastModifiedFrame = lastModifiedFrame;
            return animatedInstance;
        }

        static private void RetargetAnimated(ref PhosphorusRenderInstanceAnimated animatedInstance, int tileIndex, Vector3 targetPosition, PhosphorusRenderInstanceAnimation animationType, byte lastModifiedFrame) {
            animatedInstance.StartingPosition = animatedInstance.Position;
            animatedInstance.TargetPosition = targetPosition;
            animatedInstance.AnimationDuration = 0;
            animatedInstance.AnimationType = animationType;
            animatedInstance.GridIndex = tileIndex;
            animatedInstance.LastModifiedFrame = lastModifiedFrame;
        }

        static private PhosphorusRenderInstanceStatic ConvertToStatic(in PhosphorusRenderInstanceAnimated animatedInstance, PhosphorusRenderInstanceAnimation overrideAnim = 0) {
            PhosphorusRenderInstanceStatic staticInstance;
            staticInstance.GridIndex = animatedInstance.GridIndex;
            staticInstance.Position = animatedInstance.Position;
            staticInstance.AnimationType = overrideAnim;
            return staticInstance;
        }

        static private unsafe int ExtractMatchingInstances(PhosphorusRenderInstanceStatic* instanceBuffer, int instanceBufferSize, RingBuffer<PhosphorusRenderInstanceStatic> allInstances, int matchingIndex) {
            if (allInstances.Count == 0 || matchingIndex < 0) {
                return 0;
            }

            PhosphorusRenderInstanceStatic first = allInstances[0];
            int tileIdx = first.GridIndex;
            
            if (tileIdx != matchingIndex) {
                return 0;
            }

            int total = 0;
            while(allInstances.Count > 0 && allInstances[0].GridIndex == tileIdx) {
                if (total >= instanceBufferSize) {
                    Assert.Fail("Total static tiles extracted for tile index {0} exceeds cap of {1}", matchingIndex, instanceBufferSize);
                }
                instanceBuffer[total++] = allInstances.PopFront();
            }

            return total;
        }

        static private unsafe int ExtractMatchingInstances(PhosphorusRenderInstanceAnimated* instanceBuffer, int instanceBufferSize, RingBuffer<PhosphorusRenderInstanceAnimated> allInstances, int matchingIndex) {
            if (allInstances.Count == 0 || matchingIndex < 0) {
                return 0;
            }

            PhosphorusRenderInstanceAnimated first = allInstances[0];
            int tileIdx = first.GridIndex;

            if (tileIdx != matchingIndex) {
                return 0;
            }

            int total = 0;
            while (allInstances.Count > 0 && allInstances[0].GridIndex == tileIdx) {
                if (total >= instanceBufferSize) {
                    Assert.Fail("Total animated tiles extracted for tile index {0} exceeds cap of {1}", matchingIndex, instanceBufferSize);
                }
                instanceBuffer[total++] = allInstances.PopFront();
            }

            return total;
        }

        static private void FastForward(RingBuffer<PhosphorusRenderInstanceStatic> allInstances, int minIndex) {
            int count = allInstances.Count;
            while(count-- > 0 && allInstances[0].GridIndex != minIndex) {
                allInstances.PushBack(allInstances.PopFront());
            }
        }

        static private void FastForward(RingBuffer<PhosphorusRenderInstanceAnimated> allInstances, int minIndex) {
            int count = allInstances.Count;
            while (count-- > 0 && allInstances[0].GridIndex != minIndex) {
                allInstances.PushBack(allInstances.PopFront());
            }
        }

        static private unsafe void AppendInstances<T>(T* instanceBuffer, int count, RingBuffer<T> allInstances) where T : unmanaged {
            while(count-- > 0) {
                allInstances.PushBack(instanceBuffer[count]);
            }
        }

        [Conditional("UNITY_EDITOR")]
        static private unsafe void SanityCheckRenderInstancesToCurrentBuffer(PhosphorusRenderState[] renderStates, SimBuffer<PhosphorusTileState> targetCount) {
            PhosphorusTileState* currentPipCounts = stackalloc PhosphorusTileState[(int) targetCount.Length];
            for(int i = 0; i < targetCount.Length; i++) {
                currentPipCounts[i] = default;
            }

            foreach(var renderState in renderStates) {
                foreach(var pip in renderState.StationaryInstances) {
                    currentPipCounts[pip.GridIndex].Count++;
                }

                foreach (var pip in renderState.AnimatingInstances) {
                    currentPipCounts[pip.GridIndex].Count++;
                }
            }

            using(var psb = PooledStringBuilder.Create()) {
                for(int i = 0; i < targetCount.Length; i++) {
                    if (currentPipCounts[i].Count != targetCount[i].Count) {
                        psb.Builder.Append("\t[").Append(i).Append("] data ").Append(targetCount[i].Count).Append(" vs visual ").Append(currentPipCounts[i].Count).Append('\n');
                    }
                }

                psb.Builder.TrimEnd(StringUtils.DefaultNewLineChars);
                if (psb.Builder.Length > 0) {
                    Log.Error("[PhosphorusRendering] Pip count desync:\n{0}", psb.Builder.Flush());
                }
            }
        }
    }
}