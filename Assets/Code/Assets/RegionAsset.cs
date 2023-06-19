using UnityEngine;
using Zavala.Sim;

namespace Zavala {
    [CreateAssetMenu(menuName = "Zavala/Region Asset")]
    public class RegionAsset : ScriptableObject {
        [Header("Dimensions")]
        public int Width;
        public int Height;

        [Header("Tile Information")]
        public TerrainTileInfo[] Tiles;
    }
}