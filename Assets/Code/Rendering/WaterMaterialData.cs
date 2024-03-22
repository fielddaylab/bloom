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

        public Vector3 WaterfallOffset;

        public Shader FancyShader;
        public Shader SimpleShader;

        private void Awake() {
            TopMaterial.Load();
            WaterfallMaterial.Load();
            TopDeepMaterial.Load();

#if UNITY_EDITOR

#endif // UNITY_EDITOR
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