using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using Leaf.Runtime;
using System;
using UnityEngine;
using Zavala.Data;
using static UnityEditor.Experimental.GraphView.Port;

namespace Zavala.Sim {
    public sealed class SimAlgaeState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject {
        static public readonly StringHash32 Event_AlgaeFormed = "AlgaeState::AlgaeFormed";
        static public readonly StringHash32 Event_AlgaeGrew = "AlgaeState::AlgaeGrew";
        static public readonly StringHash32 Event_AlgaePeaked = "AlgaeState::AlgaePeaked";

        [NonSerialized] public int CurrentMinPForAlgaeGrowth;
        [NonSerialized] public AlgaeBuffers Algae;
        // public GameObject AlgaePrefab;

        [Header("Per-Region")]
        [NonSerialized] public float[] TotalAlgaePerRegion;

        void IRegistrationCallbacks.OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Algae");
        }

        void IRegistrationCallbacks.OnRegister() {
            CurrentMinPForAlgaeGrowth = AlgaeSim.MinPForAlgaeGrowthDefault;
            SimGridState gridState = ZavalaGame.SimGrid;
            TotalAlgaePerRegion = new float[RegionInfo.MaxRegions];
            Algae.Create(gridState.HexSize);

            ZavalaGame.SaveBuffer.RegisterHandler("Algae", this, 101);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts) {
            int delta = reader.Read<int>();

            Algae.PeakingTiles.Clear();
            Algae.BloomedTiles.Clear();
            Algae.GrowingTiles.Clear();

            CurrentMinPForAlgaeGrowth = AlgaeSim.MinPForAlgaeGrowthDefault + delta;
            ArrayUtils.EnsureCapacity(ref TotalAlgaePerRegion, consts.MaxRegions);
            for (int i = 0; i < consts.MaxRegions; i++) {
                reader.Read(ref TotalAlgaePerRegion[i]);
            }

            for (int i = 0; i < consts.DataRegion.Size; i++) {
                int idx = consts.DataRegion.FastIndexToGridIndex(i);
                ref var state = ref Algae.State[idx];
                reader.Read(ref state.PercentAlgae);
                state.IsPeaked = state.PercentAlgae >= 1;
                if (state.IsPeaked) {
                    Algae.PeakingTiles.Add(idx);
                } else if (state.PercentAlgae > 0) {
                    Algae.GrowingTiles.Add(idx);
                    Algae.BloomedTiles.Add(idx);
                }
            }
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts) {
            writer.Write(CurrentMinPForAlgaeGrowth - AlgaeSim.MinPForAlgaeGrowthDefault);
            for(int i = 0; i < consts.MaxRegions; i++) {
                writer.Write(TotalAlgaePerRegion[i]);
            }

            for (int i = 0; i < consts.DataRegion.Size; i++) {
                int idx = consts.DataRegion.FastIndexToGridIndex(i);
                writer.Write(Algae.State[idx].PercentAlgae);
            }
        }
    }
    public class SimAlgaeUtility {
        static public float AddAlgaeToTile(SimAlgaeState algaeState, int tileIndex, float delta, int region) {
            ref float current = ref algaeState.Algae.State[tileIndex].PercentAlgae;
            if (current + delta > 1) {
                delta = 1 - current;
            } else if (current + delta < 0) { //
                delta = -current;
            }
            current += delta;
            //int region = Game.SharedState.Get<SimGridState>().Terrain.Info[tileIndex].RegionIndex;
            RecordAlgaeToRegionTotal(algaeState, region, delta);
            return delta;
        }
        static public float RemoveAlgae(SimAlgaeState algaeState, int tileIndex, float amount, int region) {
            return AddAlgaeToTile(algaeState, tileIndex, -amount, region);
        }

        static public void RecordAlgaeToRegionTotal(SimAlgaeState state, int regionIndex, float amt) {
            state.TotalAlgaePerRegion[regionIndex] += amt;
        }

        [LeafMember("SetAlgaeGrowthThreshold")]
        public static void SetAlgaeGrowthThreshold(int minPForAlgae) {
            Game.SharedState.Get<SimAlgaeState>().CurrentMinPForAlgaeGrowth = minPForAlgae;
        }

        [LeafMember("OffsetAlgaeGrowthThreshold")]
        public static void OffsetAlgaeGrowthThreshold(int delta) {
            Game.SharedState.Get<SimAlgaeState>().CurrentMinPForAlgaeGrowth = AlgaeSim.MinPForAlgaeGrowthDefault + delta;
        }

    }
}