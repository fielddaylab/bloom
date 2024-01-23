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
        public HashSet<StringHash32> SavedViewedNodeIds = new HashSet<StringHash32>(64);

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

        unsafe void ISaveStateChunkObject.Read(object self, ref byte* data, ref int remaining, SaveStateChunkConsts consts) {
            GlobalVars.Clear();
            IntroVars.Clear();

            int globalCount = Unsafe.Read<ushort>(ref data, ref remaining);
            GlobalVars.Capacity = Mathf.NextPowerOfTwo(globalCount);
            for(int i = 0; i < globalCount; i++) {
                NamedVariant variant = Unsafe.Read<NamedVariant>(ref data, ref remaining);
                GlobalVars.Set(variant.Id, variant.Value);
            }

            GlobalVars.Optimize();

            int introCount = Unsafe.Read<ushort>(ref data, ref remaining);
            IntroVars.Capacity = Mathf.NextPowerOfTwo(globalCount);
            for (int i = 0; i < introCount; i++) {
                NamedVariant variant = Unsafe.Read<NamedVariant>(ref data, ref remaining);
                IntroVars.Set(variant.Id, variant.Value);
            }

            IntroVars.Optimize();
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref byte* data, ref int written, int capacity, SaveStateChunkConsts consts) {
            Unsafe.Write((ushort) GlobalVars.Count, ref data, ref written, capacity);
            for(int i = 0; i < GlobalVars.Count; i++) {
                Unsafe.Write(GlobalVars[i], ref data, ref written, capacity);
            }

            Unsafe.Write((ushort) IntroVars.Count, ref data, ref written, capacity);
            for (int i = 0; i < IntroVars.Count; i++) {
                Unsafe.Write(IntroVars[i], ref data, ref written, capacity);
            }
        }
    }
}