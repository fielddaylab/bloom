using System;
using UnityEngine;
using Zavala.Sim;

namespace Zavala {
    [CreateAssetMenu(menuName = "Zavala/World Asset")]
    public sealed class WorldAsset : ScriptableObject {
        [Serializable]
        public struct OffsetRegion {
            public RegionAsset Region;
            public uint X;
            public uint Y;
        }

        public OffsetRegion[] Regions;
        public uint Width;
        public uint Height;
    }

    static public class WorldUtility {
    }
}