using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;

namespace Zavala.Sim
{
    public struct PhosphorusProductionHistory {
        public const int MaxHistory = 20; // how far back history is stored
        
        public int Pending;
        public LinkedList<int> Net;  // store show much P was produced/removed over the previous ticks

        /// <summary>
        /// Outputs average phosphorus produced/removed over "depth" # of ticks
        /// </summary>
        /// <param name="depth">number of ticks to analyze</param>
        /// <param name="avg"></param>
        /// <returns></returns>
        public bool TryGetAvg(int depth, out float avg) {
            if (depth > MaxHistory || depth > Net.Count) {
                avg = float.MaxValue;
                return false;
            }

            int sum = 0;
            int index = 0;
            foreach(int i in Net) {
                if (index >= depth) {
                    break;
                }
                sum += i;
                index++;
            }

            avg = (float)sum / Net.Count;
            return true;
        }

        /// <summary>
        /// Outputs total phosphorus produced/removed over "depth" # of ticks
        /// </summary>
        /// <param name="depth">number of ticks to analyze</param>
        /// <param name="avg"></param>
        /// <returns></returns>
        public bool TryGetTotal(int depth, out float total) {
            if (depth > MaxHistory || depth > Net.Count) {
                total = float.MaxValue;
                return false;
            }

            int sum = 0;
            int index = 0;
            foreach (int i in Net) {
                if (index >= depth) {
                    break;
                }
                sum += i;
                index++;
            }

            total = sum;
            return true;
        }
    }

    public sealed class SimPhosphorusState : SharedStateComponent, IRegistrationCallbacks {
        public SimTimer Timer;

        [NonSerialized] public PhosphorusBuffers Phosphorus;
        [NonSerialized] public uint UpdatedPhosphorusRegionMask = 0;

        [Header("Per-Region")]
        public PhosphorusProductionHistory[] HistoryPerRegion = new PhosphorusProductionHistory[RegionInfo.MaxRegions];

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            SimGridState gridState = ZavalaGame.SimGrid;
            Phosphorus.Create(gridState.HexSize);
            UpdatedPhosphorusRegionMask = 0;

            for (int i = 0; i < HistoryPerRegion.Length; i++) {
                SimPhospohorusUtility.SetHistory(this, new LinkedList<int>(), i);
            }
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
                    RecordToHistory(phosphorusState, addRecord.RegionIndex, addRecord.Amount);
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
                    RecordToHistory(phosphorusState, removeRecord.RegionIndex, removeRecord.Amount);
                }
            }
            return amount;
        }

        static public void SetHistory(SimPhosphorusState state, LinkedList<int> history, int regionIndex) {
            state.HistoryPerRegion[regionIndex].Net = history;
        }

        static public void RecordToHistory(SimPhosphorusState state, int regionIndex, int phosphorusDelta) {
            state.HistoryPerRegion[regionIndex].Pending += phosphorusDelta;
        }
    }
}