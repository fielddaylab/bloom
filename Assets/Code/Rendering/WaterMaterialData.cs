using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.World {
    public class WaterMaterialData : SharedStateComponent {
        public InterpolatedMaterial TopMaterial;
        public InterpolatedMaterial WaterfallMaterial;
        public InterpolatedMaterial TopDeepMaterial;

        public Vector3 WaterfallOffset;

        private void Awake() {
            TopMaterial.Load();
            WaterfallMaterial.Load();
            TopDeepMaterial.Load();
        }

    }
}