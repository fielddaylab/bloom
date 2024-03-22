using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Bucket of nodes that can be evaluated.
    /// </summary>
    public sealed class ScriptNodeBucket {
        private RingBuffer<ScriptNode> m_Ordered = new RingBuffer<ScriptNode>(16, RingBufferMode.Expand);
        private HashSet<ScriptNode> m_Unordered = new HashSet<ScriptNode>();
        private bool m_SortDirty = false;

        /// <summary>
        /// Adds a sorted node to the bucket.
        /// </summary>
        public void AddSorted(ScriptNode scriptNode) {
            m_Ordered.PushBack(scriptNode);
            if (!m_SortDirty && m_Ordered.Count > 1 && m_Ordered[m_Ordered.Count - 2].EvalScore <= scriptNode.EvalScore) {
                m_SortDirty = true;
            }
        }

        /// <summary>
        /// Adds an unsorted node to the bucket.
        /// </summary>
        public void AddUnsorted(ScriptNode scriptNode) {
            m_Unordered.Add(scriptNode);
        }

        /// <summary>
        /// Removes a sorted node from the bucket.
        /// </summary>
        public void RemoveSorted(ScriptNode scriptNode) {
            if (m_Ordered.FastRemove(scriptNode)) {
                m_SortDirty = true;
            }
        }

        /// <summary>
        /// Removes an unsorted node from the bucket.
        /// </summary>
        public void RemoveUnsorted(ScriptNode scriptNode) {
            m_Unordered.Remove(scriptNode);
        }

        /// <summary>
        /// Ensures the bucket is sorted in evaluation order.
        /// </summary>
        public void EnsureSorted() {
            if (!m_SortDirty) {
                return;
            }

            m_Ordered.Sort(EvalSorter);
            m_SortDirty = false;
        }

        /// <summary>
        /// Retrieves the highest scoring nodes that fulfill the given predicate.
        /// </summary>
        public int GetHighestScoringSorted(LeafEvalContext evalContext, StringHash32 targetId, ScriptPersistence persistence, ScriptRuntimeState runtimeState, ICollection<ScriptNode> nodes) {
            EnsureSorted();

            int count = 0;
            int minScore = int.MinValue;
            foreach (var node in m_Ordered) {
                if (!node.Package().IsActive()) {
                    continue;
                }

                if (node.EvalScore < minScore) {
                    break;
                }

                if (!IsValidCandidateEarly(node, evalContext, targetId, persistence, runtimeState)) {
                    continue;
                }

                //if (!IsValidCandidateTriggerSpecific(node, evalContext, targetId, runtimeState)) {
                //    continue;
                //}

                if (!IsValidCandidate(node, evalContext, targetId)) {
                    continue;
                }

                minScore = node.EvalScore;
                nodes.Add(node);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Retrieves all the unsorted that fulfill the given predicate.
        /// </summary>
        public int GetAllUnsorted(LeafEvalContext evalContext, StringHash32 targetId, ScriptPersistence persistence, ScriptRuntimeState runtimeState, ICollection<ScriptNode> nodes) {
            int count = 0; 
            foreach(var node in m_Unordered) {
                if (!node.Package().IsActive()) {
                    continue;
                }

                if (!IsValidCandidateEarly(node, evalContext, targetId, persistence, runtimeState)) {
                    continue;
                }

                if (!IsValidCandidate(node, evalContext, targetId)) {
                    continue;
                }

                nodes.Add(node);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Returns if the given node is a valid candidate for execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsValidCandidate(ScriptNode node, LeafEvalContext evalContext, StringHash32 targetId) {
            if (node.Conditions.Count > 0) {
                if (!node.Conditions.Evaluate(evalContext, out LeafExpression failure).AsBool()) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns if the given node is a valid candidate for execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsValidCandidateEarly(ScriptNode node, LeafEvalContext evalContext, StringHash32 targetId, ScriptPersistence persistence, ScriptRuntimeState runtimeState) {
            if ((node.Flags & ScriptNodeFlags.IgnoreDuringCutscene) != 0 && runtimeState.Cutscene.IsRunning()) {
                return false;
            }

            if ((node.Flags & ScriptNodeFlags.Once) != 0) {
                if (persistence.SessionViewedNodeIds.Contains(node.Id())) {
                    return false;
                }
            } else if (node.RepeatPeriod > 0) {
                int viewedIndex = persistence.RecentViewedNodeIds.IndexOf(node.Id());
                if (viewedIndex >= 0 && viewedIndex < node.RepeatPeriod) {
                    return false;
                }
            }

            //if ((node.Flags & ScriptNodeFlags.AnyTarget) != 0 && !targetId.IsEmpty && targetId != node.TargetId) {
            //    return false;
            //}

            return true;
        }

        /// <summary>
        /// Returns if the given node is a valid candidate for execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsValidCandidateTriggerSpecific(ScriptNode node, LeafEvalContext evalContext, StringHash32 targetId, ScriptRuntimeState runtimeState) {
            //StringHash32 target = targetId.IsEmpty ? node.TargetId : targetId;
            //if (!target.IsEmpty) {
            //    ScriptNodePriority currentPriority = runtimeState.ThreadMap.GetCurrentPriority(target);
            //    if (!ScriptDatabaseUtility.CanInterrupt(node, currentPriority)) {
            //        return false;
            //    }
            //}
            return true;
        }

        static private readonly Comparison<ScriptNode> EvalSorter = (a, b) => {
            return b.EvalScore - a.EvalScore;
        };
    }
}