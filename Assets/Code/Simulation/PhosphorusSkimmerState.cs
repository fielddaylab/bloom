using BeauPools;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Sim;
using static Zavala.Building.BuildingPools;

namespace Zavala.World {


    public class PhosphorusSkimmerState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public List<SkimmerLocation>[] SkimmerLocsPerRegion;

        public void OnRegister() {
            SkimmerLocsPerRegion = new List<SkimmerLocation>[RegionInfo.MaxRegions];
            for (int i = 0; i < RegionInfo.MaxRegions; i++) {
                SkimmerLocsPerRegion[i] = new List<SkimmerLocation>();
            }
        }

        public void OnDeregister() {
        }
    }

    public struct SkimmerLocation {
        public int TileIndex;
        public PhosphorusSkimmer PlacedSkimmer;
    }

    public static class PhosphorusSkimmerUtility {
        /// <summary>
        /// Add a skimmer location to the global skimmer location storage.
        /// </summary>
        /// <param name="skimState">Global shared state object holding skimmer locations.</param>
        /// <param name="regionIndex">Region to add this skimmer location to.</param>
        /// <param name="tileIndex">The tile index of this skimmer location.</param>
        /// <param name="type">The type of skimmer - Algae or Dredger</param>

        public static void AddSkimmerLocation(PhosphorusSkimmerState skimState, int regionIndex, int tileIndex) {
            skimState.SkimmerLocsPerRegion[regionIndex].Add(new SkimmerLocation {
                TileIndex = tileIndex
            });
        }

        public static void SpawnSkimmersInRegion(int regionIndex, int numSkimmers) {
            SkimmerPool skimmerPool = ZavalaGame.SharedState.Get<BuildingPools>().Skimmers;
            SimGridState grid = ZavalaGame.SharedState.Get<SimGridState>();
            List<SkimmerLocation> locs = ZavalaGame.SharedState.Get<PhosphorusSkimmerState>().SkimmerLocsPerRegion[regionIndex];
            for (int i = 0; i < locs.Count; i++) {
                if (locs[i].PlacedSkimmer != null) {
                    Debug.Log("[PhosphorusSkimmerUtility] Freeing " + locs[i].PlacedSkimmer);
                    skimmerPool.Free(locs[i].PlacedSkimmer);
                    locs[i] = new SkimmerLocation() {
                        TileIndex = locs[i].TileIndex,
                        PlacedSkimmer = null
                    };
                }
            }
            for (int i = 0; i < numSkimmers; i++) {
                PhosphorusSkimmer skim = PlaceSkimmer(grid, skimmerPool, locs[i]);
                skim.gameObject.SetActive(true);
                Debug.LogWarning("[PhosphorusSkimmerState] Set skimmer to " + skim);
                locs[i] = new SkimmerLocation() {
                    TileIndex = locs[i].TileIndex,
                    PlacedSkimmer = skim
                };
                if (i == 3) { // dredging policy
                    // PlacedSkimmer.Type = Dredge
                }
            }
        }

        public static PhosphorusSkimmer PlaceSkimmer(SimGridState grid, SkimmerPool skimmerPool, SkimmerLocation skimmerLocation) {
            int tileIndex = skimmerLocation.TileIndex;
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            PhosphorusSkimmer skim = skimmerPool.Alloc(worldPos);
            skim.transform.Rotate(0, grid.Random.Next(0, 360), 0);
            return skim;
        }
    }
}