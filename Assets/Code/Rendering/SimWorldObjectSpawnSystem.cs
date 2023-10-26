using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Building;
using Zavala.Economy;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.World
{

    [SysUpdate(GameLoopPhase.LateUpdate, 0)]
    public sealed class SimWorldObjectSpawnSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState, BuildingPools>
    {
        public override bool HasWork() {
            if (base.HasWork()) {
                return m_StateA.NewRegions != 0;
            }
            return false;
        }

        public override void ProcessWork(float deltaTime) {
            PseudoRandom random = new PseudoRandom(m_StateB.WorldData.name);

            SimWorldSpawnBuffer buff = m_StateA.Spawns;
            while(buff.QueuedBuildings.TryPopFront(out var spawn)) {
                HexVector pos = m_StateB.HexSize.FastIndexToPos(spawn.TileIndex);
                Vector3 worldPos = HexVector.ToWorld(pos, m_StateB.Terrain.Info[spawn.TileIndex].Height, m_StateA.WorldSpace);
                RegionPrefabPalette palette = m_StateA.Palettes[spawn.RegionIndex];
                GameObject building = null;
                switch (spawn.Data) {
                    case BuildingType.City: {
                            building = GameObject.Instantiate(palette.City, worldPos, Quaternion.identity);
                            break;
                        }
                    case BuildingType.DairyFarm: {
                            building = GameObject.Instantiate(palette.DairyFarm, worldPos, Quaternion.identity);
                            break;
                        }
                    case BuildingType.GrainFarm: {
                            building = GameObject.Instantiate(palette.GrainFarm, worldPos, Quaternion.identity);
                            break;
                        }
                    case BuildingType.ExportDepot: {
                            building = GameObject.Instantiate(palette.ExportDepot, worldPos, Quaternion.identity);
                            break;
                        }
                }
                Assert.NotNull(building);
                EventActorUtility.RegisterActor(building.GetComponent<EventActor>(), spawn.Id);
            }

            while (buff.QueuedModifiers.TryPopFront(out var spawn)) {
                HexVector pos = m_StateB.HexSize.FastIndexToPos(spawn.TileIndex);
                Vector3 worldPos = HexVector.ToWorld(pos, m_StateB.Terrain.Info[spawn.TileIndex].Height, m_StateA.WorldSpace);
                RegionPrefabPalette palette = m_StateA.Palettes[spawn.RegionIndex];
                GameObject building = null;
                switch (spawn.Data) {
                    case RegionAsset.TerrainModifier.Tree: {
                            building = GameObject.Instantiate(palette.Tree[random.Int(palette.Tree.Length, spawn.RegionIndex)], worldPos, Quaternion.identity);
                            break;
                        }
                    case RegionAsset.TerrainModifier.Rock: {
                            building = GameObject.Instantiate(palette.Rock[random.Int(palette.Rock.Length, spawn.RegionIndex)], worldPos, Quaternion.identity);
                            break;
                        }
                }
                Assert.NotNull(building);
                EventActorUtility.RegisterActor(building.GetComponent<EventActor>(), spawn.Id);
            }

            // Spawn External Supplier
            Vector3 externalWorldPos = new Vector3(25, 0, 5); // top-right of screen
            GameObject externalDepot = GameObject.Instantiate(m_StateA.ExternalExportDepotPrefab.gameObject, externalWorldPos, Quaternion.identity);
            GameObject externalSupplier = GameObject.Instantiate(m_StateA.ExternalSupplierPrefab.gameObject, externalWorldPos, Quaternion.identity);

            ExternalState externalState = Game.SharedState.Get<ExternalState>();
            externalState.ExternalDepot = externalDepot.GetComponent<ResourceSupplierProxy>();
            externalState.ExternalSupplier = externalSupplier.GetComponent<ResourceSupplier>();

            /*
            Assert.NotNull(externalDepot);
            EventActorUtility.RegisterActor(externalDepot.GetComponent<EventActor>(), "");
            */
            /*
            Assert.NotNull(externalSupplier);
            EventActorUtility.RegisterActor(externalSupplier.GetComponent<EventActor>(), "");
            */
        }
    }
}