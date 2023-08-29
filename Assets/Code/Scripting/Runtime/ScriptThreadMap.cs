using System.Collections.Generic;
using BeauUtil;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    public sealed class ScriptThreadMap {
        public Dictionary<StringHash32, LeafThreadHandle> Threads = new Dictionary<StringHash32, LeafThreadHandle>(16, CompareUtils.DefaultEquals<StringHash32>());

        public ScriptNodePriority GetCurrentPriority(StringHash32 targetId) {
            if (Threads.TryGetValue(targetId, out LeafThreadHandle handle)) {
                ScriptThread thread = handle.GetThread<ScriptThread>();
                if (thread != null) {
                    return thread.Priority();
                }
            }

            return ScriptNodePriority.None;
        }

        public ScriptThread GetCurrentThread(StringHash32 targetId) {
            if (Threads.TryGetValue(targetId, out LeafThreadHandle handle)) {
                return handle.GetThread<ScriptThread>();
            }

            return null;
        }

        public LeafThreadHandle GetCurrentHandle(StringHash32 targetId) {
            if (Threads.TryGetValue(targetId, out LeafThreadHandle handle)) {
                return handle;
            }

            return default;
        }
    }
}