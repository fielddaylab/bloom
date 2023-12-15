using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Assets {
    /// <summary>
    /// Property attribute marking a string/StringHash32/SerializedHash32 field as an asset identifier.
    /// This will store the name of the asset.
    /// </summary>
    public class AssetNameAttribute : PropertyAttribute {
        public readonly Type AssetType;
        internal readonly bool UseDropdown;

        protected internal virtual bool Predicate(UnityEngine.Object obj) { return true; }
        protected internal virtual string Name(UnityEngine.Object obj) { return obj.name; }
        protected virtual int CalculateHash() {
            int hash = 53;
            hash = MixHash(hash, GetType());
            hash = MixHash(hash, GetType().GetMethod("Predicate", BindingFlags.NonPublic | BindingFlags.Instance));
            hash = MixHash(hash, AssetType);
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static protected int MixHash(int currentHash, int nextVal) {
            return currentHash ^ (nextVal + (currentHash >> 2) + (currentHash << 5));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static protected int MixHash(int currentHash, object nextVal) {
            return currentHash ^ ((nextVal != null ? nextVal.GetHashCode() : 0) + (currentHash >> 2) + (currentHash << 5));
        }

        internal uint GetCacheKey() {
            if (m_CachedCacheKey == 0) {
                unsafe {
                    int calculated = CalculateHash();
                    m_CachedCacheKey = *(uint*) (&calculated);
                }
            }
            return m_CachedCacheKey;
        }

        private uint m_CachedCacheKey;

        public AssetNameAttribute(Type assetType, bool useDropdown = false) {
            if (assetType == null) {
                throw new ArgumentNullException("assetType");
            }
            AssetType = assetType;
            order = -10;

            UseDropdown = useDropdown;
            if (!UseDropdown) {
                UseDropdown = GetType().GetMethod("Predicate", BindingFlags.NonPublic | BindingFlags.Instance).DeclaringType != typeof(AssetNameAttribute);
                UseDropdown |= GetType().GetMethod("Name", BindingFlags.NonPublic | BindingFlags.Instance).DeclaringType != typeof(AssetNameAttribute);
            }
        }
    }
}