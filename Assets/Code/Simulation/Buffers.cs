using System.Collections.Generic;
using Zavala.Roads;

namespace Zavala.Sim {
    /// <summary>
    /// Struct containing terrain information buffers.
    /// </summary>
    public unsafe struct TerrainBuffers {
        // terrain information
        public SimBuffer<TerrainTileInfo> Info;
        public SimBuffer<ushort> Height;
        public SimBuffer<ushort> Regions;

        public void Create(in HexGridSize size) {
            Info = SimBuffer.Create<TerrainTileInfo>(size);
            SimBuffer.Clear(Info, TerrainTileInfo.Invalid);

            Height = SimBuffer.Create<ushort>(size);
            Regions = SimBuffer.Create<ushort>(size);

            SimBuffer.Clear(Regions, (ushort) 0);
        }
    }

    /// <summary>
    /// Struct containing phosphorus information buffers.
    /// </summary>
    public unsafe struct PhosphorusBuffers {
        public SimBuffer<PhosphorusTileInfo> Info;
        public SimBuffer<PhosphorusTileState>[] States;
        public int StateIndex;

        public PhosphorusChangeBuffer Changes;

        public void Create(in HexGridSize size) {
            Info = SimBuffer.Create<PhosphorusTileInfo>(size);
            SimBuffer.Clear(Info);

            States = new SimBuffer<PhosphorusTileState>[2];
            States[0] = SimBuffer.Create<PhosphorusTileState>(size);
            States[1] = SimBuffer.Create<PhosphorusTileState>(size);

            SimBuffer.Clear(States[0]);
            SimBuffer.Clear(States[1]);

            StateIndex = 0;
            Changes.Create();
        }

        public SimBuffer<PhosphorusTileState> CurrentState() {
            return States[StateIndex];
        }

        public SimBuffer<PhosphorusTileState> NextState() {
            return States[1 - StateIndex];
        }
    }

    /// <summary>
    /// Struct containing road information buffers.
    /// </summary>
    public unsafe struct RoadBuffers {
        public SimBuffer<RoadTileInfo> Info;

        public void Create(in HexGridSize size) {
            Info = SimBuffer.Create<RoadTileInfo>(size);
            SimBuffer.Clear(Info);
        }
    }

    /// <summary>
    /// Struct containing algae information buffers.
    /// </summary>
    public unsafe struct AlgaeBuffers {
        // no need for multiple states - just the current one?
        public SimBuffer<AlgaeTileState> State;
        /// <summary>
        /// Track tiles which have phosphorus level above growth threshold
        /// </summary>
        public HashSet<int> GrowingTiles;

        public HashSet<int> PeakingTiles;

        public void Create(in HexGridSize size) {
            // TODO: only create and track AlgaeTileState for water tiles?
            State = SimBuffer.Create<AlgaeTileState>(size);
            GrowingTiles = new HashSet<int>(200);
            PeakingTiles = new HashSet<int>(200);
        }
    }

}