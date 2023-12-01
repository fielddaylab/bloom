using BeauUtil;

namespace FieldDay.Assets {
    /// <summary>
    /// Interface for a global configuration asset.
    /// </summary>
    [TypeIndexCapacity(256)]
    public interface IGlobalAsset {
        void Mount();
        void Unmount();
    }
}