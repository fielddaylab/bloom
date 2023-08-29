using FieldDay.Systems;
using UnityEngine;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent]
    [SysUpdate(GameLoopPhase.LateUpdate, 1000)]
    public sealed class ScriptRuntimeSystem : SharedStateSystemBehaviour<ScriptRuntimeState> {
        public override void ProcessWork(float deltaTime) {

            // clear default table
            m_State.ResolverOverride.ClearDefaultTable();
        }
    }
}