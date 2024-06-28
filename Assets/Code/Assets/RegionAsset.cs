using System;
using BeauUtil;
using BeauUtil.Variants;
using Leaf;
using UnityEngine;
using Zavala.Audio;
using Zavala.Sim;

namespace Zavala {
    public enum RegionId
    {
        Hillside,
        Forest,
        Prairie,
        Wetland,
        Urban
    }

    public enum BuildingType : byte {
        None,
        GrainFarm,
        DairyFarm,
        City,
        Road,
        Digester,
        Storage,
        Skimmer,
        Obstacle,
        ExportDepot,
        TollBooth,
        TempObstruction,
        SkimmerLocation,
        DigesterBroken
    }

    [CreateAssetMenu(menuName = "Zavala/Region Asset")]
    public sealed class RegionAsset : ScriptableObject {
        #region Types

        public enum TerrainModifier : byte {
            Tree,
            Rock
        }

        [Serializable]
        public struct BuildingData {
            public ushort LocalTileIndex;
            public SerializedHash32 ScriptName;
            public BuildingType Type;
            public SerializedHash32 LocationName;
            public SerializedHash32 CharacterId;
            public SerializedVariant AdditionalData;
        }

        [Serializable]
        public struct PointData {
            public ushort LocalTileIndex;
            public SerializedHash32 ScriptName;
        }

        [Serializable]
        public struct RoadData {
            public ushort LocalTileIndex;
            public TileAdjacencyMask Adjacency;
        }

        [Serializable]
        public struct ModifierData {
            public ushort LocalTileIndex;
            public SerializedHash32 ScriptName;
            public TerrainModifier Modifier;
        }


        [Serializable]
        public struct SpannerData
        {
            public ushort LocalTileIndex;
            public SerializedHash32 ScriptName;
            public BuildingType Type;
        }

        [Serializable]
        public struct WaterSoundEmitter {
            public ushort LocalTileIndex;
            public ushort Variant;
        }

        [Serializable]
        public struct WaterGroupRange {
            public ushort Offset;
            public ushort Length;
        }

        [Serializable]
        public struct BorderPoint {
            public ushort LocalTileIndex;
            public TileAdjacencyMask Borders;
            public TileCorner SharedCornerCCW;
            public TileCorner SharedCornerCW;
        }

        #endregion // Types

        [Header("Dimensions")]
        public int Width;
        public int Height;

        [Header("Tile Information")]
        public TerrainTileInfo[] Tiles = Array.Empty<TerrainTileInfo>(); // type, elevation, region index
        public BuildingData[] Buildings = Array.Empty<BuildingData>(); // cities, farms, 
        public PointData[] Points = Array.Empty<PointData>();
        public RoadData[] Roads = Array.Empty<RoadData>();
        public ModifierData[] Modifiers = Array.Empty<ModifierData>(); // obstacles
        public BorderPoint[] Borders = Array.Empty<BorderPoint>();
        public SpannerData[] Spanners = Array.Empty<SpannerData>(); // tollbooth
        public ushort[] EdgeVisualUpdateSet = Array.Empty<ushort>();
        public WaterSoundEmitter[] WaterEmitters = Array.Empty<WaterSoundEmitter>();

        [Header("Groups")]
        public ushort[] WaterGroupLocalIndices = Array.Empty<ushort>();
        public WaterGroupRange[] WaterGroups = Array.Empty<WaterGroupRange>();

        [Header("Visuals")]
        public Color32 OutlineColor;

        [Header("Audio")]
        public AmbientSfxConfig Ambience;

        [Header("Id")]
        public RegionId Id;

        [Header("Source File Info")]
        public string SourceFilePath;
    }
}