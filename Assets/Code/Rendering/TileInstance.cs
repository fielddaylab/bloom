using BeauUtil;
using FieldDay;
using UnityEditor;
using UnityEngine;
using Zavala.Sim;
using System;

namespace Zavala.World {
    public class TileInstance : MonoBehaviour {
        [Header("Tile Top")]
        public MeshRenderer TopRenderer;
        public MeshFilter TopFilter;

        [Header("Pillar")]
        public MeshRenderer PillarRenderer;

        [Header("Misc")]
        public Renderer[] Decorations;

        [NonSerialized] public SimpleMeshConfig TopDefaultConfig;
        [NonSerialized] public Material PillarDefaultMat;

        private void Awake() {
            TopDefaultConfig = new SimpleMeshConfig(TopRenderer, TopFilter);
            PillarDefaultMat = PillarRenderer ? PillarRenderer.sharedMaterial : null;
        }
    }
}