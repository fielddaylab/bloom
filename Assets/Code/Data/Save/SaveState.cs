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
    }

    public struct SaveStateChunkConsts {
        public int MaxRegions;
        public HexGridSize GridSize;
        public HexGridSubregion DataRegion;
    }

    public unsafe delegate void SaveStateChunkWriter(object context, ref byte* data, ref int bytesWritten, int capacity, SaveStateChunkConsts consts);
    public unsafe delegate void SaveStateChunkReader(object context, ref byte* data, ref int bytesRemaining, SaveStateChunkConsts consts);

    public unsafe interface ISaveStateChunkObject {
        void Write(object self, ref byte* data, ref int written, int capacity, SaveStateChunkConsts consts);
        void Read(object self, ref byte* data, ref int remaining, SaveStateChunkConsts consts);
    }

    public interface ISaveStatePostLoad {
        void PostLoad();
    }
}