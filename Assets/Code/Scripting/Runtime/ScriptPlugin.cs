using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.Assets;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using UnityEngine;
using Zavala;
using Zavala.Scripting;
using Zavala.Sim;
using Zavala.UI.Info;

namespace FieldDay.Scripting {
    public class ScriptPlugin : DefaultLeafManager<ScriptNode> {
        private readonly ScriptRuntimeState m_RuntimeState;
        private readonly Action LateEndCutsceneDelegate;

        public ScriptPlugin(ScriptRuntimeState inHost, CustomVariantResolver inResolver, IMethodCache inCache = null, LeafRuntimeConfiguration inConfiguration = null)
            : base(inHost, inResolver, inCache, inConfiguration) {
            m_RuntimeState = inHost;

            BlockMetaCache.Default.Cache(typeof(ScriptNode));
            BlockMetaCache.Default.Cache(typeof(ScriptNodePackage));

            ConfigureTagStringHandling(new CustomTagParserConfig(), new TagStringEventHandler());

            LeafUtils.ConfigureDefaultParsers(m_TagParseConfig, this, null);
            m_TagParseConfig.AddEvent("local", ReplaceLocalIdOf);
            m_TagParseConfig.AddReplace("alertRegionName", AlertRegionToName);
            // TODO: add replace "alert" to use local:alertRegion?
            m_TagParseConfig.AddEvent("viewpoliciesnext", "ViewPolicies");

            LeafUtils.ConfigureDefaultHandlers(m_TagHandler, this);

            m_TagHandler.Register(LeafUtils.Events.Character, () => { });

            LateEndCutsceneDelegate = LateDecrementNestedPauseCount;
        }

        public override LeafThreadHandle Run(ScriptNode inNode, ILeafActor inActor = null, VariantTable inLocals = null, string inName = null, bool inbImmediateTick = true) {
            if (inNode == null) {
                return default;
            }

            ScriptThread thread = m_RuntimeState.ThreadPool.Alloc();

            m_RuntimeState.Cutscene.Kill();
            m_RuntimeState.DefaultDialogue.ClearQueuedPolicyUI();

            TempAlloc<VariantTable> tempVars = m_RuntimeState.TablePool.TempAlloc();
            if (inLocals != null && inLocals.Count > 0) {
                inLocals.CopyTo(tempVars.Object);
                tempVars.Object.Base = inLocals.Base;
            }

            LeafThreadHandle handle = thread.Setup(inName, inActor, tempVars);
            tempVars.Dispose();
            thread.SetInitialNode(inNode);
            thread.AttachRoutine(Routine.Start(m_RoutineHost, LeafRuntime.Execute(thread, inNode)));

            m_RuntimeState.Cutscene = handle;

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
                persistence.SessionViewedNodeIds.Add(nodeId);
            }
            bool cutscene = (inNode.Flags & ScriptNodeFlags.Cutscene) != 0;
            if (cutscene) {
                m_RuntimeState.NestedCutscenePauseCount++;
                SimTimeUtility.Pause(SimPauseFlags.Cutscene, ZavalaGame.SimTime);
                Game.Events.Dispatch(GameEvents.LeafCutsceneStarted);
            }
            if ((inNode.Flags & ScriptNodeFlags.ForcePolicyEarly) != 0) {
                m_RuntimeState.DefaultDialogue.ForceExpandPolicyUI(inNode.AdvisorType);
            }
            if (Game.Gui.TryGetShared(out InfoPopup ip)) {
                ip.HoldOpen = false;
                ip.Hide();
            }
            m_RuntimeState.DefaultDialogue.MarkNodeEntered();
            
            // reset pin overrides at the start of a new node
            m_RuntimeState.DefaultDialogue.ClearPinForces();

            ZavalaGame.Events.Dispatch(GameEvents.DialogueStarted, EvtArgs.Box(new Zavala.Data.ScriptNodeData(inNode.FullName, !cutscene)));
        }

        public override void OnNodeExit(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            if ((inNode.Flags & ScriptNodeFlags.ForcePolicy) != 0) {
                // View Policies: expand
                m_RuntimeState.DefaultDialogue.ForceExpandPolicyUI(inNode.AdvisorType);
                //m_RuntimeState.DefaultDialogue.ForceAdvisorPolicies = inNode.AdvisorType;
                // m_RuntimeState.DefaultDialogue.ExpandPolicyUI(inNode.AdvisorType);
            }
            else {
                // Close advisor, no policies forced
                m_RuntimeState.DefaultDialogue.HideDialogueUI();
            }
            m_RuntimeState.DefaultDialogue.MarkNodeExited();

            bool cutscene = (inNode.Flags & ScriptNodeFlags.Cutscene) != 0;
            if (cutscene) {
                GameLoop.QueueEndOfFrame(LateEndCutsceneDelegate);
                Game.Events.Dispatch(GameEvents.LeafCutsceneEnded);
            }
            if (Game.Gui.TryGetShared(out InfoPopup ip) && ip.HoldOpen) {
                ip.HoldOpen = false;
            }
            ZavalaGame.Events.Dispatch(GameEvents.DialogueClosing, EvtArgs.Box(new Zavala.Data.ScriptNodeData(inNode.FullName, !cutscene)));
            
        }

        private void LateDecrementNestedPauseCount() {
            m_RuntimeState.NestedCutscenePauseCount--;
            if (m_RuntimeState.NestedCutscenePauseCount == 0) {
                SimTimeUtility.Resume(SimPauseFlags.Cutscene, ZavalaGame.SimTime);
            }
        }

        public override void OnEnd(LeafThreadState<ScriptNode> inThreadState) {
            if (m_RuntimeState.Cutscene == inThreadState.GetHandle()) {
                m_RuntimeState.Cutscene = default;
            }

            base.OnEnd(inThreadState);
        }
    
        public TagStringParser NewParser() {
            TagStringParser parser = new TagStringParser();
            parser.EventProcessor = m_TagParseConfig;
            parser.ReplaceProcessor = m_TagParseConfig;
            parser.Delimiters = Parsing.InlineEvent;
            return parser;
        }

        public override bool TryLookupObject(StringHash32 inObjectId, LeafThreadState inThreadState, out object outObject) {
            bool result = m_RuntimeState.NamedActors.TryGetValue(inObjectId, out var evt);
            outObject = evt;
            return result;
        }

        static private void ReplaceLocalIdOf(TagData inTag, object inContext, ref TagEventData ioData) {
            if (inTag.Data.StartsWith('@')) {
                StringHash32 characterId = inTag.Data.Substring(1);
                ScriptCharacterDB db = Game.SharedState.Get<ScriptCharacterDB>();

                LeafEvalContext context = LeafEvalContext.FromObject(inContext);

                int regionOverride = -1;
                if (context.Table.TryLookup("alertRegion", out Variant region)) {
                    regionOverride = region.AsInt() - 1; // 1-indexed to 0-indexed
                }

                var remapped = ScriptCharacterDBUtility.GetRemapped(db, characterId, regionOverride);

                ioData.Type = LeafUtils.Events.Character;
                ioData.Argument0 = AssetUtility.CacheNameHash(ref remapped.CachedId, remapped);
                return;
            }

            Debug.LogError("[ScriptPlugin] No local id could be found for " + inTag.Data.Substring(1));
            return;
        }

        static private string AlertRegionToName(TagData inTag, object inContext) {
            int regionIdx = -1;
            if (LeafEvalContext.FromObject(inContext).Table.TryLookup("alertRegion", out Variant region)) {
                regionIdx = region.AsInt() - 1; // 1-indexed to 0-indexed
            } else {
                regionIdx = StringParser.ParseInt(inTag.Data);
            }
            return RegionUtility.GetNameString(regionIdx);
        }
    }
}