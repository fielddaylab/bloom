using UnityEngine;

namespace Zavala.World {
    [CreateAssetMenu(menuName = "Zavala/Region Prefab Palette")]
    public sealed class RegionPrefabPalette : ScriptableObject {
        [Header("Tiles")]
        public TileInstance GroundTile;

        [Header("Buildings")]
        public GameObject City;
        public GameObject DairyFarm;
        public GameObject GrainFarm;

        [Header("Static Modifiers")]
        public GameObject[] Rock;
        public GameObject[] Tree;
    }
}