using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using Zavala.Data;

namespace Zavala.World {
    public class WaterMaterialData : SharedStateComponent {
        public InterpolatedMaterial TopMaterial;
        public InterpolatedMaterial WaterfallMaterial;
        public InterpolatedMaterial TopDeepMaterial;
        public InterpolatedMaterial SideMaterial;
        public InterpolatedMaterial SideDeepMaterial;

        public Vector3 WaterfallOffset;

        public Shader FancyShader;
        public Shader SimpleShader;

        private void Awake() {
            TopMaterial.Load();
            WaterfallMaterial.Load();
            TopDeepMaterial.Load();
            SideMaterial.Load();
            SideDeepMaterial.Load();
        }

        public void SetShaderMode(bool highQuality) {
            if (highQuality) {
                TopMaterial.ReplaceShader(FancyShader);
                TopDeepMaterial.ReplaceShader(FancyShader);
            } else {
                TopMaterial.ReplaceShader(SimpleShader);
                TopDeepMaterial.ReplaceShader(SimpleShader);
            }
        }

    }
}