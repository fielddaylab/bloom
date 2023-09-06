using System;
using BeauUtil;
using BeauUtil.Blocks;
using Leaf;
using Leaf.Compiler;
using Leaf.Runtime;
using UnityEngine.Scripting;
using Zavala.Advisor;

namespace FieldDay.Scripting {
    /// <summary>
    /// Scripting node.
    /// </summary>
    public sealed class ScriptNode : LeafNode {
        public ScriptNode(string fullName, ScriptNodePackage inPackage) : base(fullName, inPackage) {
            FullName = fullName;
        }

        public readonly string FullName;
        public ScriptNodeFlags Flags;

        public StringHash32 TriggerOrFunctionId;
        public LeafExpressionGroup Conditions;
        public StringHash32 TargetId;
        public int EvalScore;
        public AdvisorType AdvisorType;
        [BlockMeta("priority")] public ScriptNodePriority Priority = ScriptNodePriority.Medium;
        [BlockMeta("repeat")] public int RepeatPeriod;
        public ScriptNodeMemoryTarget PersistenceType;
        [BlockMeta("tag")] public StringHash32 Tag;

        public new ScriptNodePackage Package() { return (ScriptNodePackage) m_Package; }

        #region Internal

        [BlockMeta("trigger")]
        private void SetTrigger(StringHash32 triggerId) {
            Flags |= ScriptNodeFlags.Trigger;
            TriggerOrFunctionId = triggerId;
        }

        [BlockMeta("function")]
        private void SetFunction(StringHash32 functionId) {
            Flags |= ScriptNodeFlags.Function;
            TriggerOrFunctionId = functionId;
        }

        [BlockMeta("once")]
        private void SetOnce(ScriptNodeMemoryTarget target = ScriptNodeMemoryTarget.Persistent) {
            Flags |= ScriptNodeFlags.Once;
            PersistenceType = target;
        }

        [BlockMeta("cutscene")]
        private void SetCutscene() {
            Flags |= ScriptNodeFlags.Cutscene;
            Priority = ScriptNodePriority.Cutscene;
        }

        [BlockMeta("queued")]
        private void SetQueued() {
            Flags |= ScriptNodeFlags.Queued;
        }

        [BlockMeta("conditions")]
        private void SetConditions(StringSlice conditions) {
            Conditions = LeafUtils.CompileExpressionGroup(this, conditions);
            EvalScore += Conditions.Count;
        }

        [BlockMeta("evalPriority")]
        private void AdjustEvalPriority(int priority) {
            EvalScore += priority;
        }

        [BlockMeta("target")]
        private void SetTarget(StringHash32 targetId) {
            TargetId = targetId;
            if (targetId == "*") {
                Flags |= ScriptNodeFlags.AnyTarget;
            }
        }

        [BlockMeta("forcePolicy")]
        private void SetForcePolicy(AdvisorType advisorType, ScriptNodeMemoryTarget target = ScriptNodeMemoryTarget.Persistent) {
            Flags |= ScriptNodeFlags.ForcePolicy;
            AdvisorType = advisorType;
            PersistenceType = target;
        }

        #endregion // Internal
    }

    /// <summary>
    /// Behavior flags.
    /// </summary>
    [Flags]
    public enum ScriptNodeFlags : uint {
        Trigger = 0x01,
        Function = 0x02,
        Once = 0x04,
        Cutscene = 0x08,
        Queued = 0x10,
        IgnoreDuringCutscene = 0x20,
        InterruptSamePriority = 0x40,
        AnyTarget = 0x80,
        ForcePolicy = 0x100
    }

    /// <summary>
    /// Thread priority.
    /// </summary>
    public enum ScriptNodePriority {
        None,
        Low,
        Medium,
        High,
        Cutscene
    }

    /// <summary>
    /// Memory target for nodes.
    /// </summary>
    public enum ScriptNodeMemoryTarget {
        Persistent,
        Session
    }
}