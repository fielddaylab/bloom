using System;
using Leaf;
using UnityEngine;
using Zavala.Sim;

namespace Zavala {
    [CreateAssetMenu(menuName = "Zavala/Region Asset")]
    public sealed class RegionAsset : ScriptableObject {
        #region Types

        public enum BuildingType : byte {
            GrainFarm,
            DairyFarm,
            City
        }

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

        [Header("Visuals")]
        public int PaletteIndex;
        public LeafAsset LeafScript;

        [Header("Source File Info")]
        public string SourceFilePath;
    }
}