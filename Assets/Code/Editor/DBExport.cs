using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using Zavala.Data;
using Zavala.Sim;
using static Zavala.WorldAsset;

namespace Zavala.Editor {
    static public class DBExport {
        public const string ExportPath = "DBExport.json";

        private struct ActiveRange : ISerializedObject {
            public long Added;
            public long Updated;
            public long Deprecated;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("added", ref Added);
                ioSerializer.Serialize("updated", ref Updated);
                ioSerializer.Serialize("deprecated", ref Deprecated, 0L);
            }
        }

        private class Data : ISerializedObject {
            public MapData Map = new MapData();
            // TODO: public MapData
            //      width and height of total world
            //      one big array of tile information (type, elevation, region)
            //      iterate over all regions in WorldAsset
            //      for each tile: if not void, local coords -> global coords, place in global array
            //      implicitly stored as index in array -> can derive location from width and height, convert to x and y in worldspace
            //          could encode that conversion into each tile
            // public ObjectData
            //      array of buildings, obstacles, etc
            // TODO: policy values? market/budget config?
            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Object("map", ref Map);
            }
        }

        private class MapData : ISerializedObject {
            public uint Version;
            public uint Width;
            public uint Height;
            public List<RegionData> Regions = new List<RegionData>(RegionInfo.MaxRegions);


            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("version", ref Version);
                ioSerializer.Serialize("width", ref Width);
                ioSerializer.Serialize("height", ref Height);
                ioSerializer.ObjectArray("regions", ref Regions);

            }
        }

        private class RegionData : ISerializedObject {
            public string Id;
            public ActiveRange Date;
            public int Height;
            public int Width;
            public uint Elevation;

            public List<int> TileHeights = new();
            public List<TerrainType> TileTypes = new();
            public Dictionary<string, BuildingType> TileObjects = new();

            internal bool Included;
            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("id", ref Id);
                ioSerializer.Object("date", ref Date);
                ioSerializer.Serialize("height", ref Height);
                ioSerializer.Serialize("width", ref Width);
                ioSerializer.Serialize("elevation", ref Elevation);

                ioSerializer.Array("tileHeights", ref TileHeights);
                ioSerializer.EnumArray("tileTypes", ref TileTypes);
                ioSerializer.EnumMap("tileObjects", ref TileObjects);
            }
        }

        private class MapTileData : ISerializedObject {
            // index by array position in MapData
            public ushort Region;
            public ushort Height;
            public TerrainType Type;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("region", ref Region);
                ioSerializer.Serialize("height", ref Height);
                ioSerializer.Enum("terrainType", ref Type);
            }

        }

        private class ObjectData : ISerializedObject {
            public int Tile;
            public BuildingType Object; 

            public void Serialize(Serializer ioSerializer) {

            }

        }

        [MenuItem("Zavala/Export DB")]
        static public void Export() {

            //Log.Error("[DBExport] ExportDB Unimplemented!");
            //return;

            Data db;
            if (File.Exists(ExportPath)) {
                db = Serializer.ReadFile<Data>(ExportPath);
                Log.Msg("[DBExport] Read old database export from '{0}'", ExportPath);
            } else {
                db = new Data();
                Log.Msg("[DBExport] No database export found at '{0}': creating new", ExportPath);
                db.Map.Version = 0;
            }

            long nowTS = DateTime.UtcNow.ToFileTimeUtc();

            // TODO: gather region data
            WorldAsset world;
            try {
                world = ZavalaGame.SimGrid.WorldData;

            } catch (NullReferenceException e) {
                Log.Error(e.StackTrace);
                throw new NullReferenceException("[DBExport] WorldAsset not found! Use ExportDB while the game is running.");
            }

            if (db.Map.Version <= 0) {
                db.Map = new MapData();
                db.Map.Height = world.Height;
                db.Map.Width = world.Width;
            }

            HexGridSubregion totalRegion = new HexGridSubregion(ZavalaGame.SimGrid.HexSize);

            bool mapUpdated = false;

            // FIRST: initialize stuff
            for (int r = 0; r < world.Regions.Length; r++) {
                string currId = EnumLookup.RegionName[r];
                RegionData region = db.Map.Regions.Find(reg => reg.Id == currId);
                if (region == null) {
                    // new region
                    Log.Msg("[DBExport] Region {0} not found, creating new", currId);

                    region = new RegionData();
                    region.Id = currId;
                    region.Height = world.Regions[r].Region.Height;
                    region.Width = world.Regions[r].Region.Width;
                    region.Elevation = world.Regions[r].Elevation;

                    //HexGridSubregion subRegion = totalRegion.Subregion(world.Regions[r]);

                    ref TerrainTileInfo[] tiles = ref world.Regions[r].Region.Tiles;
                    for (int t = 0; t < tiles.Length; t++) {
                        region.TileHeights.Add(tiles[t].Height);
                        region.TileTypes.Add(FindTerrainType(tiles[t]));

                        if (t < world.Regions[r].Region.Buildings.Length) {
                            region.TileObjects.Add(world.Regions[r].Region.Buildings[t].LocalTileIndex.ToString(), world.Regions[r].Region.Buildings[t].Type);
                        }
                    }

                    region.Date.Added = nowTS;
                    region.Date.Updated = nowTS;
                    db.Map.Regions.Add(region);
                    mapUpdated = true;
                } else { // if region already in db
                    // update stuff!
                    Log.Msg("[DBExport] Region {0} found, checking...", region.Id);
                    bool updated = false;
                    
                    TryUpdate(ref region.Height, ref world.Regions[r].Region.Height, ref updated);
                    TryUpdate(ref region.Width, ref world.Regions[r].Region.Width, ref updated);
                    TryUpdate(ref region.Elevation, ref world.Regions[r].Elevation, ref updated);

                    ref TerrainTileInfo[] tiles = ref world.Regions[r].Region.Tiles;

                    if (region.TileHeights.Count != tiles.Length) {
                        // different lengths, rebuild everything
                        Log.Msg("\t[DBExport] Region {0} tile num changed, rebuilding", region.Id);
                        updated = true;
                        region.TileHeights = new();
                        region.TileTypes = new();
                        region.TileObjects = new();

                        for (int t = 0; t < tiles.Length; t++) {
                            region.TileHeights.Add(tiles[t].Height);
                            region.TileTypes.Add(FindTerrainType(tiles[t]));

                            if (t < world.Regions[r].Region.Buildings.Length) {
                                region.TileObjects.Add(world.Regions[r].Region.Buildings[t].LocalTileIndex.ToString(), world.Regions[r].Region.Buildings[t].Type);
                            }
                        }
                    } else {
                        // same lengths, try to update
                        for (int t = 0; t < tiles.Length; t++) {
                            // update tile heights
                            if (region.TileHeights[t] != tiles[t].Height) {
                                region.TileHeights[t] = tiles[t].Height;
                                Log.Msg("\t[DBExport] Region {0} tile {1} height updated", region.Id, t);
                                updated = true;
                            }
                            // update tile types
                            TerrainType readType = FindTerrainType(tiles[t]);
                            if (region.TileTypes[t] != readType) {
                                region.TileTypes[t] = readType;
                                Log.Msg("\t[DBExport] Region {0} tile {1} type updated", region.Id, t);
                                updated = true;
                            }
                            // update tile objects
                            // this is slow but it doesn't really matter
                            BuildingType typeOnTile = TypeOnWorldTile(t, world.Regions[r].Region.Buildings);
                            string key = t.ToString();
                            if (region.TileObjects.TryGetValue(key, out BuildingType dbType)) {
                                // ExportDB has an object at this point
                                // if the world has a different type there:
                                if (typeOnTile != dbType) {
                                    // replace the db
                                    region.TileObjects.Remove(key);
                                    if (typeOnTile != BuildingType.None) {
                                        region.TileObjects.Add(key, typeOnTile);
                                    }
                                    Log.Msg("\t[DBExport] Region {0} has different object on tile {1}, updated", region.Id, t);
                                    updated = true;
                                }

                            } else if (typeOnTile != BuildingType.None){
                                // exportDB does not have an object here, but world does
                                region.TileObjects.Add(key, typeOnTile);
                                Log.Msg("\t[DBExport] Region {0} has new object on tile {1}, updated", region.Id, t);

                            }
                        }
                    }
                    if (updated) {
                        Log.Msg("\t[DBExport] Region {0} updated, changing date", region.Id);
                        region.Date.Updated = nowTS;
                        mapUpdated = true;
                    } else {
                        Log.Msg("\t[DBExport] Region {0} was up-to-date :)", region.Id);

                    }
                }
                // is this only set to false manually?
                region.Included = true;
            }

            // NEXT: deprecate!
            // I don't think we'll really be deprecating regions but here's this anyway
            foreach (RegionData region in db.Map.Regions) {
                if (!region.Included && region.Date.Deprecated == 0) {
                    Log.Msg("[DBExport] Region {0} deprecated!", region.Id);
                    region.Date.Deprecated = nowTS;
                }
            }

            if (mapUpdated) {
                db.Map.Version++;
                Log.Msg("[DBExport] Some update found, incrementing version to {0}", db.Map.Version);
            }
            Serializer.WriteFile(db, ExportPath, OutputOptions.PrettyPrint, Serializer.Format.JSON);
        }
        
        private static TerrainType FindTerrainType(TerrainTileInfo info) {
            if ((info.Flags & TerrainFlags.IsWater) != 0) {
                if ((info.Flags & TerrainFlags.NonBuildable) != 0) {
                    return TerrainType.DeepWater;
                } else return TerrainType.Water;
            } else if (info.Category == TerrainCategory.Void) {
                return TerrainType.Void;
            } else return TerrainType.Land;
        }

        private static void TryUpdate(ref int dbVal, ref int currentVal, ref bool updated) {
            if (currentVal != dbVal) {
                dbVal = currentVal;
                if (!updated) {
                    updated = true;
                }
            }
        }
        private static void TryUpdate(ref uint dbVal, ref uint currentVal, ref bool updated) {
            if (currentVal != dbVal) {
                dbVal = currentVal;
                if (!updated) {
                    updated = true;
                }
            }
        }

        private static BuildingType TypeOnWorldTile(int localIdx, RegionAsset.BuildingData[] buildings) {
            foreach (RegionAsset.BuildingData b in buildings) {
                if (b.LocalTileIndex == localIdx) {
                    return b.Type;
                }
            }
            return BuildingType.None;
        }
    }
}