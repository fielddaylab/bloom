using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using System;
using UnityEngine;
using Zavala.Actors;
using Zavala.Data;
using Zavala.Economy;
using Zavala.Rendering;
using static FieldDay.Scenes.PreloadManifest;

namespace Zavala.Sim
{
    public sealed class SimPhosphorusState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject {
        public SimTimer Timer;

        [NonSerialized] public PhosphorusBuffers Phosphorus;
        [NonSerialized] public uint UpdatedPhosphorusRegionMask = 0;

        [Header("Per-Region")]
        public DataHistory[] HistoryPerRegion;
        [NonSerialized] public long[] TotalPPerRegion;

        void IRegistrationCallbacks.OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Phosphorus");
        }

        void IRegistrationCallbacks.OnRegister() {
            SimGridState gridState = ZavalaGame.SimGrid;
            Phosphorus.Create(gridState.HexSize);
            UpdatedPhosphorusRegionMask = 0;
            TotalPPerRegion = new long[RegionInfo.MaxRegions];
            DataHistoryUtil.InitializeDataHistory(ref HistoryPerRegion, RegionInfo.MaxRegions, 20);

            ZavalaGame.SaveBuffer.RegisterHandler("Phosphorus", this, 100);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            var grid = ZavalaGame.SimGrid;
            var currentBuffer = Phosphorus.CurrentState();
            for (int i = 0; i < consts.DataRegion.Size; i++) {
                int idx = consts.DataRegion.FastIndexToGridIndex(i);
                ushort count = reader.Read<ushort>();
                currentBuffer[idx].Count = count;

                if (count > 0) {
                    Phosphorus.Changes.PushAdd(new PhosphorusTileAddRemove() {
                        Amount = count,
                        TileIdx = idx,
                        RegionIndex = grid.Terrain.Regions[idx]
                    });
                }
            }

            ArrayUtils.EnsureCapacity(ref TotalPPerRegion, consts.MaxRegions);
            for(int i = 0; i < consts.MaxRegions; i++) {
                reader.Read(ref TotalPPerRegion[i]);
            }

            DataHistoryUtil.Read(ref HistoryPerRegion, ref reader);
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            var currentBuffer = Phosphorus.CurrentState();
            for (int i = 0; i < consts.DataRegion.Size; i++) {
                int idx = consts.DataRegion.FastIndexToGridIndex(i);
                writer.Write(currentBuffer[idx].Count);
            }

            for (int i = 0; i < consts.MaxRegions; i++) {
                writer.Write(TotalPPerRegion[i]);
            }

            DataHistoryUtil.Write(HistoryPerRegion, ref writer);
        }
    }

    /// <summary>
    /// Phosphorus state utility methods.
    /// </summary>
    static public class SimPhospohorusUtility {
        /// <summary>
        /// Adds phosphorus to the given system and tile index.
        /// </summary>
        static public int AddPhosphorus(SimPhosphorusState phosphorusState, int tileIndex, int amount, bool allowOversaturation = true) {
            Assert.True(amount >= 0);
            if (amount > 0) {
                PhosphorusTileInfo tileInfo = phosphorusState.Phosphorus.Info[tileIndex];
                ref ushort count = ref phosphorusState.Phosphorus.CurrentState()[tileIndex].Count;
                int available = (allowOversaturation ? PhosphorusSim.MaxPhosphorusPerTile : PhosphorusSim.TileSaturationThreshold) - count;
                Log.Msg("[SimPhosphorusUtility] Tile {0}: Attempting to add {1} phosphorus with {2} space remaining", tileIndex, amount, available);
                amount = Math.Min(amount, available);
                if (amount > 0) {
                    count += (ushort) amount;
                    PhosphorusTileAddRemove addRecord = new PhosphorusTileAddRemove() {
                        TileIdx = tileIndex,
                        RegionIndex = tileInfo.RegionIndex,
                        Amount = (ushort) amount
                    };
                    phosphorusState.Phosphorus.Changes.PushAdd(addRecord);
                    RecordToPhosphorusHistory(phosphorusState, addRecord.RegionIndex, addRecord.Amount);
                }
            }
            return amount;
        }

        /// <summary>
        /// Removes phosphorus from the given system and tile index.
        /// </summary>
        static public int RemovePhosphorus(SimPhosphorusState phosphorusState, int tileIndex, int amount) {
            Assert.True(amount >= 0);
            if (amount > 0) {
                PhosphorusTileInfo tileInfo = phosphorusState.Phosphorus.Info[tileIndex];
                ref ushort count = ref phosphorusState.Phosphorus.CurrentState()[tileIndex].Count;
                amount = Math.Min(amount, count);
                count -= (ushort) amount;

                if (amount > 0) {
                    PhosphorusTileAddRemove removeRecord = new PhosphorusTileAddRemove() {
                        TileIdx = tileIndex,
                        RegionIndex = tileInfo.RegionIndex,
                        Amount = (ushort)amount
                    };
                    phosphorusState.Phosphorus.Changes.PushRemove(removeRecord);
                    RecordToPhosphorusHistory(phosphorusState, removeRecord.RegionIndex, removeRecord.Amount);
                }
            }
            return amount;
        }

        static public void RecordToPhosphorusHistory(SimPhosphorusState state, int regionIndex, int phosphorusDelta) {
            // only record when out of tutorial
            TutorialState tutorial = Game.SharedState.Get<TutorialState>();
            if (tutorial.CurrState <= TutorialState.State.InactiveSim) {
                return;
            }

            state.HistoryPerRegion[regionIndex].AddPending(phosphorusDelta);
            state.TotalPPerRegion[regionIndex] += (phosphorusDelta);
        }

        static public void GenerateProportionalPhosphorus(SimPhosphorusState phosphorus, int tileIndex, ActorPhosphorusGenerator generator, ref ResourceBlock resources, int manureMod, int mFertMod, int dFertMod, bool consume) {

            int totalToAdd = generator.Amount * (
                resources.Manure * manureMod +
                resources.MFertilizer * mFertMod +
                resources.DFertilizer * dFertMod
                );
            if (consume && resources.PhosphorusCount > RunoffParams.RunoffConsumeFertilizerThreshold) {
                ResourceBlock.GatherPhosphorusPrioritized(resources, totalToAdd, out ResourceBlock outBlock);
                resources -= outBlock;
                //ResourceBlock.Consume(ref resources, outBlock);
                if (generator.RunoffParticleOrigin != null) {
                    ResourceStorageUtility.RefreshStorageDisplays(generator.transform.parent.GetComponent<ResourceStorage>());
                    VfxUtility.PlayEffect(generator.RunoffParticleOrigin.position, EffectType.Poop_Runoff);
                }    
            }
            AddPhosphorus(phosphorus, tileIndex, totalToAdd);
            generator.AmountProducedLastTick = totalToAdd;
            if (totalToAdd > 0 && totalToAdd < mFertMod) {
                generator.RunoffImproving = true;
            } else generator.RunoffImproving = false;
        }

        static public bool TryRunoffManure(SimPhosphorusState phosphorus, int tileIndex, ActorPhosphorusGenerator gen, ref ResourceBlock resources) {
            if (!gen.ConsumeFertilizerForRunoff) return false;
            if (resources.Manure <= 0) {
                gen.AmountProducedLastTick = 0;
                return false;
            }
            if (resources.Manure < RunoffParams.RunoffConsumeFertilizerThreshold) {
                AddPhosphorus(phosphorus, tileIndex, RunoffParams.PassiveManureRunoff);
                gen.AmountProducedLastTick = RunoffParams.PassiveManureRunoff;
                return false;
            }

            resources.Manure -= RunoffParams.RunoffConsumeFertilizerThreshold;
            if (gen.RunoffParticleOrigin != null) {
                ResourceStorageUtility.RefreshStorageDisplays(gen.transform.parent.GetComponent<ResourceStorage>());
                VfxUtility.PlayEffect(gen.RunoffParticleOrigin.position, EffectType.Poop_Runoff);
            }
            gen.RunoffImproving = false;
            AddPhosphorus(phosphorus, tileIndex, RunoffParams.SittingManureRunoffProportion);
            gen.AmountProducedLastTick = RunoffParams.SittingManureRunoffProportion;
            return true;
        }
    }
}