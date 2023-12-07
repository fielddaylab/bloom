using System.Collections.Generic;
using BeauUtil;

namespace FieldDay.Assets {
    /// <summary>
    /// Interface for a global configuration asset.
    /// </summary>
    [TypeIndexCapacity(1024)]
    public interface IGlobalAsset {
        void Mount();
        void Unmount();
    }

    /// <summary>
    /// Interface for an asset containing other assets.
    /// </summary>
    public interface IAssetPackage {
        IEnumerable<KeyValuePair<StringHash32, object>> GetSubAssets();
    }
}