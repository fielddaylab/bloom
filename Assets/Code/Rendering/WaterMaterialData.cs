using FieldDay.SharedState;

namespace Zavala.World {
    public class WaterMaterialData : SharedStateComponent {
        public InterpolatedMaterial TopMaterial;
        public InterpolatedMaterial WaterfallMaterial;

        private void Awake() {
            TopMaterial.Load();
            WaterfallMaterial.Load();
        }
        
    }
}