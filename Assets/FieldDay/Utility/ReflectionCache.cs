using System;
using BeauUtil;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil.Debugger;

namespace FieldDay {
    /// <summary>
    /// Reflection cache.
    /// </summary>
    static public class ReflectionCache {
        static private Assembly[] s_CachedUserAssemblies;

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
    }
}