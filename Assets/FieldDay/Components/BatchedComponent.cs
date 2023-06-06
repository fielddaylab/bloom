using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// Base class for a component that automatically registers itself
    /// to all relevant systems in SystemsMgr
    /// </summary>
    public abstract class BatchedComponent : MonoBehaviour, IComponentData {
        protected virtual void OnEnable() {
            Game.Systems.AddComponent(this);
        }

        protected virtual void OnDisable() {
            if (!Game.IsShuttingDown) {
                Game.Systems.RemoveComponent(this);
            }
        }
    }
}