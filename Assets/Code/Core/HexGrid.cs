using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil.Debugger;
using UnityEngine;

namespace Zavala {

    /// <summary>
    /// Hex grid size and helper struct for space/index conversions.
    /// Every second column is offset by 1/2 a space in a positive direction (up).
    /// Based on https://www.redblobgames.com/grids/hexagons/ using odd-q rules
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
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
                    idxBuffer[i] = (short) PointOffsetToIndexOffset(s_GridOffsets[i], Width);
                }
            }
        }

        #region Validation

        /// <summary>
        /// Returns if the given point is within the grid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidPos(HexVector vec) {
            int x = vec.X, y = vec.Y;
            return x >= 0 && x < Width && y >= 0 && y < Height;
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
        /// Returns if the given position relative to the current vector is valid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidPosOffset(HexVector vec, TileDirection direction) {
            HexVector target = HexVector.Offset(vec, direction);
            return IsValidPos(target);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PosToIndex(HexVector vec) {
            return PosToIndex(vec.X, vec.Y);
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

            return HexVector.FromGrid((int) (index % Width), (int) (index / Width));
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
            return HexVector.FromGrid((int) (index % Width), (int) (index / Width));
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

        private readonly struct IndexOffsetPair {
            public readonly int X;
            public readonly int Y;

            public IndexOffsetPair(int x, int y) {
                X = x;
                Y = y;
            }
        }

        static private readonly IndexOffsetPair[] s_GridOffsets = new IndexOffsetPair[] {
            new IndexOffsetPair(0, 0),
            new IndexOffsetPair(0, 0),

            new IndexOffsetPair(-1, -1),
            new IndexOffsetPair(-1, 0),

            new IndexOffsetPair(0, -1),
            new IndexOffsetPair(0, -1),

            new IndexOffsetPair(1, -1),
            new IndexOffsetPair(1, 0),

            new IndexOffsetPair(1, 0),
            new IndexOffsetPair(1, 1),

            new IndexOffsetPair(0, 1),
            new IndexOffsetPair(0, 1),

            new IndexOffsetPair(-1, 0),
            new IndexOffsetPair(-1, 1),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private int PointOffsetToIndexOffset(IndexOffsetPair offset, uint width) {
            return offset.X + (int) width * offset.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private int DirToTableIdx(TileDirection direction, int index, uint width) {
            return (2 * (int) direction) + ((index % (int) width) & 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private int DirToTableIdx(TileDirection direction, int x) {
            return (2 * (int) direction) + (x & 1);
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

            public HexVector Current { get { return HexVector.FromGrid(m_Current % m_Width, m_Current / m_Width); } }

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
    /// Hex grid coordinate in cubic space (int)
    /// </summary>
    [DebuggerDisplay("[{Q},{R},{S}]")]
    public readonly struct HexVector : IEquatable<HexVector> {
        public readonly int Q;
        public readonly int R;
        public readonly int S;

        public HexVector(int q, int r) {
            Q = q;
            R = r;
            S = -q - r;
        }

        public HexVector(int q, int r, int s) {
            Q = q;
            R = r;
            S = s;
            Assert.True((Q + R + S) == 0, "invalid s component");
        }

        #region Grid

        public int X {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Q; }
        }

        public int Y {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return R + (Q - (Q & 1)) / 2; }
        }

        /// <summary>
        /// Creates a HexVector from the given grid coordinates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public HexVector FromGrid(int x, int y) {
            return new HexVector(x, y - (x - (x & 1)) / 2);
        }

        #endregion // Grid

        #region Overrides

        public bool Equals(HexVector other) {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj) {
            if (obj is HexVector) {
                return Equals((HexVector) obj);
            }
            return false;
        }

        public override int GetHashCode() {
            int hash = 1039;
            hash = 31 * hash ^ Q.GetHashCode();
            hash = 31 * hash ^ R.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return string.Format("[{0},{1},{2}]", Q, R, S);
        }

        #endregion // Overrides

        #region Operators

        static public HexVector operator +(in HexVector a, in HexVector b) {
            return new HexVector(a.Q + b.Q, a.R + b.R, a.S + b.S);
        }

        static public HexVector operator -(in HexVector a, in HexVector b) {
            return new HexVector(a.Q - b.Q, a.R - b.R, a.S - b.S);
        }

        static public HexVector operator *(in HexVector a, int scale) {
            return new HexVector(a.Q * scale, a.R * scale, a.S * scale);
        }

        static public bool operator==(in HexVector a, in HexVector b) {
            return a.Q == b.Q && a.R == b.R; // s is derived from q and r, so we don't need to check
        }

        static public bool operator !=(in HexVector a, in HexVector b) {
            return a.Q != b.Q || a.R != b.R;
        }

        #endregion // Operators

        #region World Conversion

        static public Vector3 ToWorld(in HexVector point, float height, in HexGridWorldSpace worldSpace) {
            return new Vector3(worldSpace.Offset.x + (point.X * worldSpace.Scale.x),
                worldSpace.Offset.y + height * worldSpace.Scale.y,
                worldSpace.Offset.z + (point.Y + (point.X & 1) * 0.5f) * worldSpace.Scale.z
            );
        }

        static public Vector3 ToWorld(int index, float height, in HexGridWorldSpace worldSpace) {
            return ToWorld(FromGrid(index % (int) worldSpace.GridWidth, index / (int) worldSpace.GridWidth), height, worldSpace);
        }

        static public HexVector FromWorld(Vector3 position, in HexGridWorldSpace worldSpace) {
            int x = (int) Math.Round(((position.x - worldSpace.Offset.x) / worldSpace.Scale.x));
            int y = (int) Math.Round(((position.z - worldSpace.Offset.z) / worldSpace.Scale.z) - (x & 1) * 0.5f);
            return FromGrid(x, y);
        }

        #endregion // World Conversion

        #region Math

        /// <summary>
        /// Returns the length of this vector.
        /// </summary>
        public int Length() {
            return (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;
        }

        /// <summary>
        /// Returns the euclidian length of this vector.
        /// </summary>
        public float EuclidianLength() {
            return (float) Math.Sqrt((Q * Q) + (R * R) + (Q * R));
        }

        /// <summary>
        /// Returns the squared euclidian length of this vector.
        /// </summary>
        public int EuclidianLengthSq() {
            return (Q * Q) + (R * R) + (Q * R);
        }

        /// <summary>
        /// Returns the distance between two vectors.                 
        /// </summary
        static public int Distance(in HexVector a, in HexVector b) {
            return (Math.Abs(b.Q - a.Q) + Math.Abs(b.R - a.R) + Math.Abs(b.S - a.S)) / 2;
        }

        /// <summary>
        /// Returns the euclidian distance between two vectors.
        /// </summary
        static public float EuclidianDistance(in HexVector a, in HexVector b) {
            int dq = b.Q - a.Q;
            int dr = b.R - a.R;
            return (float) Math.Sqrt(dq * dq + dr * dr + dq * dr);
        }

        /// <summary>
        /// Returns the squared euclidian distance between two vectors.
        /// </summary
        static public int EuclidianDistanceSq(in HexVector a, in HexVector b) {
            int dq = b.Q - a.Q;
            int dr = b.R - a.R;
            return (dq * dq) + (dr * dr) + (dq * dr);
        }

        #endregion // Math

        #region Offsets

        /// <summary>
        /// HexCoord offset table per direction.
        /// </summary>
        static private readonly HexVector[] s_VectorOffsets = new HexVector[]
        {
            new HexVector(0, 0),
            new HexVector(-1, 0),
            new HexVector(0, -1),
            new HexVector(1, -1),
            new HexVector(1, 0),
            new HexVector(0, 1),
            new HexVector(-1, 1),
        };

        /// <summary>
        /// Returns the HexVector offset for the given direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public HexVector Offset(TileDirection direction) {
            return s_VectorOffsets[(int) direction];
        }

        /// <summary>
        /// Returns a HexVector offset on the given direction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public HexVector Offset(in HexVector vec, TileDirection direction) {
            HexVector off = s_VectorOffsets[(int) direction];
            return new HexVector(vec.Q + off.Q, vec.R + off.R, vec.S + off.S);
        }

        #endregion // Offsets
    }

    /// <summary>
    /// Hex grid coordinate in cubic space (float)
    /// </summary>
    [DebuggerDisplay("[{Q},{R},{S}]")]
    public readonly struct HexVectorF : IEquatable<HexVectorF> {
        public readonly float Q;
        public readonly float R;
        public readonly float S;

        public HexVectorF(float q, float r) {
            Q = q;
            R = r;
            S = -q - r;
        }

        public HexVectorF(float q, float r, float s) {
            Q = q;
            R = r;
            S = s;
            Assert.True((Q + R + S) == 0, "invalid s component");
        }

        #region Offset Space

        public float X {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Q; }
        }

        public float Y {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return R + (Q - ((int) Q & 1)) / 2; }
        }

        #endregion // Offset Space

        #region Overrides

        public bool Equals(HexVectorF other) {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj) {
            if (obj is HexVectorF) {
                return Equals((HexVectorF) obj);
            }
            return false;
        }

        public override int GetHashCode() {
            int hash = 1039;
            hash = 31 * hash ^ Q.GetHashCode();
            hash = 31 * hash ^ R.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return string.Format("[{0},{1},{2}]", Q, R, S);
        }

        #endregion // Overrides

        #region Operators

        static public HexVectorF operator +(in HexVectorF a, in HexVectorF b) {
            return new HexVectorF(a.Q + b.Q, a.R + b.R, a.S + b.S);
        }

        static public HexVectorF operator +(in HexVectorF a, in HexVector b) {
            return new HexVectorF(a.Q + b.Q, a.R + b.R, a.S + b.S);
        }

        static public HexVectorF operator +(in HexVector a, in HexVectorF b) {
            return new HexVectorF(a.Q + b.Q, a.R + b.R, a.S + b.S);
        }

        static public HexVectorF operator -(in HexVectorF a, in HexVectorF b) {
            return new HexVectorF(a.Q - b.Q, a.R - b.R, a.S - b.S);
        }

        static public HexVectorF operator -(in HexVectorF a, in HexVector b) {
            return new HexVectorF(a.Q - b.Q, a.R - b.R, a.S - b.S);
        }

        static public HexVectorF operator -(in HexVector a, in HexVectorF b) {
            return new HexVectorF(a.Q - b.Q, a.R - b.R, a.S - b.S);
        }

        static public HexVectorF operator *(in HexVectorF a, float scale) {
            return new HexVectorF(a.Q * scale, a.R * scale, a.S * scale);
        }

        static public HexVectorF operator *(float scale, in HexVectorF a) {
            return new HexVectorF(a.Q * scale, a.R * scale, a.S * scale);
        }

        static public bool operator ==(in HexVectorF a, in HexVectorF b) {
            return a.Q == b.Q && a.R == b.R; // s is derived from q and r, so we don't need to check
        }

        static public bool operator !=(in HexVectorF a, in HexVectorF b) {
            return a.Q != b.Q || a.R != b.R;
        }

        static public bool operator ==(in HexVectorF a, in HexVector b) {
            return a.Q == b.Q && a.R == b.R; // s is derived from q and r, so we don't need to check
        }

        static public bool operator !=(in HexVectorF a, in HexVector b) {
            return a.Q != b.Q || a.R != b.R;
        }

        static public bool operator ==(in HexVector a, in HexVectorF b) {
            return a.Q == b.Q && a.R == b.R; // s is derived from q and r, so we don't need to check
        }

        static public bool operator !=(in HexVector a, in HexVectorF b) {
            return a.Q != b.Q || a.R != b.R;
        }

        static public implicit operator HexVectorF(in HexVector source) {
            return new HexVectorF(source.Q, source.R, source.S);
        }

        static public explicit operator HexVector(in HexVectorF source) {
            return Round(source);
        }

        #endregion // Operators

        #region Math

        /// <summary>
        /// Returns the length of this vector.
        /// </summary>
        public float Length() {
            return (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;
        }

        /// <summary>
        /// Returns the euclidian length of this vector.
        /// </summary>
        public float EuclidianLength() {
            return (float) Math.Sqrt((Q * Q) + (R * R) + (Q * R));
        }

        /// <summary>
        /// Returns the squared euclidian length of this vector.
        /// </summary>
        public float EuclidianLengthSq() {
            return (Q * Q) + (R * R) + (Q * R);
        }

        /// <summary>
        /// Returns the distance between two vectors.                 
        /// </summary
        static public float Distance(in HexVectorF a, in HexVectorF b) {
            return (Math.Abs(b.Q - a.Q) + Math.Abs(b.R - a.R) + Math.Abs(b.S - a.S)) / 2;
        }

        /// <summary>
        /// Returns the euclidian distance between two vectors.
        /// </summary
        static public float EuclidianDistance(in HexVectorF a, in HexVectorF b) {
            float dq = b.Q - a.Q;
            float dr = b.R - a.R;
            return (float) Math.Sqrt(dq * dq + dr * dr + dq * dr);
        }

        /// <summary>
        /// Returns the squared euclidian distance between two vectors.
        /// </summary
        static public float EuclidianDistanceSq(in HexVectorF a, in HexVectorF b) {
            float dq = b.Q - a.Q;
            float dr = b.R - a.R;
            return (dq * dq) + (dr * dr) + (dq * dr);
        }

        /// <summary>
        /// Rounds the given HexVector to integer components.
        /// </summary>
        static public HexVector Round(in HexVectorF vec) {
            int q = (int) Math.Round(vec.Q);
            int r = (int) Math.Round(vec.R);
            int s = (int) Math.Round(vec.S);

            float dq = Math.Abs(q - vec.Q);
            float dr = Math.Abs(r - vec.R);
            float ds = Math.Abs(s - vec.S);

            if (dq > dr && dq > ds) {
                q = -r - s;
            } else if (dr > ds) {
                r = -q - s;
            } else {
                s = -q - r;
            }

            return new HexVector(q, r, s);
        }

        #endregion // Math

        #region Offset

        /// <summary>
        /// Returns a HexVector offset on the given direction.
        /// </summary>
        static public HexVectorF Offset(in HexVectorF vec, TileDirection direction) {
            HexVector off = HexVector.Offset(direction);
            return new HexVectorF(vec.Q + off.Q, vec.R + off.R, vec.S + off.S);
        }

        #endregion // Offset
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
        public bool ContainsPos(HexVector vec) {
            int x = vec.X, y = vec.Y;
            return x >= X && y >= Y
                && x < X + Width && y < Y + Height;
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
            return HexVector.FromGrid(X + (index % Width), Y + (index / Width));
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
        public readonly Vector3 Center;
        public readonly Vector3 Offset;

        public HexGridWorldSpace(HexGridSize size) {
            GridWidth = size.Width;
            GridHeight = size.Height;
            Scale = new Vector3(1, 1, 1);
            Center = default(Vector3);

            Vector3 offset;
            offset.x = -GridWidth / 2f * Scale.x;
            offset.y = 0;
            offset.z = -GridHeight / 2f;
            Offset = offset;
        }

        public HexGridWorldSpace(HexGridSize size, Vector3 scale, Vector3 offset) {
            GridWidth = size.Width;
            GridHeight = size.Height;
            Scale = scale;
            Center = offset;

            offset.x -= GridWidth / 2f * scale.x;
            offset.z -= GridHeight / 2f * scale.z;
            Offset = offset;
        }

        static public implicit operator HexGridWorldSpace(HexGridSize size) {
            return new HexGridWorldSpace(size);
        }
    }
}