using System;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using FieldDay.SharedState;
using Leaf;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Scripting;
using Zavala.Sim;

using Random = System.Random;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent]
    [SharedStateInitOrder(-1)]
    public sealed class ScriptPersistence : SharedStateComponent, IRegistrationCallbacks {
        // node ids
        public RingBuffer<StringHash32> RecentViewedNodeIds = new RingBuffer<StringHash32>(32, RingBufferMode.Overwrite);
        public HashSet<StringHash32> SessionViewedNodeIds = new HashSet<StringHash32>(256);
        public HashSet<StringHash32> SavedViewedNodeIds = new HashSet<StringHash32>(64);

        public VariantTable GlobalVars = new VariantTable("global");

        public void OnDeregister() {
        }

        public void OnRegister() {
            ScriptUtility.RegisterTable(GlobalVars);
        }
    }
}