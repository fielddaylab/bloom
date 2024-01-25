using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using Zavala.Sim;

namespace Zavala.Data {
    public class SaveMgr {
        private struct ChunkRecord {
            public StringHash32 Id;
            public uint UncompressedSize;
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

        public const int TotalBufferSize = 256 * Unsafe.KiB; // 256k buffer
        public const int MainBufferSize = 128 * Unsafe.KiB; // 128k buffer
        public const int ChunkBufferSize = 64 * Unsafe.KiB; // 64k buffer
        public const int ScratchBufferSize = 64 * Unsafe.KiB; // 64k buffer

        private unsafe byte* m_Buffer;
        private unsafe byte* m_ChunkBuffer;
        private unsafe Unsafe.ArenaHandle m_ScratchBuffer;

        private int m_UsedSize;
        private SaveStateHeader m_CurrentHeader;
        private SaveStateChunkConsts m_CurrentConsts;
        private SaveScratchpad m_CurrentScratch;
        private RingBuffer<ChunkRecord> m_ChunkRecords = new RingBuffer<ChunkRecord>(32, RingBufferMode.Expand);
        private StringHash32 m_ActiveChunk;

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
            m_Buffer = (byte*) Unsafe.Alloc(TotalBufferSize);
            m_ChunkBuffer = m_Buffer + MainBufferSize;
            m_ScratchBuffer = Unsafe.CreateArena(m_ChunkBuffer + ChunkBufferSize, ScratchBufferSize);
        }

        public unsafe void Free() {
            if (m_Buffer != null) {
                Unsafe.Free(m_Buffer);
                m_Buffer = null;
                m_ChunkBuffer = null;
                m_ScratchBuffer = default;
            }
        }

        #region Handlers

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

        #endregion // Handlers

        #region Write

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
            ByteWriter writer;
            writer.Head = m_Buffer;
            writer.Capacity = MainBufferSize;
            writer.Written = 0;
            writer.Tag = default;

            m_ChunkRecords.Clear();

            if (m_WriterOrderDirty) {
                m_ChunkWriters.Sort((a, b) => a.Order - b.Order);
                m_WriterOrderDirty = false;
            }

            writer.WriteUTF8(header.PlayerCode);
            writer.Write(header.LastSaveTS);
            writer.Write(header.Playtime);

            writer.Write((byte) consts.MaxRegions);
            writer.Write((byte) consts.GridSize.Width);
            writer.Write((byte) consts.GridSize.Height);
            writer.Write((byte) consts.DataRegion.X);
            writer.Write((byte) consts.DataRegion.Y);
            writer.Write((byte) consts.DataRegion.Width);
            writer.Write((byte) consts.DataRegion.Height);
            writer.Skip(1); // padding

            byte* manifestWriteMarker = writer.Head;

            SaveStateManifest manifest;
            manifest.ChunkCount = (uint) m_ChunkWriters.Count;
            manifest.Checksum = 0;
            manifest.Length = 0;

            writer.Write(manifest);

            m_ScratchBuffer.Reset();

            SaveScratchpad scratch;
            scratch.Allocator = m_ScratchBuffer;
            scratch.BlockCount = 0;
            scratch.Blocks = m_ScratchBuffer.AllocSpan<SaveScratchBlock>(32);
            m_CurrentScratch = scratch;

            byte* manifestLengthChecksumMarker = writer.Head;
            int manifestLengthCalcMarker = writer.Written;

            foreach(var chunk in m_ChunkWriters) {
                byte* chunkHeaderWriteMarker = writer.Head;

                SaveStateChunkHeader chunkHeader;
                chunkHeader.Id = chunk.Id;
                chunkHeader.ChunkLength = 0;
                chunkHeader.ChunkLengthUncompressed = 0;

                writer.Write(chunkHeader);

                ByteWriter chunkWriter;
                chunkWriter.Head = m_ChunkBuffer;
                chunkWriter.Written = 0;
                chunkWriter.Capacity = ChunkBufferSize;
                chunkWriter.Tag = chunk.Id;

                chunk.Writer(chunk.Context, ref chunkWriter, consts, ref m_CurrentScratch);

                byte* chunkDataStart = writer.Head;
                int compressedSize;

                bool compressed = UnsafeExt.Compress(m_ChunkBuffer, chunkWriter.Written, chunkDataStart, writer.Capacity - writer.Written, &compressedSize);
                writer.Head += compressedSize;
                writer.Written += compressedSize;

                chunkHeader.ChunkLength = (uint) compressedSize;
                chunkHeader.ChunkLengthUncompressed = (uint) chunkWriter.Written;

                Unsafe.Copy(&chunkHeader, sizeof(SaveStateChunkHeader), chunkHeaderWriteMarker);

                m_ChunkRecords.PushBack(new ChunkRecord() {
                    Id = chunkHeader.Id,
                    Data = new UnsafeSpan<byte>(chunkDataStart, chunkHeader.ChunkLength),
                    UncompressedSize = chunkHeader.ChunkLengthUncompressed
                });

                Log.Msg("[SaveMgr] Wrote chunk '{0}' ({1}, {2} uncompressed)", chunk.Id, Unsafe.FormatBytes(chunkHeader.ChunkLength), Unsafe.FormatBytes(chunkHeader.ChunkLengthUncompressed));
                //Log.Msg("...compressed " + Unsafe.DumpMemory(chunkDataStart, chunkHeader.ChunkLength, ' ', 2));
                //Log.Msg("...uncompressed " + Unsafe.DumpMemory(m_ChunkBuffer, chunkWriter.Written, ' ', 2));
            }

            manifest.Length = (uint) (writer.Written - manifestLengthCalcMarker);
            manifest.Checksum = Unsafe.Hash64(manifestLengthChecksumMarker, (int) manifest.Length);

            Unsafe.Copy(&manifest, sizeof(SaveStateManifest), manifestWriteMarker);

            m_UsedSize = writer.Written;

            Log.Msg("[SaveMgr] Wrote save data ({0} chunks, {1})", manifest.ChunkCount, Unsafe.FormatBytes(m_UsedSize));
        }

        #endregion // Write

        #region Read

        public bool HasSave {
            get { return m_UsedSize > 0; }
        }

        public unsafe UnsafeSpan<byte> GetDecodeBuffer() {
            return new UnsafeSpan<byte>(m_Buffer, MainBufferSize);
        }

        public unsafe void Read() {
            Read(new UnsafeSpan<byte>(m_Buffer, (uint) m_UsedSize));
        }

        public unsafe void Read(UnsafeSpan<byte> bytes) {
            if (bytes.Ptr != m_Buffer) {
                Unsafe.Copy(bytes.Ptr, bytes.Length, m_Buffer, MainBufferSize);
            }

            m_UsedSize = bytes.Length;

            ByteReader reader;
            reader.Head = m_Buffer;
            reader.Remaining = bytes.Length;
            reader.Tag = default;

            SaveStateHeader header;
            header.PlayerCode = reader.ReadUTF8();
            header.LastSaveTS = reader.Read<long>();
            header.Playtime = reader.Read<double>();
            m_CurrentHeader = header;

            SaveStateChunkConsts consts;
            consts.MaxRegions = reader.Read<byte>();

            byte gridWidth = reader.Read<byte>(),
                gridHeight = reader.Read<byte>();

            consts.GridSize = new HexGridSize(gridWidth, gridHeight);

            byte dataX = reader.Read<byte>(),
                dataY = reader.Read<byte>(),
                dataW = reader.Read<byte>(),
                dataH = reader.Read<byte>();
            reader.Skip(1);

            consts.DataRegion = new HexGridSubregion(consts.GridSize).Subregion(dataX, dataY, dataW, dataH);
            m_CurrentConsts = consts;

            SaveScratchpad scratch;
            scratch.Allocator = m_ScratchBuffer;
            m_ScratchBuffer.Reset();
            scratch.BlockCount = 0;
            scratch.Blocks = m_ScratchBuffer.AllocSpan<SaveScratchBlock>(32);
            m_CurrentScratch = scratch;

            SaveStateManifest manifest = reader.Read<SaveStateManifest>();
            if (manifest.Length != reader.Remaining) {
                Log.Error("[SaveMgr] Mismatch between read bytes ({0}) and manifest bytes ({1})", reader.Remaining, manifest.Length);
            }

            ulong calculatedChecksum = Unsafe.Hash64(reader.Head, reader.Remaining);
            if (calculatedChecksum != manifest.Checksum) {
                Log.Error("[SaveMgr] Checksum failed - calculated {0} but expected {1}", calculatedChecksum, manifest.Checksum);
            }

            Log.Msg("[SaveMgr] Reading save data ({0} chunks, {1})", manifest.ChunkCount, Unsafe.FormatBytes(m_UsedSize));

            m_ChunkRecords.Clear();

            int chunkCount = (int) manifest.ChunkCount;
            while(chunkCount-- > 0) {
                SaveStateChunkHeader chunkHeader = reader.Read<SaveStateChunkHeader>();
                m_ChunkRecords.PushBack(new ChunkRecord() {
                    Id = chunkHeader.Id,
                    UncompressedSize = chunkHeader.ChunkLengthUncompressed,
                    Data = new UnsafeSpan<byte>(reader.Head, chunkHeader.ChunkLength)
                });
                Log.Msg("[SaveMgr] Read chunk '{0}' ({1}, {2} uncompressed)", chunkHeader.Id, Unsafe.FormatBytes(chunkHeader.ChunkLength), Unsafe.FormatBytes(chunkHeader.ChunkLengthUncompressed));
                //Log.Msg("...compressed " + Unsafe.DumpMemory(reader.Head, chunkHeader.ChunkLength, ' ', 2));
                reader.Skip((int) chunkHeader.ChunkLength);
            }

            Assert.True(reader.Remaining == 0);
        }

        public void HandleChunks() {
            foreach (var chunk in m_ChunkRecords) {
                if (m_ChunkReaders.TryGetValue(chunk.Id, out ChunkReader reader)) {
                    ByteReader bytes = UnpackChunkRecord(chunk);
                    reader.Reader(reader.Context, ref bytes, m_CurrentConsts, ref m_CurrentScratch);
                    ReleaseChunk(bytes);
                }
            }
        }

        public void HandlePostLoad() {
            foreach (var postLoad in m_PostLoadHandlers) {
                postLoad.PostLoad(this, m_CurrentConsts, ref m_CurrentScratch);
            }
        }

        public unsafe ByteReader ReadChunk(StringHash32 chunkId) {
            for(int i = 0; i < m_ChunkRecords.Count; i++) {
                ChunkRecord record = m_ChunkRecords[i];
                if (record.Id == chunkId) {
                    return UnpackChunkRecord(record);
                }
            }

            Log.Error("[SaveMgr] No chunk with id '{0}' found", chunkId);
            return default;
        }

        private unsafe ByteReader UnpackChunkRecord(ChunkRecord record) {
            if (m_ActiveChunk != record.Id) {
                if (!m_ActiveChunk.IsEmpty) {
                    Log.Warn("[SaveMgr] Active chunk changing from '{0}' to '{1}'", m_ActiveChunk, record.Id);
                } else {
                    Log.Msg("[SaveMgr] Active chunk '{0}' ", record.Id);
                }
            }

            m_ActiveChunk = record.Id;
            if (record.UncompressedSize == record.Data.Length) {
                //Log.Msg("...uncompressed " + Unsafe.DumpMemory(record.Data.Ptr, record.Data.Length, ' ', 2));
                return new ByteReader() {
                    Head = record.Data.Ptr,
                    Remaining = record.Data.Length,
                    Tag = record.Id
                };
            } else {
                int decompressedSize;
                bool decompressed = UnsafeExt.Decompress(record.Data.Ptr, record.Data.Length, m_ChunkBuffer, ChunkBufferSize, &decompressedSize);
                Assert.True(decompressed && record.UncompressedSize == decompressedSize);
                //Log.Msg("...uncompressed " + Unsafe.DumpMemory(m_ChunkBuffer, decompressedSize, ' ', 2));
                return new ByteReader() {
                    Head = m_ChunkBuffer,
                    Remaining = decompressedSize,
                    Tag = record.Id
                };
            }
        }

        public void ReleaseChunk(ByteReader reader) {
            if (!reader.Tag.IsEmpty && m_ActiveChunk == reader.Tag) {
                m_ActiveChunk = default;
                Log.Msg("[SaveMgr] Chunk '{0}' released", reader.Tag);
            }
        }

        #endregion // Read

        [DebugMenuFactory]
        static private DMInfo SaveDebugMenu() {
            DMInfo info = new DMInfo("Save", 4);
            info.AddButton("Write Current to Memory", () => ZavalaGame.SaveBuffer.Write());
            info.AddButton("Read Current from Memory", () => {
                ZavalaGame.SaveBuffer.Read();
                Game.Scenes.ReloadMainScene();
            }, () => ZavalaGame.SaveBuffer.HasSave);
            return info;
        }
    }
}