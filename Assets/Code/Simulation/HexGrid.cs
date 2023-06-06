using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace Zavala.Sim {

    /// <summary>
    /// Hex grid size and helper struct for space/index conversions.
    /// Every other column is offset by 1/2 a space in a positive direction (down).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{Width}x{Height}")]
    public unsafe readonly struct HexGridSize {
        
        public readonly uint Width;
        public readonly uint Height;
        public readonly uint Size;

        // index offset table (stored this way so we can use the "readonly" modifier)
        // using the "fixed" keyword doesn't allow it to be marked as readonly unfortunately :(
        private readonly short m_IndexOffset00;
        private readonly short m_IndexOffset01;
        private readonly short m_IndexOffset02;
        private readonly short m_IndexOffset03;
        private readonly short m_IndexOffset04;
        private readonly short m_IndexOffset05;
        private readonly short m_IndexOffset06;
        private readonly short m_IndexOffset07;
        private readonly short m_IndexOffset08;
        private readonly short m_IndexOffset09;
        private readonly short m_IndexOffset10;
        private readonly short m_IndexOffset11;
        private readonly short m_IndexOffset12;
        private readonly short m_IndexOffset13;

        public HexGridSize(uint width, uint height) : this() {
            Width = width;
            Height = height;
            Size = width * height;

            fixed(short* idxBuffer = &m_IndexOffset00) {
                for(int i = 0; i < 14; i++) {
                    idxBuffer[i] = (short) PointOffsetToIndexOffset(s_PointOffsets[i], Width);
                }
            }
        }

        #region Validation

        /// <summary>
        /// Returns if the given point is within the grid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidPos(HexVector point) {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
        }

        /// <summary>
        /// Returns if the given index is within the grid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidIndex(int index) {
            return index >= 0 && index < Size;
        }

        #endregion // Validation

        #region Offsets

        /// <summary>
        /// Returns the tile index corresponding to the given tile direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int OffsetIndexFrom(int index, TileDirection direction) {
            fixed(short* idxBuffer = &m_IndexOffset00) {
                return index + idxBuffer[DirToTableIdx(direction, index, Width)];
            }
        }

        /// <summary>
        /// Returns the point offset for the given direction.
        /// </summary>
        public HexVector OffsetPosFrom(HexVector point, TileDirection direction) {
            return point + s_PointOffsets[DirToTableIdx(direction, point.X)];
        }

        /// <summary>
        /// Returns if the given position relative to the current vector is valid.
        /// </summary>
        public bool IsValidPosOffset(HexVector point, TileDirection direction) {
            HexVector target = point + s_PointOffsets[DirToTableIdx(direction, point.X)];
            return target.X >= 0 && target.X < Width && target.Y >= 0 && target.Y < Height;
        }

        /// <summary>
        /// Returns if the given position relative to the current vector is valid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidIndexOffset(int index, TileDirection direction) {
            return IsValidPosOffset(IndexToPos(index), direction);
        }

        #endregion // Offsets

        #region Position To Index

        /// <summary>
        /// Converts the given point to a tile index.
        /// This will return -1 for any out-of-bounds tile positions.
        /// </summary>
        public int PosToIndex(int x, int y) {
            if (x < 0 || x >= Width || y < 0 || y >= Height) {
                return -1;
            }

            return x + (int) (y * Width);
        }

        /// <summary>
        /// Converts the given point to a tile index.
        /// This will return -1 for any out-of-bounds tile positions.
        /// </summary>
        public int PosToIndex(HexVector point) {
            if (point.X < 0 || point.X >= Width || point.Y < 0 || point.Y >= Height) {
                return -1;
            }

            return point.X + (int) (point.Y * Width);
        }

        /// <summary>
        /// Converts the given point to a tile index.
        /// This does no bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FastPosToIndex(int x, int y) {
            return x + (int) (y * Width);
        }

        /// <summary>
        /// Converts the given point to a tile index.
        /// This does no bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FastPosToIndex(HexVector point) {
            return point.X + (int) (point.Y * Width);
        }

        #endregion // Position To Index

        #region Index to Position

        /// <summary>
        /// Converts the given tile index into a point.
        /// Out of bounds indices return -1, -1
        /// </summary>
        public HexVector IndexToPos(int index) {
            if (index < 0 || index >= Size) {
                return new HexVector(-1, -1);
            }

            return new HexVector((int) (index % Width), (int) (index / Width));
        }

        /// <summary>
        /// Converts the given tile index into a pair of coordinates.
        /// Returns false if given an out-of-bounds index.
        /// </summary>
        public bool IndexToPos(int index, out int x, out int y) {
            if (index < 0 || index >= Size) {
                x = y = -1;
                return false;
            }

            x= (int) (index % Width);
            y = (int) (index / Width);
            return true;
        }

        /// <summary>
        /// Converts the given tile index into a point.
        /// This does not bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HexVector FastIndexToPos(int index) {
            return new HexVector((int) (index % Width), (int) (index / Width));
        }

        /// <summary>
        /// Converts the given tile index into a pair of coordinates.
        /// This does not bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastIndexToPos(int index, out int x, out int y) {
            x = (int) (index % Width);
            y = (int) (index % Height);
        }

        #endregion // Index to Position
    
        #region Tables

        // Important notes on the offset tables
        // Hex grids can be treated internally the same as standard repeating grids
        // Every other column is offset in the -y direction by half a tile
        // Adjacency rules differ per column type.
        // Let's call even index columns "type 0" and odd index columns "type 1"
        // 
        // Type 0 columns have the following adjacency pattern:
        //      X X X
        //      X o X
        //        X
        // Whereas Type 1 columns have this pattern:
        //        X
        //      X o X
        //      X X X
        // These offsets are stored in tables in pairs of entries
        // The tables are laid out such that:
        //      offset = 2 * (int) direction + type
        // The HexVector offsets are consistent for any size
        // The Index offsets depend on the grid width

        /// <summary>
        /// HexVector offset table per direction.
        /// </summary>
        static private readonly HexVector[] s_PointOffsets = new HexVector[]
        {
            new HexVector(0, 0),
            new HexVector(0, 0),

            new HexVector(-1, 0),
            new HexVector(-1, -1),

            new HexVector(0, -1),
            new HexVector(0, -1),

            new HexVector(1, 0),
            new HexVector(1, -1),

            new HexVector(1, 1),
            new HexVector(1, 0),

            new HexVector(0, 1),
            new HexVector(0, 1),

            new HexVector(-1, 1),
            new HexVector(-1, 0),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private int PointOffsetToIndexOffset(HexVector offset, uint width) {
            return offset.X + (int) width * offset.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private int DirToTableIdx(TileDirection direction, int index, uint width) {
            return (2 * (int) direction) + ((index % (int) width) % 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private int DirToTableIdx(TileDirection direction, int x) {
            return (2 * (int) direction) + (x % 2);
        }

        #endregion // Tables

        #region Enumerator

        public IndexEnumerator GetEnumerator() {
            return new IndexEnumerator((int) Size);
        }

        public PosEnumerator GetPosEnumerator() {
            return new PosEnumerator((int) Size, (int) Width);
        }

        public struct IndexEnumerator : IEnumerator<int> {
            private readonly int m_Size;
            private int m_Current;

            public IndexEnumerator(int size) {
                m_Size = size;
                m_Current = -1;
            }

            public int Current { get { return m_Current; } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() {
            }

            public bool MoveNext() {
                return ++m_Current < m_Size;
            }

            public void Reset() {
                m_Current = -1;
            }
        }

        public struct PosEnumerator : IEnumerator<HexVector> {
            private readonly int m_Size;
            private readonly int m_Width;
            private int m_Current;

            public PosEnumerator(int size, int width) {
                m_Size = size;
                m_Width = width;
                m_Current = -1;
            }

            public HexVector Current { get { return new HexVector(m_Current % m_Width, m_Current / m_Width); } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() {
            }

            public bool MoveNext() {
                return ++m_Current < m_Size;
            }

            public void Reset() {
                m_Current = -1;
            }
        }

        #endregion // Enumerator
    }

    /// <summary>
    /// Hex grid vector of ints.
    /// </summary>
    [DebuggerDisplay("[{X},{Y}]")]
    public struct HexVector : IEquatable<HexVector> {
        public int X;
        public int Y;

        public HexVector(int x, int y) {
            X = x;
            Y = y;
        }

        #region World Position

        public Vector3 ToWorld(float height) {
            return new Vector3(X, height, (Y - (X % 2) * 0.5f));
        }

        public Vector3 ToWorld(float height, float scale) {
            return new Vector3(X * scale, height * scale, scale * (Y - (X % 2) * 0.5f));
        }

        public Vector3 ToWorld(float height, in HexGridWorldSpace worldSpace) {
            return new Vector3(worldSpace.Offset.x + (X - worldSpace.GridWidth / 2f) * worldSpace.Scale.x,
                worldSpace.Offset.y + height * worldSpace.Scale.y,
                worldSpace.Offset.z + (Y - (X % 2) * 0.5f - worldSpace.GridHeight / 2f) * worldSpace.Scale.z
            );
        }

        static public Vector3 ToWorld(int index, float height, in HexGridWorldSpace worldSpace) {
            return new HexVector(index % (int) worldSpace.GridWidth, index / (int) worldSpace.GridWidth).ToWorld(height, worldSpace);
        }

        static public HexVector FromWorld(Vector3 position) {
            int x = (int) Math.Round(position.x);
            int y = (int) Math.Round(position.z + (x % 2) * 0.5f);
            return new HexVector(x, y);
        }

        static public HexVector FromWorld(Vector3 position, float scale) {
            int x = (int) Math.Round(position.x / scale);
            int y = (int) Math.Round((position.z / scale) + (x % 2) * 0.5f);
            return new HexVector(x, y);
        }

        static public HexVector FromWorld(Vector3 position, in HexGridWorldSpace worldSpace) {
            int x = (int) Math.Round(((position.x - worldSpace.Offset.x) / worldSpace.Scale.x) + worldSpace.GridWidth / 2f);
            int y = (int) Math.Round(((position.z - worldSpace.Offset.z) / worldSpace.Scale.z) + (x % 2) * 0.5f + worldSpace.GridHeight / 2f);
            return new HexVector(x, y);
        }

        #endregion // World Position

        #region Overrides

        public override bool Equals(object obj) {
            if (obj is HexVector) {
                return Equals((HexVector) obj);
            }

            return false;
        }

        public override int GetHashCode() {
            return 3 * (X << 14) ^ Y;
        }

        public bool Equals(HexVector other) {
            return X == other.X && Y == other.Y;
        }

        static public HexVector operator+(HexVector a, HexVector b) {
            return new HexVector(a.X + b.X, a.Y + b.Y);
        }

        static public HexVector operator-(HexVector a, HexVector b) {
            return new HexVector(a.X - b.X, a.Y - b.Y);
        }

        static public bool operator==(HexVector a, HexVector b) {
            return a.X == b.X && a.Y == b.Y;
        }

        static public bool operator!=(HexVector a, HexVector b) {
            return a.X != b.X || a.Y != b.Y;
        }

        #endregion // Overrides
    }

    /// <summary>
    /// Subregion of a hex grid.
    /// </summary>
    [DebuggerDisplay("[{X},{Y},{Width}x{Height}]")]
    public readonly struct HexGridSubregion : IEquatable<HexGridSubregion> {
        public readonly uint Size;
        public readonly ushort X;
        public readonly ushort Y;
        public readonly ushort Width;
        public readonly ushort Height;
        private readonly ushort m_SrcWidth;

        public HexGridSubregion(HexGridSize fullSize) {
            X = 0;
            Y = 0;
            Width = (ushort) fullSize.Width;
            Height = (ushort) fullSize.Height;
            m_SrcWidth = (ushort) fullSize.Width;
            Size = fullSize.Size;
        }

        private HexGridSubregion(ushort x, ushort y, ushort width, ushort height, ushort srcWidth) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Size = (uint) (Width * Height);
            m_SrcWidth = srcWidth;
        }

        public HexGridSubregion Subregion(ushort x, ushort y, ushort width, ushort height) {
            ushort subX = (ushort) (X + x);
            ushort subY = (ushort) (Y + Y);
            if (subX + width > X + Width || subY + height > Y + Height) {
                throw new ArgumentOutOfRangeException();
            }
            return new HexGridSubregion(subX, subY, width, height, m_SrcWidth);
        }

        #region Validation

        /// <summary>
        /// Returns if the given position is within the subregion.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsPos(HexVector pos) {
            return pos.X >= X && pos.Y >= Y
                && pos.X < X + Width && pos.Y < Y + Height;
        }

        /// <summary>
        /// Returns if the given grid index is contained within this subregion.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsGridIndex(int index) {
            int gridX = index % m_SrcWidth;
            int gridY = index / m_SrcWidth;
            return gridX >= X && gridY >= Y
                && gridX < X + Width && gridY < Y + Height;
        }

        #endregion // Validation

        #region Conversions

        /// <summary>
        /// Converts a local index to a grid position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HexVector FastIndexToPos(int index) {
            return new HexVector(X + (index % Width), Y + (index / Width));
        }

        /// <summary>
        /// Converts a local index to a grid index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FastIndexToGridIndex(int index) {
            int gridX = X + (index % Width);
            int gridY = Y + (index / Width);
            return gridX + gridY * m_SrcWidth;
        }

        #endregion // Conversions

        #region Overrides

        public bool Equals(HexGridSubregion other) {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj) {
            if (obj is HexGridSubregion) {
                return Equals((HexGridSubregion) obj);
            }

            return false;
        }

        public override int GetHashCode() {
            int hash = X.GetHashCode();
            hash = hash << 7 ^ Y.GetHashCode();
            hash = hash >> 3 ^ Width.GetHashCode();
            hash = hash << 5 & Height.GetHashCode();
            return hash;
        }

        #endregion // Overrides
    
        #region Enumerators

        public IndexEnumerator GetEnumerator() {
            return new IndexEnumerator(this);
        }

        public struct IndexEnumerator : IEnumerator<int> {
            private readonly HexGridSubregion m_Region;
            private int m_Current;

            public IndexEnumerator(HexGridSubregion subregion) {
                m_Region = subregion;
                m_Current = -1;
            }

            public int Current { get { return m_Region.FastIndexToGridIndex(m_Current); } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() {
            }

            public bool MoveNext() {
                return ++m_Current < m_Region.Size;
            }

            public void Reset() {
                m_Current = -1;
            }
        }

        #endregion // Enumerators
    }

    /// <summary>
    /// Tile directions.
    /// </summary>
    public enum TileDirection : byte {
        Self,
        SW,
        S,
        SE,
        NE,
        N,
        NW,

        COUNT
    }

    /// <summary>
    /// Grid space to world space mapping parameters.
    /// </summary>
    public readonly struct HexGridWorldSpace {
        public readonly uint GridWidth;
        public readonly uint GridHeight;
        public readonly Vector3 Scale;
        public readonly Vector3 Offset;

        public HexGridWorldSpace(HexGridSize size) {
            GridWidth = size.Width;
            GridHeight = size.Height;
            Scale = new Vector3(1, 1, 1);
            Offset = default(Vector3);
        }

        public HexGridWorldSpace(HexGridSize size, Vector3 scale, Vector3 offset) {
            GridWidth = size.Width;
            GridHeight = size.Height;
            Scale = scale;
            Offset = offset;
        }

        static public implicit operator HexGridWorldSpace(HexGridSize size) {
            return new HexGridWorldSpace(size);
        }
    }
}