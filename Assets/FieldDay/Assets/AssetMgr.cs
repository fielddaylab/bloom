using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

using GlobalAssetIndex = BeauUtil.TypeIndex<FieldDay.Assets.IGlobalAsset>;

namespace FieldDay.Assets {
    /// <summary>
    /// Asset manager.
    /// </summary>
    public sealed class AssetMgr {
        private readonly Dictionary<StringHash32, object> m_NamedAssetLookup = new Dictionary<StringHash32, object>(1024, CompareUtils.DefaultEquals<StringHash32>());
        private readonly IGlobalAsset[] m_GlobalAssetTable = new IGlobalAsset[GlobalAssetIndex.Capacity];
        private readonly uint[] m_GlobalAssetRefCount = new uint[GlobalAssetIndex.Capacity];
        private readonly HashSet<IGlobalAsset> m_GlobalAssetSet = new HashSet<IGlobalAsset>(64, CompareUtils.DefaultEquals<IGlobalAsset>());

        #region Events

        internal void Shutdown() {
            m_NamedAssetLookup.Clear();
        }

        #endregion // Events

        #region Registration

        public void Register(IGlobalAsset globalAsset) {

        }

        #endregion // Registration

        #region Lookup

        #endregion // Lookup
    }
}