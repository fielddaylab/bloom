using System;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using FieldDay.Systems;
using Leaf;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Scripting;
using Zavala.Sim;
using Zavala.UI;
using Random = System.Random;

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