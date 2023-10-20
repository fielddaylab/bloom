using UnityEngine;

namespace Zavala.World {
    [CreateAssetMenu(menuName = "Zavala/Region Prefab Palette")]
    public sealed class RegionPrefabPalette : ScriptableObject {
        [Header("Tiles")]
        public TileInstance GroundTile;
        [Tooltip("Optimized ground tile with no pillar. Used only when it's determined the pillar cannot ever be seen for a given location.")] public TileInstance InnerGroundTile;

        [Header("Buildings")]
        public GameObject City;
        public GameObject DairyFarm;
        public GameObject GrainFarm;
        public GameObject ExportDepot;

        [Header("Static Modifiers")]
        public GameObject[] Rock;
        public GameObject[] Tree;
    }
}