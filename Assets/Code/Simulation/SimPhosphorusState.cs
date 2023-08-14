using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace Zavala.Sim {
    public sealed class SimPhosphorusState : SharedStateComponent, IRegistrationCallbacks {
        public SimTimer Timer;

        [NonSerialized] public PhosphorusBuffers Phosphorus;
        [NonSerialized] public uint UpdatedPhosphorusRegionMask = 0;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            SimGridState gridState = ZavalaGame.SimGrid;
            Phosphorus.Create(gridState.HexSize);
            UpdatedPhosphorusRegionMask = 0;
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
                }
            }
            return amount;
        }
    }
}