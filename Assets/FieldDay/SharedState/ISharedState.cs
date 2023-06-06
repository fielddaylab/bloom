using System;
using UnityEngine.Scripting;

namespace FieldDay.SharedState {
    /// <summary>
    /// Singleton state object.
    /// </summary>
    public interface ISharedState { }

    /// <summary>
    /// Attribute marking a static field or property as an injected ISharedState reference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    [Preserve]
    public sealed class SharedStateReferenceAttribute : PreserveAttribute {
    }
}