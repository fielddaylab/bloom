using FieldDay.SharedState;

namespace Zavala.World {
    public class WaterMaterialData : SharedStateComponent {
        public InterpolatedMaterial TopMaterial;
        public InterpolatedMaterial WaterfallMaterial;
        public InterpolatedMaterial TopDeepMaterial;

        private void Awake() {
            TopMaterial.Load();
            WaterfallMaterial.Load();
            TopDeepMaterial.Load();
        }

    }
}