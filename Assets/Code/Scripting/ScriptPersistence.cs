using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.SharedState;
using UnityEngine;
using Zavala;
using Zavala.Data;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent]
    [SharedStateInitOrder(-1)]
    public sealed class ScriptPersistence : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject {
        // node ids
        public RingBuffer<StringHash32> RecentViewedNodeIds = new RingBuffer<StringHash32>(32, RingBufferMode.Overwrite);
        public HashSet<StringHash32> SessionViewedNodeIds = new HashSet<StringHash32>(256);

        public VariantTable GlobalVars = new VariantTable("global");
        public VariantTable IntroVars = new VariantTable("intro");

        public void OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("ScriptPersist");
        }

        public void OnRegister() {
            ScriptUtility.RegisterTable(GlobalVars);
            ScriptUtility.RegisterTable(IntroVars);

            ZavalaGame.SaveBuffer.RegisterHandler("ScriptPersist", this, -100);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            GlobalVars.Clear();
            IntroVars.Clear();
            SessionViewedNodeIds.Clear();

            int globalCount = reader.Read<ushort>();
            GlobalVars.Capacity = Mathf.NextPowerOfTwo(globalCount);
            for(int i = 0; i < globalCount; i++) {
                NamedVariant variant = reader.Read<NamedVariant>();
                GlobalVars.Set(variant.Id, variant.Value);
            }

            GlobalVars.Optimize();

            int introCount = reader.Read<ushort>();
            IntroVars.Capacity = Mathf.NextPowerOfTwo(globalCount);
            for (int i = 0; i < introCount; i++) {
                NamedVariant variant = reader.Read<NamedVariant>();
                IntroVars.Set(variant.Id, variant.Value);
            }

            IntroVars.Optimize();

            int viewedNodeCount = reader.Read<ushort>();
            SetUtils.EnsureCapacity(SessionViewedNodeIds, viewedNodeCount);
            for(int i = 0; i < viewedNodeCount; i++) {
                SessionViewedNodeIds.Add(reader.Read<StringHash32>());
            }
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            writer.Write((ushort) GlobalVars.Count);
            for(int i = 0; i < GlobalVars.Count; i++) {
                writer.Write(GlobalVars[i]);
            }

            writer.Write((ushort) IntroVars.Count);
            for (int i = 0; i < IntroVars.Count; i++) {
                writer.Write(IntroVars[i]);
            }

            writer.Write((ushort) SessionViewedNodeIds.Count);
            foreach(var node in SessionViewedNodeIds) {
                writer.Write(node);
            }
        }
    }
}