using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Data;
using Zavala.World;

namespace Zavala.Building {
    public class BuildingPersistence : MonoBehaviour, ISaveStateChunkObject, ISaveStatePostLoad {
        #region Types

        private struct PersistenceRecord {
            public int TileIndex;
            public BuildingType Type;
            public int AuxComponentCount;
            public UnsafeSpan<byte> AuxComponentData;
        }

        #endregion // Types

        private void OnEnable() {
            ZavalaGame.SaveBuffer.RegisterHandler("Buildings", this, 40);
            ZavalaGame.SaveBuffer.RegisterPostLoad(this);
        }

        private void OnDisable() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Buildings");
            ZavalaGame.SaveBuffer.DeregisterPostLoad(this);
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
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

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            var world = ZavalaGame.SimWorld;

            int persistCount = reader.Read<int>();

            var data = scratch.CreateBlock<PersistenceRecord>("BuildingPersistence", persistCount);
            for(int i = 0; i < persistCount; i++) {
                ref PersistenceRecord record = ref data[i];

                record.TileIndex = reader.Read<ushort>();
                record.Type = reader.Read<BuildingType>();
                record.AuxComponentCount = reader.Read<byte>();

                ushort totalSize = reader.Read<ushort>();
                UnsafeSpan<byte> blockCopy = scratch.Alloc<byte>(totalSize);
                Unsafe.Copy(reader.Head, totalSize, blockCopy.Ptr);
                reader.Skip(totalSize);

                record.AuxComponentData = blockCopy;

                if (record.Type == BuildingType.Storage || record.Type == BuildingType.Digester) {
                    world.Spawns.QueuedBuildings.PushBack(new SpawnRecord<BuildingSpawnData>() {
                        TileIndex = (ushort) record.TileIndex,
                        Data = new BuildingSpawnData() {
                            Type = record.Type
                        }
                    });
                }
            }
        }

        unsafe void ISaveStatePostLoad.PostLoad(SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            var data = scratch.GetBlock<PersistenceRecord>("BuildingPersistence");

            var iter = Game.Components.ComponentsOfType<PersistBuilding>(out int persistCount);
            Assert.True(persistCount == data.Length);

            while (iter.MoveNext()) {
                var comp = iter.Current;

                bool found = false;
                for(int i = 0; i < data.Length; i++) {
                    var record = data[i];
                    if (record.TileIndex == comp.Position.TileIndex) {
                        // unpack data
                        ByteReader reader;
                        reader.Head = record.AuxComponentData.Ptr;
                        reader.Remaining = record.AuxComponentData.Length;
                        reader.Tag = default;

                        Assert.True(record.AuxComponentCount == comp.PersistentComponents.Length);

                        for(int compIdx = 0; compIdx < comp.PersistentComponents.Length; compIdx++) {
                            ((IPersistBuildingComponent) comp.PersistentComponents[compIdx]).Read(comp, ref reader);
                        }

                        found = true;
                        break;
                    }
                }

                Assert.True(found, "Could not find data for building '{0}'", comp.name);
            }
        }
    }
}