using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Zavala.Sim {

    #region Static Info

    /// <summary>
    /// Data for each tile indicating its terrain type.
    /// </summary>
    [Serializable]
    public struct TerrainTileInfo {
        public ushort RegionIndex;
        public TerrainCategory Category;
        public ushort SubCategory;
        public TerrainFlags Flags;
        public ushort Height;

        static public readonly TerrainTileInfo Invalid = new TerrainTileInfo() {
            RegionIndex = Tile.InvalidIndex16,
            Category = TerrainCategory.Void,
            SubCategory = 0,
            Flags = TerrainFlags.NonBuildable,
            Height = ushort.MaxValue
        };
    }

    /// <summary>
    /// General terrain categories.
    /// </summary>
    public enum TerrainCategory : ushort {
        Void,
        Land,
        Water
    }

    /// <summary>
    /// Terrain behavior flags.
    /// </summary>
    [Flags]
    public enum TerrainFlags : ushort {
        NonBuildable = 0x01,
        IsBorder = 0x02,
        BorderingWater = 0x04, // Needed? Seems to already check during phosphorus flow...
        IsWater = 0x08,
        TopHidden = 0x10,
        IsOccupied = 0x20,
        IsInGroup = 0x40,
        CullBase = 0x80, // if the base of this tile would be hidden by surrounding tiles
    }

    #endregion // Static Info

    /// <summary>
    /// Terrain information utility functions.
    /// </summary>
    static public class TerrainInfo {
        /// <summary>
        /// Retrieves the maximum height of the given terrain grid.
        /// </summary>
        static public ushort GetMaximumHeight(SimBuffer<TerrainTileInfo> terrainBuffer, in HexGridSize gridSize) {
            uint max = 0;
            foreach(var index in gridSize) {
                if (terrainBuffer[index].Category != TerrainCategory.Void) {
                    max = Math.Max(max, terrainBuffer[index].Height);
                }
            }
            return (ushort) max;
        }

        /// <summary>
        /// Retrieves the maximum height of the given terrain subregion.
        /// </summary>
        static public ushort GetMaximumHeight(SimBuffer<TerrainTileInfo> terrainBuffer, in HexGridSubregion gridRegion, ushort regionIndex) {
            uint max = 0;
            foreach (var index in gridRegion) {
                if (terrainBuffer[index].RegionIndex == regionIndex && terrainBuffer[index].Category != TerrainCategory.Void) {
                    max = Math.Max(max, terrainBuffer[index].Height);
                }
            }
            return (ushort) max;
        }

        /// <summary>
        /// Extracts height and region data from the terrain buffer to the given height/region buffers.
        /// </summary>
        static public void ExtractHeightAndRegionData(SimBuffer<TerrainTileInfo> terrainBuffer, SimBuffer<ushort> heightBuffer, SimBuffer<ushort> regionBuffer) {
            if (terrainBuffer.Length != heightBuffer.Length || terrainBuffer.Length != regionBuffer.Length) {
                Assert.Fail("buffer lengths do not match");
            }

            for(int i = 0; i < terrainBuffer.Length; i++) {
                heightBuffer[i] = terrainBuffer[i].Height;
                regionBuffer[i] = terrainBuffer[i].RegionIndex;
            }
        }
    }
}