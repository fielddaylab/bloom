using BeauUtil;
using FieldDay;
using UnityEditor;
using UnityEngine;
using Zavala.Sim;
using System;

namespace Zavala.World {
    public class TileInstance : MonoBehaviour {
        public Renderer TopRenderer;
        public Renderer PillarRenderer;
        public Renderer[] Decorations;

        [NonSerialized] public Material TopDefaultMat;
        [NonSerialized] public Material PillarDefaultMat;

        private void Awake() {
            TopDefaultMat = TopRenderer ? TopRenderer.sharedMaterial : null;
            PillarDefaultMat = PillarRenderer ? PillarRenderer.sharedMaterial : null;
        }
    }
}