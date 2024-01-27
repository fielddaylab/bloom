using FieldDay.SharedState;

namespace Zavala.Data {
    public class UserSettings : ISharedState {
        public string PlayerCode = null;
        public float MusicVolume = 0.8f;
        public bool HighQualityMode = false;
    }
}