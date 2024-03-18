using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace FieldDay {
    /// <summary>
    /// Reflection cache.
    /// </summary>
    static public class ReflectionCache {
        /// <summary>
        /// Cached enum information.
        /// </summary>
        public struct EnumInfoCache {
            public object[] Values;
            public string[] Names;
        }

        static private readonly Dictionary<Type, EnumInfoCache> s_CachedEnumInfo = new Dictionary<Type, EnumInfoCache>(4);

        #region Assemblies

        /// <summary>
        /// Array of all user assemblies.
        /// </summary>
        static public IEnumerable<Assembly> UserAssemblies {
            get {
                return Reflect.FindAllUserAssemblies();
            }
        }

        #endregion // Assemblies

        #region Enums

        static public EnumInfoCache EnumInfo<T>() {
            return EnumInfo(typeof(T));
        }

        static public EnumInfoCache EnumInfo(Type enumType) {
            EnumInfoCache cache;
            if (!s_CachedEnumInfo.TryGetValue(enumType, out cache)) {
                List<object> values = new List<object>();
                List<string> names = new List<string>();
                foreach(var field in enumType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                    if (field.IsDefined(typeof(HiddenAttribute)) || field.IsDefined(typeof(ObsoleteAttribute))) {
                        continue;
                    }

                    LabelAttribute label = (LabelAttribute) field.GetCustomAttribute(typeof(LabelAttribute));
                    string name;
                    if (label != null) {
                        name = label.Name;
                    } else {
                        name = InspectorName(field.Name);
                    }

                    object value = field.GetValue(null);

                    values.Add(value);
                    names.Add(name);
                }

                cache.Values = values.ToArray();
                cache.Names = names.ToArray();
                s_CachedEnumInfo.Add(enumType, cache);
            }
            return cache;
        }

        #endregion // Enums

        #region String

        /// <summary>
        /// Returns the nicified name for the given field/type name.
        /// </summary>
        static public unsafe string InspectorName(string name) {
            char* buff = stackalloc char[name.Length * 2];
            bool wasUpper = true, isUpper;
            int charsWritten = 0;

            int i = 0;
            if (name.Length > 1) {
                char first = name[0];
                if (first == '_') {
                    i = 1;
                } else if (first == 'm' || first == 's' || first == 'k') {
                    char second = name[1];
                    if (second == '_' || char.IsUpper(second)) {
                        i = 2;
                    }
                }
            }

            for (; i < name.Length; i++) {
                char c = name[i];
                isUpper = char.IsUpper(c);
                if (isUpper && !wasUpper && charsWritten > 0) {
                    buff[charsWritten++] = ' ';
                }
                buff[charsWritten++] = c;

                wasUpper = isUpper;
            }

            return new string(buff, 0, charsWritten);
        }

        #endregion // String
    }

    /// <summary>
    /// Attribute enumerator.
    /// </summary>
    public struct AttributeEnumerable<TAttr, TInfo> : IEnumerable<AttributeBinding<TAttr, TInfo>>, IEnumerator<AttributeBinding<TAttr, TInfo>>, IDisposable
        where TAttr : Attribute
        where TInfo : MemberInfo {

        private IEnumerator<AttributeBinding<TAttr, TInfo>> m_Native;
        private IEnumerator<SerializedAttributeSet.AttributePair<TAttr>> m_FromSet;

        public AttributeEnumerable(IEnumerable<AttributeBinding<TAttr, TInfo>> enumerable) {
            m_Native = enumerable.GetEnumerator();
            m_FromSet = null;
        }

        public AttributeEnumerable(IEnumerable<SerializedAttributeSet.AttributePair<TAttr>> enumerable) {
            m_Native = null;
            m_FromSet = enumerable.GetEnumerator();
        }

        #region Disposable

        public void Dispose() {
            (m_Native as IDisposable)?.Dispose();
            (m_FromSet as IDisposable)?.Dispose();

            m_Native = null;
            m_FromSet = null;
        }

        #endregion // Disposable

        #region Enumerable

        public AttributeEnumerable<TAttr, TInfo> GetEnumerator() {
            return this;
        }

        IEnumerator<AttributeBinding<TAttr, TInfo>> IEnumerable<AttributeBinding<TAttr, TInfo>>.GetEnumerator() {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this;
        }

        #endregion // Enumerable

        #region Enumerator

        public AttributeBinding<TAttr, TInfo> Current {
            get {
                if (m_Native != null) {
                    return m_Native.Current;
                } else {
                    return new AttributeBinding<TAttr, TInfo>(m_FromSet.Current.Attribute, (TInfo) m_FromSet.Current.Info);
                }
            }
        }

        object IEnumerator.Current { get { return Current; } }

        public bool MoveNext() {
            if (m_Native != null) {
                return m_Native.MoveNext();
            } else {
                return m_FromSet.MoveNext();
            }
        }

        public void Reset() {
            m_Native?.Reset();
            m_FromSet?.Reset();
        }

        #endregion // Enumerator
    }
}