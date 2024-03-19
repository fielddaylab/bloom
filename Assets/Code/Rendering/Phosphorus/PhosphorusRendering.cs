using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using UnityEngine;
using Zavala.Sim;

namespace Zavala {
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
        public RingBuffer<PhosphorusRenderInstanceAnimated> AnimatingInstances;

        public void Create() {
            int perTile = (int) Unsafe.AlignUp16((uint) PhosphorusSim.TileSaturationThreshold);
            AnimatingInstances = new RingBuffer<PhosphorusRenderInstanceAnimated>(perTile * 4, RingBufferMode.Expand);
        }
    }

    static public class PhosphorusRendering {
        [ConfigVar("Particle Amount", 1, 32, 1)] static public int ParticlesPerPip = 32;
        [ConfigVar("Particle Size", 0, 1, 0.01f)] static public float ParticleSize = 0.05f;
        [ConfigVar("Particle Lerp", 0, 16, 0.25f)] static public float ParticleLerp = 7;

        public delegate Vector3 RandomTilePositionDelegate(int index, ushort height, in HexGridWorldSpace worldSpace);

        static public void PrepareChangeBuffer(PhosphorusChangeBuffer changeBuffer) {
            //changeBuffer.Remove.Quicksort(PhosphorusTileAddRemove.Sorter.Instance);
            //changeBuffer.Add.Quicksort(PhosphorusTileAddRemove.Sorter.Instance);
            changeBuffer.Transfers.Quicksort(PhosphorusTileTransfer.Sorter.Instance);
        }

        static public void ProcessChanges(PhosphorusRenderState[] renderStates, int numRenderStates, PhosphorusChangeBuffer changeBuffer, SimBuffer<PhosphorusTileState> currentState, SimBuffer<TerrainTileInfo> terrainInfo, SimBuffer<ushort> heightmap, in HexGridWorldSpace worldSpace, RandomTilePositionDelegate getRandomPos, System.Random randomizer, byte updateIndex) {
            bool hasTransfer = changeBuffer.Transfers.Count > 0;

            if (hasTransfer) {
                unsafe {
                    PhosphorusTileTransfer transfer;
                    int totalTransfers = changeBuffer.Transfers.Count;
                    int regionIndex;
                    bool tileIsWater, targetIsWater;

                    for(int i = 0; i < totalTransfers; i++) {
                        transfer = changeBuffer.Transfers[i];
                        regionIndex = transfer.EndRegionIndex;

                        tileIsWater = (terrainInfo[transfer.StartIdx].Flags & TerrainFlags.IsWater) != 0;
                        targetIsWater = (terrainInfo[transfer.EndIdx].Flags & TerrainFlags.IsWater) != 0;

                        if (tileIsWater && targetIsWater) {
                            continue;
                        }

                        ref PhosphorusRenderState renderState = ref renderStates[regionIndex];

                        int toCreate = transfer.Transfer * ParticlesPerPip;
                        while(toCreate-- > 0) {
                            PhosphorusRenderInstanceAnimated animated;
                            animated.AnimationDuration = 0;
                            animated.AnimationType = 0;
                            animated.GridIndex = transfer.EndIdx;
                            animated.LastModifiedFrame = Frame.Index8;
                            animated.StartingPosition = getRandomPos(transfer.StartIdx, heightmap[transfer.StartIdx], worldSpace);
                            animated.Position = animated.StartingPosition;
                            animated.TargetPosition = getRandomPos(transfer.EndIdx, heightmap[transfer.EndIdx], worldSpace);
                            renderState.AnimatingInstances.PushBack(animated);
                        }
                    }
                }
            }
        }

        static public void ProcessMovement(PhosphorusRenderState renderState, float deltaTime, float lerpAmount, float sqrMinDistance) {
            for(int i = renderState.AnimatingInstances.Count - 1; i >= 0; i--) {
                ref PhosphorusRenderInstanceAnimated inst = ref renderState.AnimatingInstances[i];
                bool done = false;
                inst.Position = Vector3.Lerp(inst.Position, inst.TargetPosition, lerpAmount);
                if (Vector3.SqrMagnitude(inst.Position - inst.TargetPosition) < sqrMinDistance) {
                    done = true;
                }
                
                if (done) {
                    renderState.AnimatingInstances.FastRemoveAt(i);
                }
            }
        }
    }
}