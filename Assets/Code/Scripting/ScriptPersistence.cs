using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent]
    [SharedStateInitOrder(-1)]
    public sealed class ScriptPersistence : SharedStateComponent, IRegistrationCallbacks {
        // node ids
        public RingBuffer<StringHash32> RecentViewedNodeIds = new RingBuffer<StringHash32>(32, RingBufferMode.Overwrite);
        public HashSet<StringHash32> SessionViewedNodeIds = new HashSet<StringHash32>(256);
        public HashSet<StringHash32> SavedViewedNodeIds = new HashSet<StringHash32>(64);

        public VariantTable GlobalVars = new VariantTable("global");
        public VariantTable IntroVars = new VariantTable("intro");

        public void OnDeregister() {
        }

        public void OnRegister() {
            ScriptUtility.RegisterTable(GlobalVars);
            ScriptUtility.RegisterTable(IntroVars);
        }
    }
}