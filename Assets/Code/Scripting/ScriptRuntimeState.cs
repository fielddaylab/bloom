using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using FieldDay.SharedState;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Advisor;
using Zavala.Scripting;
using Zavala.UI;
using Random = System.Random;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent]
    [SharedStateInitOrder(-10)]
    public sealed class ScriptRuntimeState : SharedStateComponent, IRegistrationCallbacks {
        public readonly ScriptThreadMap ThreadMap = new ScriptThreadMap();
        public readonly RingBuffer<LeafThreadHandle> ActiveThreads = new RingBuffer<LeafThreadHandle>(8, RingBufferMode.Expand);
        public LeafThreadHandle Cutscene;
        public int NestedCutscenePauseCount;

        public readonly Dictionary<StringHash32, EventActor> NamedActors = new Dictionary<StringHash32, EventActor>(16, CompareUtils.DefaultEquals<StringHash32>());

        public ScriptPlugin Plugin;
        public CustomVariantResolver Resolver;
        public IPool<ScriptThread> ThreadPool;
        public IPool<VariantTable> TablePool;
        public IPool<TagStringParser> ParserPool;
        public readonly Random Random = new Random();
        public CustomVariantResolver ResolverOverride;

        public DialogueBox DefaultDialogue;

        public void OnRegister() {
            Resolver = new CustomVariantResolver();

            ResolverOverride = new CustomVariantResolver();
            ResolverOverride.Base = Resolver;
            
            Plugin = new ScriptPlugin(this, Resolver);
            Plugin.ConfigureDisplay(DefaultDialogue, DefaultDialogue);

            ThreadPool = new DynamicPool<ScriptThread>(4, (p) => {
                return new ScriptThread(p, Plugin);
            });
            ThreadPool.Prewarm();

            ParserPool = new DynamicPool<TagStringParser>(2, (p) => Plugin.NewParser());
            ParserPool.Prewarm();

            TablePool = new DynamicPool<VariantTable>(8, Pool.DefaultConstructor<VariantTable>());
            TablePool.Config.RegisterOnAlloc((p, t) => t.Name = "temp");
            TablePool.Config.RegisterOnFree((p, t) => t.Reset());
            TablePool.Prewarm();
        }

        public void OnDeregister() {
        }
    }

    static public class ScriptUtility {
        [SharedStateReference] static public ScriptRuntimeState Runtime { get; private set; }
        [SharedStateReference] static public ScriptPersistence Persistence { get; private set; }
        [SharedStateReference] static public ScriptDatabase Database { get; private set; }

        static public LeafEvalContext GetContext(ScriptRuntimeState state, ILeafActor actor, VariantTable table) {
            if (table == null || table.Count == 0) {
                return LeafEvalContext.FromResolver(state.Plugin, state.Resolver, actor);
            }

            state.ResolverOverride.SetDefaultTable(table);
            return LeafEvalContext.FromResolver(state.Plugin, state.ResolverOverride, actor);
        }

        static public void RegisterTable(VariantTable table) {
            Runtime.Resolver.SetTable(table);
        }

        static public void DeregisterTable(VariantTable table) {
            Runtime.Resolver.ClearTable(table.Name);
        }

        static public void Invoke(StringHash32 functionId, VariantTable vars = null) {
            Invoke(functionId, null, vars);
        }

        static public void Invoke(StringHash32 functionId, ILeafActor actor, VariantTable vars = null) {
            using (PooledList<ScriptNode> funcNodes = PooledList<ScriptNode>.Create()) {
                LeafEvalContext context = GetContext(Runtime, actor, vars);
                ScriptDatabaseUtility.FindAllFunctions(Database, functionId, context, default, funcNodes);
                foreach (var node in funcNodes) {
                    Runtime.Plugin.Run(node, null, vars);
                }
            }
        }

        static public LeafThreadHandle Trigger(StringHash32 triggerId, VariantTable vars = null) {
            return Trigger(triggerId, null, vars);
        }

        static public LeafThreadHandle Trigger(StringHash32 triggerId, ILeafActor actor, VariantTable vars = null) {
            Invoke(triggerId, vars);

            Debug.Log("[ScriptUtility] Triggered event " + triggerId.ToDebugString());

            LeafEvalContext context = GetContext(Runtime, actor, vars);
            ScriptNode node = ScriptDatabaseUtility.FindRandomTrigger(Database, triggerId, context, default);
            if (node != null) {
                Debug.Log("[Script] triggered node " + node.Id().ToDebugString());
                return Runtime.Plugin.Run(node, actor, vars);
            }

            /*
            LeafEvalContext context = GetContext(Runtime, actor, vars);
            ScriptNode node = ScriptDatabaseUtility.FindSpecificNode(Database, node.Id(), context, default);
            if (node != null) {
                Debug.Log("[Script] triggered node " + node.Id().ToDebugString());
                return Runtime.Plugin.Run(node, actor, vars);
            }
            */

            return default;
        }

        static public StringHash32 FindCharacterId(TagString str) {
            str.TryFindEvent(LeafUtils.Events.Character, out var evtData);
            return evtData.Argument0.AsStringHash();
        }

        static public bool ActorExists(StringHash32 actorId)
        {
            return Runtime.NamedActors.TryGetValue(actorId, out EventActor actor);
        }

        static public EventActor LookupActor(StringHash32 actorId) {
            Runtime.NamedActors.TryGetValue(actorId, out EventActor actor);
            return actor;
        }

        static public void ParseToTag(ref TagString tagString, StringSlice content, LeafEvalContext context = default) {
            TagStringParser parser = Runtime.ParserPool.Alloc();
            parser.Parse(ref tagString, content, context);
            Runtime.ParserPool.Free(parser);
        }

        static public void MountDisplayer(ITextDisplayer displayer) {
            Runtime.DefaultDialogue.HideDialogueUI();
            Runtime.Plugin.ConfigureDisplay(displayer, displayer as IChoiceDisplayer);
        }

        static public void UnmountDisplayer(ITextDisplayer displayer) {
            Runtime?.Plugin.ConfigureDisplay(Runtime.DefaultDialogue, Runtime.DefaultDialogue);
        }

        static public void AutoOpenPolicyCards(PolicyType type) {
            DialogueBox box = Runtime.DefaultDialogue;
            box.CardsToShow = type;
            box.ShowHand = true;
        }

        static public void UnpinDialogue() {
            Runtime.DefaultDialogue.Pin.Unpin();
        }

        [LeafMember("Visited")]
        static public bool Visited(StringHash32 nodeId) {
            return Persistence.SessionViewedNodeIds.Contains(nodeId);
        }

        [LeafMember("ForcePolicyInstant")]
        static public void ForcePolicyInstant(AdvisorType type) {
            Runtime.DefaultDialogue.ForceExpandPolicyUI(type);
        }

        [LeafMember("ForceCloseDialogue")]
        static public void ForceCloseDialogue() {
            Runtime.DefaultDialogue.HideDialogueUI();
        }
    }
}