using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Data;

namespace Zavala.Building {
    public class BuildingPersistence : MonoBehaviour, ISaveStateChunkObject, ISaveStatePostLoad {
        #region Types

        private struct DataRecord {
            public int TileIndex;
            public BuildingType Type;
            public int AuxComponentCount;
            public UnsafeSpan<byte> AuxComponentData;
        }

        #endregion // Types

        private Unsafe.ArenaHandle m_DelayedDataArena;

        private void OnEnable() {
            ZavalaGame.SaveBuffer.RegisterHandler("Buildings", this, 40);
            m_DelayedDataArena = SimAllocator.AllocArena(64 * Unsafe.KiB);
        }

        private void OnDisable() {
            m_DelayedDataArena.Release();
            ZavalaGame.SaveBuffer.DeregisterHandler("Buildings");
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts) {
            var iter = Game.Components.ComponentsOfType<PersistBuilding>(out int persistCount);
            writer.Write(persistCount);

            while(iter.MoveNext()) {
                var comp = iter.Current;
                writer.Write((ushort) comp.Position.TileIndex);
                writer.Write(comp.Position.Type);

                int auxCount = comp.PersistentComponents.Length;
                writer.Write((byte) auxCount);

                byte* dataSizePtr = writer.Head;
                writer.Write((ushort) 0);

                int sizeRef = writer.Written;

                for(int i = 0; i < auxCount; i++) {
                    ((IPersistBuildingComponent) comp.PersistentComponents[i]).Write(comp, ref writer);
                }

                ushort totalSize = (ushort) (writer.Written - sizeRef);
                Unsafe.Copy(&totalSize, sizeof(ushort), dataSizePtr);
            }
        }

        void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts) {
            
        }

        void ISaveStatePostLoad.PostLoad() {
            // ensure user buildings will spawn correctly
        }
    }
}