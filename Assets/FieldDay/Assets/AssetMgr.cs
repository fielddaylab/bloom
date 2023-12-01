using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

using GlobalAssetIndex = BeauUtil.TypeIndex<FieldDay.Assets.IGlobalAsset>;

namespace FieldDay.Assets {
    /// <summary>
    /// Asset manager.
    /// </summary>
    public sealed class AssetMgr {
        private readonly Dictionary<StringHash32, UnityEngine.Object> m_NamedAssetLookup = new Dictionary<StringHash32, Object>(1024, CompareUtils.DefaultEquals<StringHash32>());
        private readonly IGlobalAsset[] m_GlobalAssetTable = new IGlobalAsset[GlobalAssetIndex.Capacity];

        #region Events

        internal void Shutdown() {
            m_NamedAssetLookup.Clear();
        }

        #endregion // Events
    }
}