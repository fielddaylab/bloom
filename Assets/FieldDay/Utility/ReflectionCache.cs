using BeauUtil;
using System;
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

        static private Assembly[] s_CachedUserAssemblies;
        static private readonly Dictionary<Type, EnumInfoCache> s_CachedEnumInfo = new Dictionary<Type, EnumInfoCache>(4);

        #region Assemblies

        /// <summary>
        /// Array of all user assemblies.
        /// </summary>
        static public Assembly[] UserAssemblies {
            get {
                if (s_CachedUserAssemblies == null) {
                    List<Assembly> userAssemblies = new List<Assembly>(16);
                    userAssemblies.AddRange(Reflect.FindAllUserAssemblies());
                    s_CachedUserAssemblies = userAssemblies.ToArray();
                }
                return s_CachedUserAssemblies;
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
}