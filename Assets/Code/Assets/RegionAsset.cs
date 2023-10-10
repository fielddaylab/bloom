using System;
using BeauUtil;
using Leaf;
using UnityEngine;
using Zavala.Sim;

namespace Zavala {
    public enum RegionId
    {
        Hillside,
        Prairie,
        Region3,
        Region4,
        Region5
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
        Obstacle
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
            public string ScriptName;
            public BuildingType Type;
        }

        [Serializable]
        public struct PointData {
            public ushort LocalTileIndex;
            public string ScriptName;
        }

        [Serializable]
        public struct RoadData {
            public ushort LocalTileIndex;
            public TileAdjacencyMask Adjacency;
        }

        [Serializable]
        public struct ModifierData {
            public ushort LocalTileIndex;
            public string ScriptName;
            public TerrainModifier Modifier;
        }

        [Serializable]
        public struct WaterGroupRange {
            public ushort Offset;
            public ushort Length;
        }

        #endregion // Types

        [Header("Dimensions")]
        public int Width;
        public int Height;

        [Header("Tile Information")]
        public TerrainTileInfo[] Tiles = Array.Empty<TerrainTileInfo>();
        public BuildingData[] Buildings = Array.Empty<BuildingData>();
        public PointData[] Points = Array.Empty<PointData>();
        public RoadData[] Roads = Array.Empty<RoadData>();
        public ModifierData[] Modifiers = Array.Empty<ModifierData>();

        [Header("Groups")]
        public ushort[] WaterGroupLocalIndices = Array.Empty<ushort>();
        public WaterGroupRange[] WaterGroups = Array.Empty<WaterGroupRange>();

        [Header("Visuals")]
        public LeafAsset LeafScript;

        [Header("Id")]
        public RegionId Id;

        [Header("Source File Info")]
        public string SourceFilePath;
    }
}