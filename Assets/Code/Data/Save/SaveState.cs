using BeauUtil;

namespace Zavala.Data {
    public struct SaveStateHeader {
        public string PlayerCode;
        public long LastSaveTS;
        public double Playtime;
    }

    public struct SaveStateManifest {
        public uint Length;
        public uint ChunkCount;
        public ulong Checksum;
    }

    public struct SaveStateChunkHeader {
        public StringHash32 Id;
        public uint ChunkLength;
        public uint ChunkLengthUncompressed;
    }

    public struct SaveStateChunkConsts {
        public int MaxRegions;
        public HexGridSize GridSize;
        public HexGridSubregion DataRegion;
    }

    public delegate void SaveStateChunkWriter(object context, ref ByteWriter writer, SaveStateChunkConsts consts);
    public delegate void SaveStateChunkReader(object context, ref ByteReader reader, SaveStateChunkConsts consts);

    public interface ISaveStateChunkObject {
        void Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts);
        void Read(object self, ref ByteReader reader, SaveStateChunkConsts consts);
    }

    public interface ISaveStatePostLoad {
        void PostLoad();
    }
}