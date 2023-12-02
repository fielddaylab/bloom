using System.IO;
using System.Collections.Generic;
using BeauData;
using UnityEngine;
using Zavala.Sim;
using BeauUtil.Debugger;
using UnityEngine.TextCore.Text;
using BeauUtil.Variants;

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
            public JSON GroupLayer;

            public int HeightTileOffset;
            public int TypeTileOffset;
            public int ObjectTileOffset;
            public int GroupTileOffset;
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
                    case "Groups.tsx": {
                        data.GroupTileOffset = child["firstgid"].AsInt;
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
                    case "Groups": {
                        data.GroupLayer = child;
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
                        case 0: { // water
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
                        case 5: { // deep water
                            tileInfo.Category = TerrainCategory.Water;
                            tileInfo.Flags = TerrainFlags.IsWater;
                            tileInfo.Flags |= TerrainFlags.NonBuildable;
                                break;
                        }
                    }
                }
            }

            return buffer;
        }

        static private readonly string[] CharacterRegionSuffixes = new string[] {
            "Hill", "Forest", "Prairie", "Wetland", "Urban"
        };

        static public void ReadStaticConstructions(in TiledData data, RegionId regionId, HashSet<int> occupiedTileIndices, TerrainTileInfo[] tiles, out RegionAsset.BuildingData[] buildings, out RegionAsset.ModifierData[] modifiers, out RegionAsset.SpannerData[] spanners) {
            List<RegionAsset.BuildingData> buildingList = new List<RegionAsset.BuildingData>(8);
            List<RegionAsset.ModifierData> modifierList = new List<RegionAsset.ModifierData>(8);
            List<RegionAsset.SpannerData> spannerList = new List<RegionAsset.SpannerData>(8);

            JSON objectArray = data.ObjectLayer["objects"];
            foreach(var obj in objectArray.Children) {
                int gid = obj["gid"].AsInt;
                if (gid > 0) {
                    int objType = gid - data.ObjectTileOffset;
                    int pos = HexToTile(data, ObjectPosToHex(data, obj));
                    string scriptName = obj["name"].AsString;
                    JSON props = FlattenProperties(obj["properties"]);
                    switch (objType) {
                        case 0: { // grain
                            buildingList.Add(new RegionAsset.BuildingData() {
                                LocalTileIndex = (ushort) pos,
                                Type = BuildingType.GrainFarm,
                                ScriptName = scriptName,
                                LocationName = string.Format("location.{0}.name", scriptName),
                                CharacterId = string.Concat("grain", CharacterRegionSuffixes[(int) regionId])
                            });
                            occupiedTileIndices.Add(pos);
                            tiles[pos].Flags |= TerrainFlags.TopHidden;
                            tiles[pos].Flags |= TerrainFlags.IsOccupied;
                            break;
                        }
                        case 1: { // dairy
                            buildingList.Add(new RegionAsset.BuildingData() {
                                LocalTileIndex = (ushort) pos,
                                Type = BuildingType.DairyFarm,
                                ScriptName = scriptName,
                                LocationName = string.Format("location.{0}.name", scriptName),
                                CharacterId = string.Concat("cafo", CharacterRegionSuffixes[(int) regionId])
                            });
                            occupiedTileIndices.Add(pos);
                            tiles[pos].Flags |= TerrainFlags.TopHidden;
                            tiles[pos].Flags |= TerrainFlags.IsOccupied;
                            break;
                        }
                        case 2: { // city
                            buildingList.Add(new RegionAsset.BuildingData() {
                                LocalTileIndex = (ushort) pos,
                                Type = BuildingType.City,
                                ScriptName = scriptName,
                                LocationName = string.Format("location.{0}.name", scriptName),
                                CharacterId = string.Concat("city", CharacterRegionSuffixes[(int) regionId]),
                                AdditionalData = new SerializedVariant(props?["Population"]?.AsInt ?? 0)
                            });
                            occupiedTileIndices.Add(pos);
                            tiles[pos].Flags |= TerrainFlags.TopHidden;
                            tiles[pos].Flags |= TerrainFlags.IsOccupied;
                            break;
                        }
                        case 3: { // Tree
                            modifierList.Add(new RegionAsset.ModifierData() {
                                LocalTileIndex = (ushort) pos,
                                ScriptName = scriptName,
                                Modifier = RegionAsset.TerrainModifier.Tree
                            });
                            tiles[pos].Flags |= TerrainFlags.NonBuildable;
                            tiles[pos].Flags |= TerrainFlags.TopHidden;
                            tiles[pos].Flags |= TerrainFlags.IsOccupied;
                            break;
                        }
                        case 4: { // Rock
                            modifierList.Add(new RegionAsset.ModifierData() {
                                LocalTileIndex = (ushort) pos,
                                ScriptName = scriptName,
                                Modifier = RegionAsset.TerrainModifier.Rock
                            });
                            tiles[pos].Flags |= TerrainFlags.NonBuildable;
                            tiles[pos].Flags |= TerrainFlags.TopHidden;
                            tiles[pos].Flags |= TerrainFlags.IsOccupied;
                            break;
                        }
                        case 6: { // road
                            occupiedTileIndices.Add(pos);
                            break;
                        }
                        case 7: { // export depot
                                buildingList.Add(new RegionAsset.BuildingData() {
                                    LocalTileIndex = (ushort)pos,
                                    Type = BuildingType.ExportDepot,
                                    ScriptName = scriptName,
                                    LocationName = string.Format("location.{0}.name", scriptName),
                                });
                                occupiedTileIndices.Add(pos);
                                // tiles[pos].Flags |= TerrainFlags.TopHidden;
                                tiles[pos].Flags |= TerrainFlags.IsOccupied;
                                break;
                        }
                        case 8: { // toll booth
                                spannerList.Add(new RegionAsset.SpannerData() {
                                    LocalTileIndex = (ushort)pos,
                                    Type = BuildingType.TollBooth,
                                    ScriptName = scriptName
                                });
                                tiles[pos].Flags |= TerrainFlags.IsToll;
                                break;
                            }
                        case 9:
                            { // temporary obstruction
                                buildingList.Add(new RegionAsset.BuildingData()
                                {
                                    LocalTileIndex = (ushort)pos,
                                    Type = BuildingType.TempObstruction,
                                    ScriptName = scriptName
                                });
                                tiles[pos].Flags |= TerrainFlags.IsOccupied;
                                tiles[pos].Flags |= TerrainFlags.NonBuildable;
                                break;
                            }
                    }
                }
            }
            buildings = buildingList.ToArray();
            modifiers = modifierList.ToArray();
            spanners = spannerList.ToArray();
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

        static public void ReadWaterGroups(in TiledData data, TerrainTileInfo[] tiles, out ushort[] groupTileIndices, out RegionAsset.WaterGroupRange[] ranges) {
            if (data.GroupLayer == null) {
                groupTileIndices = null;
                ranges = null;
                return;
            }

            List<ushort>[] groups = new List<ushort>[4];
            for(int i = 0; i < 4; i++) {
                groups[i] = new List<ushort>();
            }

            JSON groupArray = data.GroupLayer["data"];
            for(int i = 0; i < groupArray.Count; i++) {
                int group = groupArray[i].AsInt;
                if (group > 0) {
                    group = group - data.GroupTileOffset;
                    int tileIndex = IndexToTile(data, i);
                    tiles[tileIndex].Flags |= TerrainFlags.IsInGroup;
                    groups[group].Add((ushort) tileIndex);
                }
            }

            List<ushort> groupIndices = new List<ushort>(64);
            List<RegionAsset.WaterGroupRange> groupData = new List<RegionAsset.WaterGroupRange>();

            for(int i = 0; i < 4; i++) {
                if (groups[i].Count > 0) {
                    groups[i].Sort();
                    groupData.Add(new RegionAsset.WaterGroupRange() {
                        Offset = (ushort) groupIndices.Count,
                        Length = (ushort) groups[i].Count
                    });
                    groupIndices.AddRange(groups[i]);
                }
            }

            groupTileIndices = groupIndices.ToArray();
            ranges = groupData.ToArray();
        }

        static public void AnalyzeBorderData(in TiledData data, TerrainTileInfo[] tiles, out RegionAsset.BorderPoint[] borderPoints, out ushort[] edgeVisualUpdateSet) {
            List<RegionAsset.BorderPoint> borders = new List<RegionAsset.BorderPoint>();
            List<ushort> visualUpdateEdges = new List<ushort>();

            HexGridSize.IndexEnumerator idxEnumerator = data.GridSize.GetEnumerator();
            while(idxEnumerator.MoveNext()) {
                int idx = idxEnumerator.Current;

                ref TerrainTileInfo tileInfo = ref tiles[idx];
                if (tileInfo.Category == TerrainCategory.Void) {
                    continue;
                }

                HexVector pos = data.GridSize.FastIndexToPos(idx);
                TileAdjacencyMask borderMask = default;

                for(TileDirection dir = TileDirection.Self + 1; dir < TileDirection.COUNT; dir++) {
                    if (!data.GridSize.IsValidPosOffset(pos, dir, out HexVector next) || tiles[data.GridSize.FastPosToIndex(next)].Category == TerrainCategory.Void) {
                        borderMask |= dir;
                    }
                }

                if (!borderMask.IsEmpty) {
                    tileInfo.Flags |= TerrainFlags.IsBorder;
                    borders.Add(new RegionAsset.BorderPoint() {
                        LocalTileIndex = (ushort) idx,
                        Borders = borderMask
                    });

                    if (IsTowardsCamera(borderMask) && (tileInfo.Flags & TerrainFlags.IsWater) != 0) {
                        visualUpdateEdges.Add((ushort) idx);
                    }
                }
            }

            borderPoints = borders.ToArray();
            edgeVisualUpdateSet = visualUpdateEdges.ToArray();
        }

        private const TerrainFlags IgnoreCullingTerrainFlags = TerrainFlags.IsWater;

        static public void AnalyzeBaseCullingData(in TiledData data, TerrainTileInfo[] tiles) {
            int culled = 0;

            HexGridSize.IndexEnumerator idxEnumerator = data.GridSize.GetEnumerator();
            while (idxEnumerator.MoveNext()) {
                int idx = idxEnumerator.Current;

                ref TerrainTileInfo tileInfo = ref tiles[idx];
                if (tileInfo.Category == TerrainCategory.Void || (tileInfo.Flags & IgnoreCullingTerrainFlags) != 0) {
                    continue;
                }

                HexVector pos = data.GridSize.FastIndexToPos(idx);
                TileAdjacencyMask lowerMask = default;

                for (TileDirection dir = TileDirection.Self + 1; dir < TileDirection.COUNT; dir++) {
                    if (IsTowardsCamera(dir)) {
                        if (data.GridSize.IsValidPosOffset(pos, dir, out HexVector next)) {
                            var neighbor = tiles[data.GridSize.FastPosToIndex(next)];
                            if (neighbor.Category == TerrainCategory.Void || neighbor.Category == TerrainCategory.Water) {
                                lowerMask |= dir;
                            } else if (neighbor.Height < tileInfo.Height) {
                                lowerMask |= dir;
                            }
                        } else {
                            lowerMask |= dir;
                        }
                    }
                }

                if (lowerMask.IsEmpty) {
                    tileInfo.Flags |= TerrainFlags.CullBase;
                    culled++;
                }
            }

            Log.Msg("culling {0} tile bases", culled);
        }

        static private bool IsTowardsCamera(TileAdjacencyMask direction) {
            return direction.Has(TileDirection.NW) || direction.Has(TileDirection.SW) || direction.Has(TileDirection.S);
        }

        static private bool IsTowardsCamera(TileDirection direction) {
            switch (direction) {
                case TileDirection.NW:
                case TileDirection.SW:
                case TileDirection.S:
                    return true;

                default:
                    return false;
            }
        }

        static private JSON FlattenProperties(JSON sourceArr) {
            if (sourceArr == null || sourceArr.Count == 0) {
                return null;
            }

            JSON obj = JSON.CreateObject();
            foreach(var element in sourceArr.Children) {
                obj.Add(element["name"].AsString, JSON.CreateValue(element["value"].AsString));
            }
            return obj;
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