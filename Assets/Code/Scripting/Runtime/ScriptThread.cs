using System;
using BeauPools;
using BeauUtil;
using Leaf;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    public class ScriptThread : LeafThreadState<ScriptNode> {
        private readonly IPool<ScriptThread> m_Pool;
        
        private ScriptNode m_OriginalNode;

        // record state
        private bool m_RecordedDialog;
        private StringHash32 m_LastKnownCharacter;
        private string m_LastKnownName;

        public ScriptThread(IPool<ScriptThread> pool, ILeafPlugin<ScriptNode> inPlugin) : base(inPlugin) {
            m_Pool = pool;
        }

        public void SetInitialNode(ScriptNode node) {
            m_OriginalNode = node;
        }

        protected override void Reset() {
            base.Reset();

            m_OriginalNode = null;
            m_Pool.Free(this);
        }
    }
}