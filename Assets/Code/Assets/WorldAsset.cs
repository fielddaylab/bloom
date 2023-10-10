using System;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala {
    [CreateAssetMenu(menuName = "Zavala/World Asset")]
    public sealed class WorldAsset : ScriptableObject {
        [Serializable]
        public struct OffsetRegion {
            public RegionAsset Region;
            public uint X;
            public uint Y;
            public uint Elevation;
        }

        public OffsetRegion[] Regions;
        public RegionPrefabPalette[] Palettes;
        public RegionPrefabPalette DefaultPalette;

        public uint Width;
        public uint Height;

        public RegionPrefabPalette Palette(int index) {
            RegionPrefabPalette palette;
            return index < 0 || index >= Palettes.Length || (palette = Palettes[index]) == null ? DefaultPalette : palette;
        }
    }

    static public class WorldUtility {
    }
}