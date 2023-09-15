using System.IO;
using System.Collections.Generic;
using BeauData;
using UnityEngine;
using Zavala.Sim;
using UnityEngine.Rendering.Universal;

namespace Zavala.Editor {
    static public class RegionImport {
        public struct TiledData {
            public JSON Json;
            public HexGridSize GridSize;
            public Vector2 PixelSize;
            public Vector2 GridCellSize;

            public JSON HeightLayer;
            public JSON TypeLayer;
            public JSON ObjectLayer;

            public int HeightTileOffset;
            public int TypeTileOffset;
            public int ObjectTileOffset;
        }

        public enum ErrorCode {
            Success = 0,
            FileMissing,
            InvalidJSON,
            IncorrectSettings,
        }

        static public ErrorCode TryReadTiledJSON(string filePath, out TiledData data) {
            JSON json;
            data = default;
            
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                return ErrorCode.FileMissing;
            }

            try {
                using (FileStream stream = File.OpenRead(filePath)) {
                    json = JSON.Parse(stream);
                }
            } catch {
                return ErrorCode.InvalidJSON;
            }

            if (json["type"].AsString != "map") {
                return ErrorCode.IncorrectSettings;
            }

            if (json["orientation"].AsString != "hexagonal") {
                return ErrorCode.IncorrectSettings;
            }

            if (json["staggerindex"].AsString != "even") {
                return ErrorCode.IncorrectSettings;
            }

            if (json["staggeraxis"].AsString != "x") {
                return ErrorCode.IncorrectSettings;
            }

            if (json["infinite"].AsBool) {
                return ErrorCode.IncorrectSettings;
            }

            data.Json = json;
            return ErrorCode.Success;
        }

        static public void ReadMapSize(ref TiledData data) {
            data.GridSize = new HexGridSize(data.Json["width"].AsUInt, data.Json["height"].AsUInt);

            float cellWidth = data.Json["tilewidth"].AsFloat;
            float cellHeight = data.Json["tileheight"].AsFloat;
            float cellSideLength = data.Json["hexsidelength"].AsFloat;

            data.GridCellSize = new Vector2((cellWidth + cellSideLength) / 2, cellHeight);
            data.PixelSize = new Vector2(data.GridSize.Width * data.GridCellSize.x, (data.GridSize.Height + 0.5f) * data.GridCellSize.y);
        }

        static public void ReadTileOffsets(ref TiledData data) {
            JSON tilesetJson = data.Json["tilesets"];
            foreach(var child in tilesetJson.Children) {
                string source = child["source"].AsString;
                switch(source) {
                    case "Height.tsx": {
                        data.HeightTileOffset = child["firstgid"].AsInt;
                        break;
                    }
                    case "Type.tsx": {
                        data.TypeTileOffset = child["firstgid"].AsInt;
                        break;
                    }
                    case "Objects.tsx": {
                        data.ObjectTileOffset = child["firstgid"].AsInt;
                        break;
                    }
                }
            }
        }

        static public void ReadLayers(ref TiledData data) {
            JSON layersJson = data.Json["layers"];
            foreach(var child in layersJson.Children) {
                string name = child["name"].AsString;
                switch(name) {
                    case "Tile Type": {
                        data.TypeLayer = child;
                        break;
                    }
                    case "Tile Height": {
                        data.HeightLayer = child;
                        break;
                    }
                    case "Objects": {
                        data.ObjectLayer = child;
                        break;
                    }
                }
            }
        }

        static public TerrainTileInfo[] ReadTerrain(in TiledData data, int heightScale) {
            TerrainTileInfo[] buffer = new TerrainTileInfo[data.GridSize.Size];
            for(int i = 0; i < buffer.Length; i++) {
                buffer[i] = TerrainTileInfo.Invalid;
            }

            // read heights
            JSON heightArray = data.HeightLayer["data"];
            for(int i = 0; i < heightArray.Count; i++) {
                int height = heightArray[i].AsInt;
                if (height > 0) {
                    height = (height - data.HeightTileOffset) * heightScale;
                    int tileIndex = IndexToTile(data, i);
                    buffer[tileIndex].Height = (ushort) height;
                }
            }

            JSON typeArray = data.TypeLayer["data"];
            for(int i = 0; i < typeArray.Count; i++) {
                int type = typeArray[i].AsInt;
                if (type > 0) {
                    type = type - data.TypeTileOffset;
                    int tileIndex = IndexToTile(data, i);
                    ref TerrainTileInfo tileInfo = ref buffer[tileIndex];
                    switch (type) {
                        case 0: {
                            tileInfo.Category = TerrainCategory.Water;
                            tileInfo.Flags = TerrainFlags.IsWater;
                            break;
                        }

                        case 1: {
                            tileInfo.Category = TerrainCategory.Land;
                            tileInfo.SubCategory = 0;
                            tileInfo.Flags = 0;
                            break;
                        }

                        case 2: {
                            tileInfo.Category = TerrainCategory.Land;
                            tileInfo.SubCategory = 1;
                            tileInfo.Flags = 0;
                            break;
                        }

                        case 3: {
                            tileInfo.Category = TerrainCategory.Land;
                            tileInfo.SubCategory = 2;
                            tileInfo.Flags = 0;
                            break;
                        }

                        case 4: {
                            tileInfo.Category = TerrainCategory.Land;
                            tileInfo.SubCategory = 3;
                            tileInfo.Flags = 0;
                            break;
                        }
                    }
                }
            }

            return buffer;
        }

        static public void ReadStaticConstructions(in TiledData data, HashSet<int> occupiedTileIndices, TerrainTileInfo[] tiles, out RegionAsset.BuildingData[] buildings, out RegionAsset.ModifierData[] modifiers) {
            List<RegionAsset.BuildingData> buildingList = new List<RegionAsset.BuildingData>(8);
            List<RegionAsset.ModifierData> modifierList = new List<RegionAsset.ModifierData>(8);
            JSON objectArray = data.ObjectLayer["objects"];
            foreach(var obj in objectArray.Children) {
                int gid = obj["gid"].AsInt;
                if (gid > 0) {
                    int objType = gid - data.ObjectTileOffset;
                    int pos = HexToTile(data, ObjectPosToHex(data, obj));
                    string scriptName = obj["name"].AsString;
                    switch (objType) {
                        case 0: { // grain
                            buildingList.Add(new RegionAsset.BuildingData() {
                                LocalTileIndex = (ushort) pos,
                                Type = RegionAsset.BuildingType.GrainFarm,
                                ScriptName = scriptName
                            });
                            occupiedTileIndices.Add(pos);
                            break;
                        }
                        case 1: { // dairy
                            buildingList.Add(new RegionAsset.BuildingData() {
                                LocalTileIndex = (ushort) pos,
                                Type = RegionAsset.BuildingType.DairyFarm,
                                ScriptName = scriptName
                            });
                            occupiedTileIndices.Add(pos);
                            break;
                        }
                        case 2: { // city
                            buildingList.Add(new RegionAsset.BuildingData() {
                                LocalTileIndex = (ushort) pos,
                                Type = RegionAsset.BuildingType.City,
                                ScriptName = scriptName
                            });
                            occupiedTileIndices.Add(pos);
                            break;
                        }
                        case 3: {
                            modifierList.Add(new RegionAsset.ModifierData() {
                                LocalTileIndex = (ushort) pos,
                                ScriptName = scriptName,
                                Modifier = RegionAsset.TerrainModifier.Tree
                            });
                            tiles[pos].Flags |= TerrainFlags.NonBuildable;
                            break;
                        }
                        case 4: {
                            modifierList.Add(new RegionAsset.ModifierData() {
                                LocalTileIndex = (ushort) pos,
                                ScriptName = scriptName,
                                Modifier = RegionAsset.TerrainModifier.Rock
                            });
                            tiles[pos].Flags |= TerrainFlags.NonBuildable;
                            break;
                        }
                        case 6: { // road
                            occupiedTileIndices.Add(pos);
                            break;
                        }
                    }
                }
            }
            buildings = buildingList.ToArray();
            modifiers = modifierList.ToArray();
        }

        static public RegionAsset.RoadData[] ReadRoads(in TiledData data, HashSet<int> occupiedTileIndices) {
            List<RegionAsset.RoadData> roads = new List<RegionAsset.RoadData>(8);
            JSON objectArray = data.ObjectLayer["objects"];
            foreach (var obj in objectArray.Children) {
                int gid = obj["gid"].AsInt;
                if (gid <= 0) {
                    continue;
                }

                int objType = gid - data.ObjectTileOffset;
                if (objType != 6) {
                    continue;
                }

                int tileIndex = HexToTile(data, ObjectPosToHex(data, obj));
                TileAdjacencyMask mask = new TileAdjacencyMask();
                for (TileDirection dir = TileDirection.Self + 1; dir < TileDirection.COUNT; dir++) {
                    if (data.GridSize.IsValidIndexOffset(tileIndex, dir)) {
                        int adj = data.GridSize.OffsetIndexFrom(tileIndex, dir);
                        if (occupiedTileIndices.Contains(adj)) {
                            mask |= dir;
                        }
                    }
                }

                roads.Add(new RegionAsset.RoadData() {
                    LocalTileIndex = (ushort) tileIndex,
                    Adjacency = mask
                });
            }

            return roads.ToArray();
        }

        static public RegionAsset.PointData[] ReadScriptPoints(in TiledData data) {
            List<RegionAsset.PointData> points = new List<RegionAsset.PointData>(8);
            JSON objectArray = data.ObjectLayer["objects"];
            foreach (var obj in objectArray.Children) {
                if (!obj["point"].AsBool) {
                    continue;
                }

                points.Add(new RegionAsset.PointData() {
                    LocalTileIndex = (ushort) HexToTile(data, ObjectPosToHex(data, obj)),
                    ScriptName = obj["name"].AsString
                });
            }
            return points.ToArray();
        }

        #region Space Conversion

        static public HexVector PixelToHex(in TiledData data, Vector2 pixelLocation) {
            pixelLocation.y = data.PixelSize.y - pixelLocation.y;
            int x = (int) Mathf.Floor(pixelLocation.x / data.GridCellSize.x);
            int y = (int) Mathf.Floor((pixelLocation.y / data.GridCellSize.y) - (x & 1) * 0.5f);
            return HexVector.FromGrid(x, y);
        }

        static public HexVector ObjectPosToHex(in TiledData data, JSON objectJson) {
            Vector2 pixelLocation = new Vector2(objectJson["x"].AsFloat, objectJson["y"].AsFloat);
            pixelLocation.x += objectJson["width"].AsFloat / 2;
            pixelLocation.y -= objectJson["height"].AsFloat / 2;
            return PixelToHex(data, pixelLocation);
        }

        static public int HexToTile(in TiledData data, HexVector hex) {
            return data.GridSize.FastPosToIndex(hex);
        }

        static public int IndexToTile(in TiledData data, int index) {
            data.GridSize.FastIndexToPos(index, out int x, out int y);
            y = (int) data.GridSize.Height - 1 - y;
            return data.GridSize.FastPosToIndex(x, y);
        }

        #endregion // Space Conversion
    }
}