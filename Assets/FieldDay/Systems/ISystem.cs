using System;
using UnityEngine.Scripting;

namespace FieldDay.Systems {
    /// <summary>
    /// Game system.
    /// </summary>
    public interface ISystem {
        /// <summary>
        /// Initializes the system.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shuts down the system.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Indicates if the system has any work to process.
        /// </summary>
        bool HasWork();

        /// <summary>
        /// Processes available work.
        /// </summary>
        void ProcessWork(float deltaTime);
    }

    /// <summary>
    /// Component system.
    /// </summary>
    public interface IComponentSystem : ISystem {
        /// <summary>
        /// Number of components in this system.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Type of component.
        /// </summary>
        Type ComponentType { get; }

        /// <summary>
        /// Adds the given component to the system.
        /// </summary>
        void Add(object component);

        /// <summary>
        /// Removes the given component from the system.
        /// </summary>
        void Remove(object component);
    }

    /// <summary>
    /// Component system.
    /// </summary>
    public interface IComponentSystem<in TPrimary> : IComponentSystem
        where TPrimary : class, IComponentData {
        /// <summary>
        /// Adds the given component to the system.
        /// </summary>
        void Add(TPrimary component);

        /// <summary>
        /// Removes the given component from the system.
        /// </summary>
        void Remove(TPrimary component);
    }

    /// <summary>
    /// Component system.
    /// </summary>
    public interface IComponentSystem<in TPrimary, in TSecondary> : IComponentSystem
        where TPrimary : class, IComponentData
        where TSecondary : class, IComponentData {
        /// <summary>
        /// Adds the given component to the system.
        /// </summary>
        void Add(TPrimary component);

        /// <summary>
        /// Removes the given component from the system.
        /// </summary>
        void Remove(TPrimary component);
    }

    /// <summary>
    /// Attribute marking a static field or property as an injected system reference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false), Preserve]
    public sealed class SysReferenceAttribute : PreserveAttribute {
    }

    /// <summary>
    /// Attribute defining system initialization order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false), Preserve]
    public sealed class SysInitOrderAttribute : PreserveAttribute {
        public readonly int Order;

        public SysInitOrderAttribute(int order) {
            Order = order;
        }
    }

    /// <summary>
    /// Attribute defining system update order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false), Preserve]
    public sealed class SysUpdateAttribute : PreserveAttribute {
        public readonly GameLoopPhase Phase;
        public readonly int Order;

        public SysUpdateAttribute(GameLoopPhase phase, int order = 0) {
            Phase = phase;
            Order = order;
        }
    }
}