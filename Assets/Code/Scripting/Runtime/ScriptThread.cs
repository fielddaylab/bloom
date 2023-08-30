using System;
using BeauPools;
using BeauUtil;
using Leaf;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    public class ScriptThread : LeafThreadState<ScriptNode> {
        private readonly IPool<ScriptThread> m_Pool;
        
        private ScriptNode m_OriginalNode;
        private ScriptNodePriority m_Priority;

        // record state
        private bool m_RecordedDialog;
        private StringHash32 m_LastKnownCharacter;
        private string m_LastKnownName;

        public ScriptThread(IPool<ScriptThread> pool, ILeafPlugin<ScriptNode> inPlugin) : base(inPlugin) {
            m_Pool = pool;
        }

        public ScriptNodePriority Priority() {
            return m_Priority;
        }

        public void SetInitialNode(ScriptNode node) {
            m_OriginalNode = node;
            m_Priority = node.Priority;
        }

        protected override void Reset() {
            base.Reset();

            m_OriginalNode = null;
            m_Priority = default;
            m_Pool.Free(this);
        }
    }
}