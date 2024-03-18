using System.Collections.Generic;
using BeauUtil;

namespace FieldDay.Assets {
    /// <summary>
    /// Interface for a named asset.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface INamedAsset {
    }

    /// <summary>
    /// Interface for a global configuration asset.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface IGlobalAsset {
        void Mount();
        void Unmount();
    }

    /// <summary>
    /// Lightweight asset data.
    /// Useful for assets whose data can be contained with a struct.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface ILiteAsset {
    }

    /// <summary>
    /// Interface for an asset package.
    /// </summary>
    public interface IAssetPackage {
        void Mount(AssetMgr mgr);
        void Unmount(AssetMgr mgr);
    }
}