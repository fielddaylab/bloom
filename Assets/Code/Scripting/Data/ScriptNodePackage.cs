using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.IO;
using Leaf;
using Leaf.Compiler;

namespace FieldDay.Scripting {
    public sealed class ScriptNodePackage : LeafNodePackage<ScriptNode> {
        private bool m_Active = false;
        private int m_UseCount = 0;
        private IHotReloadable m_HotReload; // TODO: Implement
        internal LeafAsset m_SourceAsset; // TODO: Implement

        public ScriptNodePackage(string inName) : base(inName) {
        }

        #region Active

        public bool IsActive() {
            return m_Active;
        }

        public bool SetActive(bool active) {
            if (m_Active != active) {
                m_Active = active;
                return true;
            }

            return false;
        }

        #endregion // Active

        #region Reference Count

        public void AddReference() {
            m_UseCount++;
        }

        public void ReleaseReference() {
            m_UseCount--;
        }

        public bool IsReferenced() {
            return m_UseCount > 0;
        }

        #endregion // Reference Count

        #region Nodes

        /// <summary>
        /// All trigger responses.
        /// </summary>
        public IEnumerable<ScriptNode> Responses() {
            foreach(var node in m_Nodes.Values) {
                if ((node.Flags & ScriptNodeFlags.Trigger) != 0) {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// All functions.
        /// </summary>
        public IEnumerable<ScriptNode> Functions() {
            foreach (var node in m_Nodes.Values) {
                if ((node.Flags & ScriptNodeFlags.Function) != 0) {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// All functions.
        /// </summary>
        public IEnumerable<ScriptNode> Queued() {
            foreach (var node in m_Nodes.Values) {
                if ((node.Flags & ScriptNodeFlags.Queued) != 0) {
                    yield return node;
                }
            }
        }

        #endregion // Nodes

        #region Generator

        public class Parser : LeafParser<ScriptNode, ScriptNodePackage> {
            static public readonly Parser Instance = new Parser();

            public override ScriptNodePackage CreatePackage(string inFileName) {
                return new ScriptNodePackage(inFileName);
            }

            protected override ScriptNode CreateNode(string inFullId, StringSlice inExtraData, ScriptNodePackage inPackage) {
                return new ScriptNode(inFullId, inPackage);
            }
        }

        #endregion // Generator
    }
}