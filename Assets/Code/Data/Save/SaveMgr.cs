using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using Zavala.Sim;

namespace Zavala.Data {
    public class SaveMgr {
        private struct ChunkRecord {
            public StringHash32 Id;
            public UnsafeSpan<byte> Data;
        }

        private struct ChunkReader {
            public SaveStateChunkReader Reader;
            public object Context;
        }

        private struct ChunkWriter {
            public StringHash32 Id;
            public int Order;
            public SaveStateChunkWriter Writer;
            public object Context;
        }

        public const int BufferSize = 512 * Unsafe.KiB; // 512k buffer

        private unsafe byte* m_Buffer;
        private int m_UsedSize;
        private RingBuffer<ChunkRecord> m_ChunkRecords = new RingBuffer<ChunkRecord>(32, RingBufferMode.Expand);

        private readonly RingBuffer<ChunkWriter> m_ChunkWriters = new RingBuffer<ChunkWriter>(32, RingBufferMode.Expand);
        private bool m_WriterOrderDirty = false;

        private readonly Dictionary<StringHash32, ChunkReader> m_ChunkReaders;
        private readonly RingBuffer<ISaveStatePostLoad> m_PostLoadHandlers = new RingBuffer<ISaveStatePostLoad>(32, RingBufferMode.Expand);

        public SaveMgr() {
            m_ChunkReaders = MapUtils.Create<StringHash32, ChunkReader>(32);
            Allocate();
        }

        public unsafe void Allocate() {
            Free();
            m_Buffer = (byte*) Unsafe.Alloc(BufferSize);
        }

        public unsafe void Free() {
            if (m_Buffer != null) {
                Unsafe.Free(m_Buffer);
                m_Buffer = null;
            }
        }

        public unsafe void RegisterHandler(StringHash32 id, ISaveStateChunkObject chunkObj, int order = 0) {
            RegisterHandler(id, chunkObj, chunkObj.Write, chunkObj.Read, order);
        }

        public unsafe void RegisterHandler(StringHash32 id, object context, SaveStateChunkWriter writer, SaveStateChunkReader reader, int order = 0) {
            m_ChunkWriters.PushBack(new ChunkWriter() {
                Context = context,
                Id = id,
                Writer = writer,
                Order = order
            });
            m_WriterOrderDirty = true;

            m_ChunkReaders.Add(id, new ChunkReader() {
                Context = context,
                Reader = reader
            });
        }

        public void RegisterPostLoad(ISaveStatePostLoad postLoad) {
            m_PostLoadHandlers.PushBack(postLoad);
        }

        public void DeregisterHandler(StringHash32 id) {
            m_ChunkReaders.Remove(id);

            int chunkWriterIdx = m_ChunkWriters.FindIndex((w, i) => w.Id == i, id);
            if (chunkWriterIdx >= 0) {
                m_ChunkWriters.FastRemoveAt(chunkWriterIdx);
                m_WriterOrderDirty = true;
            }
        }

        public void DeregisterPostLoad(ISaveStatePostLoad postLoad) {
            m_PostLoadHandlers.FastRemove(postLoad);
        }

        public void Write() {
            SaveStateHeader header;
            header.PlayerCode = "DEBUG";
            header.LastSaveTS = DateTime.UtcNow.ToFileTimeUtc();
            header.Playtime = 0;

            SaveStateChunkConsts consts;
            consts.GridSize = ZavalaGame.SimGrid.HexSize;
            consts.MaxRegions = RegionInfo.MaxRegions;
            consts.DataRegion = ZavalaGame.SimGrid.SimulationRegion;

            Write(header, consts);
        }

        public unsafe void Write(SaveStateHeader header, SaveStateChunkConsts consts) {
            byte* head = m_Buffer;
            int totalSize = 0;

            m_ChunkRecords.Clear();

            if (m_WriterOrderDirty) {
                m_ChunkWriters.Sort((a, b) => a.Order - b.Order);
                m_WriterOrderDirty = false;
            }

            Unsafe.WriteUTF8(header.PlayerCode, ref head, ref totalSize, BufferSize);
            Unsafe.Write(header.LastSaveTS, ref head, ref totalSize, BufferSize);
            Unsafe.Write(header.Playtime, ref head, ref totalSize, BufferSize);

            Unsafe.Write((byte) consts.MaxRegions, ref head, ref totalSize, BufferSize);
            Unsafe.Write((byte) consts.GridSize.Width, ref head, ref totalSize, BufferSize);
            Unsafe.Write((byte) consts.GridSize.Height, ref head, ref totalSize, BufferSize);
            Unsafe.Write((byte) consts.DataRegion.X, ref head, ref totalSize, BufferSize);
            Unsafe.Write((byte) consts.DataRegion.Y, ref head, ref totalSize, BufferSize);
            Unsafe.Write((byte) consts.DataRegion.Width, ref head, ref totalSize, BufferSize);
            Unsafe.Write((byte) consts.DataRegion.Height, ref head, ref totalSize, BufferSize);
            Unsafe.Write((byte) 0, ref head, ref totalSize, BufferSize); // padding

            byte* manifestWriteMarker = head;

            SaveStateManifest manifest;
            manifest.ChunkCount = (uint) m_ChunkWriters.Count;
            manifest.Checksum = 0;
            manifest.Length = 0;

            Unsafe.Write(manifest, ref head, ref totalSize, BufferSize);

            byte* manifestLengthChecksumMarker = head;
            int manifestLengthCalcMarker = totalSize;

            foreach(var chunk in m_ChunkWriters) {
                byte* chunkHeaderWriteMarker = head;

                SaveStateChunkHeader chunkHeader;
                chunkHeader.Id = chunk.Id;
                chunkHeader.ChunkLength = 0;

                Unsafe.Write(chunkHeader, ref head, ref totalSize, BufferSize);

                int chunkLengthCalcMarker = totalSize;

                chunk.Writer(chunk.Context, ref head, ref totalSize, BufferSize, consts);

                byte* chunkDataStart = head;
                chunkHeader.ChunkLength = (uint) (totalSize - chunkLengthCalcMarker);

                Unsafe.Copy(&chunkHeader, sizeof(SaveStateChunkHeader), chunkHeaderWriteMarker);

                m_ChunkRecords.PushBack(new ChunkRecord() {
                    Id = chunkHeader.Id,
                    Data = new UnsafeSpan<byte>(chunkDataStart, chunkHeader.ChunkLength)
                });
            }

            manifest.Length = (uint) (totalSize - manifestLengthCalcMarker);
            manifest.Checksum = Unsafe.Hash64(manifestLengthChecksumMarker, (int) manifest.Length);

            Unsafe.Copy(&manifest, sizeof(SaveStateManifest), manifestWriteMarker);

            m_UsedSize = totalSize;

            Log.Msg("[SaveMgr] Wrote save data ({0} chunks, {1})", manifest.ChunkCount, Unsafe.FormatBytes(m_UsedSize));
        }

        [DebugMenuFactory]
        static private DMInfo SaveDebugMenu() {
            DMInfo info = new DMInfo("Save", 4);
            info.AddButton("Write Current to Memory", () => ZavalaGame.SaveBuffer.Write());
            return info;
        }
    }
}