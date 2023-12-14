using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Scripting;
using Zavala.World;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.Update, -49, ZavalaGame.SimulationUpdateMask)] // after phosphorus system
    public sealed class SimAlgaeSystem : SharedStateSystemBehaviour<SimAlgaeState, SimPhosphorusState, SimGridState> {
        // does it make sense to define events in the system or the state?

        public override void ProcessWork(float deltaTime) {
            if (m_StateB.Timer.HasAdvanced()) {
                // for each tile that had a phosphorus change:
                foreach (int tileIndex in m_StateB.Phosphorus.Changes.AffectedTiles) {
                    // check if tile is water
                    ushort flags = m_StateB.Phosphorus.Info[tileIndex].Flags;
                    bool isWater = (flags & (ushort) TerrainFlags.IsWater) != 0;
                    if (!isWater) continue;
                    // get phosphorus count from tile
                    int phosphorusCount = m_StateB.Phosphorus.CurrentState()[tileIndex].Count;
                    // update GrowingTiles
                    if (phosphorusCount >= m_StateA.CurrentMinPForAlgaeGrowth) {
                        m_StateA.Algae.GrowingTiles.Add(tileIndex);
                    } else {
                        m_StateA.Algae.GrowingTiles.Remove(tileIndex);
                        m_StateA.Algae.State[tileIndex].IsPeaked = false;
                    }


                }
                // Grow algae
                foreach (int tile in m_StateA.Algae.GrowingTiles) {
                    ref float algaeGrowth = ref m_StateA.Algae.State[tile].PercentAlgae;
                    // trigger algae events
                    // TODO: don't dispatch events if algae has already peaked
                    if (!m_StateA.Algae.State[tile].IsPeaked) {
                        DispatchGrowthEvent(algaeGrowth, tile, ref m_StateA.Algae);
                    }
                    // increment by step
                    if (algaeGrowth < 1) {
                        algaeGrowth += AlgaeSim.AlgaeGrowthIncrement;
                    }
                }

                // for bloomed tiles that aren't growing (i.e. below P threshold):
                // decrement algae. if this brings them to 0 or lower, remove from bloomed

                m_StateA.Algae.BloomedTiles.RemoveWhere(
                    tile => !m_StateA.Algae.GrowingTiles.Contains(tile) 
                    && (m_StateA.Algae.State[tile].PercentAlgae -= AlgaeSim.AlgaeGrowthIncrement) <= 0);
            }
        }

        private void DispatchGrowthEvent (float currentGrowth, int tileIndex, ref AlgaeBuffers algaeBuffers) {
            if (currentGrowth <= 0) {
                ZavalaGame.Events.Dispatch(SimAlgaeState.Event_AlgaeFormed, tileIndex);
                algaeBuffers.BloomedTiles.Add(tileIndex);
                // InstantiateAlgae(m_StateC, tileIndex);
            } else if (currentGrowth >= 1) {
                ZavalaGame.Events.Dispatch(SimAlgaeState.Event_AlgaePeaked, tileIndex);
                if (!algaeBuffers.State[tileIndex].IsPeaked) {
                    algaeBuffers.State[tileIndex].IsPeaked = true;
                    algaeBuffers.PeakingTiles.Add(tileIndex);
                }
            } else {
                ZavalaGame.Events.Dispatch(SimAlgaeState.Event_AlgaeGrew, tileIndex);
            }
        }

        /*
        /// <summary>
        /// Temporary solution for instantiating an algae game object
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="tileIndex"></param>
        private void InstantiateAlgae (SimGridState grid, int tileIndex) {
            // TODO: grow algae object based on growth state?
            // associate with 
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            // Vector3 worldPos = HexVector.ToWorld(tileIndex, grid.Terrain.Height[tileIndex], ZavalaGame.SimWorld.WorldSpace);
            worldPos.y += 0.01f;
            GameObject newAlgae = Instantiate(m_StateA.AlgaePrefab, worldPos, m_StateA.AlgaePrefab.transform.rotation);
            if ((grid.Terrain.Info[tileIndex].Flags & TerrainFlags.IsInGroup) != 0) {
                if (newAlgae.TryGetComponent(out EventActor evtActor)) {
                    Destroy(evtActor);
                }
            }
        }
        */
    }
}