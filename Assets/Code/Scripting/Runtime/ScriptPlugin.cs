using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using BeauUtil.Variants;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using UnityEngine;
using Zavala;
using Zavala.Scripting;

namespace FieldDay.Scripting {
    public class ScriptPlugin : DefaultLeafManager<ScriptNode> {
        private readonly ScriptRuntimeState m_RuntimeState;

        public ScriptPlugin(ScriptRuntimeState inHost, CustomVariantResolver inResolver, IMethodCache inCache = null, LeafRuntimeConfiguration inConfiguration = null)
            : base(inHost, inResolver, inCache, inConfiguration) {
            m_RuntimeState = inHost;

            BlockMetaCache.Default.Cache(typeof(ScriptNode));
            BlockMetaCache.Default.Cache(typeof(ScriptNodePackage));

            ConfigureTagStringHandling(new CustomTagParserConfig(), new TagStringEventHandler());

            LeafUtils.ConfigureDefaultParsers(m_TagParseConfig, this, null);
            m_TagParseConfig.AddReplace("local", ReplaceLocalIdOf);

            LeafUtils.ConfigureDefaultHandlers(m_TagHandler, this);

            m_TagHandler.Register(LeafUtils.Events.Character, () => { });
        }

        public override LeafThreadHandle Run(ScriptNode inNode, ILeafActor inActor = null, VariantTable inLocals = null, string inName = null, bool inbImmediateTick = true) {
            if (inNode == null || !CheckPriorityState(inNode, m_RuntimeState)) {
                return default;
            }

            ScriptThread thread = m_RuntimeState.ThreadPool.Alloc();

            if ((inNode.Flags & ScriptNodeFlags.Cutscene) != 0) {
                m_RuntimeState.Cutscene.Kill();
                // TODO: Kill lower priority threads
            }

            TempAlloc<VariantTable> tempVars = m_RuntimeState.TablePool.TempAlloc();
            if (inLocals != null && inLocals.Count > 0) {
                inLocals.CopyTo(tempVars.Object);
                tempVars.Object.Base = inLocals.Base;
            }

            LeafThreadHandle handle = thread.Setup(inName, inActor, tempVars);
            tempVars.Dispose();
            thread.SetInitialNode(inNode);
            thread.AttachRoutine(Routine.Start(m_RoutineHost, LeafRuntime.Execute(thread, inNode)));

            m_RuntimeState.ActiveThreads.PushBack(handle);
            if ((inNode.Flags & ScriptNodeFlags.Cutscene) != 0) {
                m_RuntimeState.Cutscene = handle;
            }

            if (!inNode.TargetId.IsEmpty) {
                m_RuntimeState.ThreadMap.Threads[inNode.TargetId] = handle;
            }

            if (inbImmediateTick && m_RoutineHost.isActiveAndEnabled) {
                thread.ForceTick();
            }

            return handle;
        }

        public override void OnNodeEnter(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            ScriptPersistence persistence = Game.SharedState.Get<ScriptPersistence>();

            StringHash32 nodeId = inNode.Id();
            persistence.RecentViewedNodeIds.PushFront(nodeId);
            if ((inNode.Flags & ScriptNodeFlags.Once) != 0) {
                switch (inNode.PersistenceType) {
                    case ScriptNodeMemoryTarget.Persistent: {
                        persistence.SessionViewedNodeIds.Add(nodeId);
                        persistence.SavedViewedNodeIds.Add(nodeId);
                        break;
                    }
                    case ScriptNodeMemoryTarget.Session: {
                        persistence.SessionViewedNodeIds.Add(nodeId);
                        break;
                    }
                }
            }
        }

        public override void OnNodeExit(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            if ((inNode.Flags & ScriptNodeFlags.ForcePolicy) != 0) {
                // View Policies: expand
                m_RuntimeState.DefaultDialogue.ExpandPolicyUI(inNode.AdvisorType);
            }
            else {
                // Close advisor, no policies forced
                m_RuntimeState.DefaultDialogue.HideAdvisorUI();
            }
        }

        public override void OnEnd(LeafThreadState<ScriptNode> inThreadState) {
            base.OnEnd(inThreadState);
        }
    
        public TagStringParser NewParser() {
            TagStringParser parser = new TagStringParser();
            parser.EventProcessor = m_TagParseConfig;
            parser.ReplaceProcessor = m_TagParseConfig;
            parser.Delimiters = Parsing.InlineEvent;
            return parser;
        }

        static private bool CheckPriorityState(ScriptNode node, ScriptRuntimeState runtime) {
            StringHash32 target = node.TargetId;
            if (target.IsEmpty) {
                return true;
            }

            ScriptThread thread = runtime.ThreadMap.GetCurrentThread(target);
            if (thread != null) {
                if (!ScriptDatabaseUtility.CanInterrupt(node, thread.Priority())) {
                    return false;
                }

                thread.Kill();
                runtime.ThreadMap.Threads.Remove(target);
            }

            return true;
        }


        static private string ReplaceLocalIdOf(TagData inTag, object inContext) {
            if (inTag.Data.StartsWith('@')) {
                StringHash32 characterId = inTag.Data.Substring(1);
                ScriptCharacterDB db = Game.SharedState.Get<ScriptCharacterDB>();

                return "{@" + ScriptCharacterDBUtility.GetRemapped(db, characterId).name + "}";
            }

            Debug.LogError("[ScriptPlugin] No local id could be found for " + inTag.Data.Substring(1));
            return inTag.Data.ToString();
        }
    }
}