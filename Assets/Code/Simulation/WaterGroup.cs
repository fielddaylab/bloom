using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.Sim {
    public struct WaterGroupInfo {
        public const int MaxGroups = 16;
        public const int MaxTilesPerGroup = 64;

        public ushort RegionId;
        public ushort TileCount;
        public unsafe fixed ushort TileIndices[MaxTilesPerGroup];
    }
}