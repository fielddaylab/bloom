using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.World;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.Update, -49)] // after phosphorus system
    public sealed class SimAlgaeSystem : SharedStateSystemBehaviour<SimAlgaeState, SimPhosphorusState, SimGridState> {
        // does it make sense to define events in the system or the state?
        static public readonly StringHash32 Event_AlgaeFormed = "AlgaeState::AlgaeFormed";
        static public readonly StringHash32 Event_AlgaeGrew = "AlgaeState::AlgaeGrew";
        static public readonly StringHash32 Event_AlgaePeaked = "AlgaeState::AlgaePeaked";

        public override void ProcessWork(float deltaTime) {
            if (m_StateB.Timer.HasAdvanced()) {
                // for each tile that had a phosphorus change:
                foreach (int tile in m_StateB.Phosphorus.Changes.AffectedTiles) {
                    ushort flags = m_StateB.Phosphorus.Info[tile].Flags;
                    bool isWater = (flags & (ushort) TerrainFlags.IsWater) != 0;
                    if (!isWater) continue;
                    int phosphorusCount = m_StateB.Phosphorus.CurrentState()[tile].Count;
                    // update GrowingTiles
                    // (add and remove have their own checks for contains)
                    if (phosphorusCount >= AlgaeSim.MinPForAlgaeGrowth) {
                        Debug.Log(m_StateA);
                        Debug.Log(m_StateA.Algae);
                        Debug.Log(m_StateA.Algae.GrowingTiles);

                        m_StateA.Algae.GrowingTiles.Add(tile);
                    } else {
                        m_StateA.Algae.GrowingTiles.Remove(tile);
                    }
                }
                foreach (int tile in m_StateA.Algae.GrowingTiles) {
                    ref float algaeGrowth = ref m_StateA.Algae.State[tile].PercentAlgae;
                    // trigger algae events
                    DispatchGrowthEvent(algaeGrowth, tile);
                    // increment by step
                    algaeGrowth += AlgaeSim.AlgaeGrowthIncrement;
                }
            }
        }

        private void DispatchGrowthEvent (float currentGrowth, int tile) {
            if (currentGrowth <= 0) {
                ZavalaGame.Events.Dispatch(Event_AlgaeFormed, tile);
                InstantiateAlgae(m_StateC, tile);
            } else if (currentGrowth < 1) {
                ZavalaGame.Events.Dispatch(Event_AlgaeGrew, tile);
            } else {
                ZavalaGame.Events.Dispatch(Event_AlgaePeaked, tile);
            }
        }

        /// <summary>
        /// Temporary solution for instantiating an algae game object
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="tileIndex"></param>
        private void InstantiateAlgae (SimGridState grid, int tileIndex) {
            // TODO: grow algae object based on growth state?
            GameObject newAlgae = Instantiate(m_StateA.AlgaePrefab);
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            worldPos.y += 0.01f;
            newAlgae.transform.position = worldPos;
        }
    }
}