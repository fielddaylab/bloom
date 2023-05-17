using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using BeauUtil;
using BeauUtil.Debugger;

namespace Zavala.Sim {

    /// <summary>
    /// Mask representing a tile and all 6 possible connections around the tile.
    /// </summary>
    public struct TileAdjacencyMask : IEquatable<TileAdjacencyMask> {
        public const byte OnlyConnectionsMask = 0xFE;

        public byte Value;

        public TileAdjacencyMask(TileDirection direction) {
            Value = (byte) (1 << (int) direction);
        }

        public TileAdjacencyMask(byte value) {
            Value = value;
        }

        public TileAdjacencyMask(int value) {
            Value = (byte) (value & 0xFF);
        }

        /// <summary>
        /// Returns if this mask is empty.
        /// </summary>
        public bool IsEmpty {
            get { return Value == 0; }
        }

        /// <summary>
        /// Gets/sets the flag stored for the given direction.
        /// </summary>
        public bool this[TileDirection direction] {
            get {
                return (Value & (1 << (int) direction)) != 0;
            }
            set {
                if (value) {
                    Value |= (byte) (1 << (int) direction);
                } else {
                    Value &= (byte) ~(1 << (int) direction);
                }
            }
        }

        /// <summary>
        /// Returns how many tiles are represented by this mask.
        /// </summary>
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Bits.Count((uint) Value); }
        }

        /// <summary>
        /// Returns how many connecting tiles are represented by this mask.
        /// </summary>
        public int OutgoingCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Bits.Count((uint) (Value & OnlyConnectionsMask)); }
        }

        /// <summary>
        /// Returns if the given direction is present in the mask.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public bool Has(TileDirection direction) {
            return (Value & (1 << (int) direction)) != 0;
        }

        /// <summary>
        /// Clears the mask.
        /// </summary>
        public void Clear() {
            Value = 0;
        }

        #region Enumerator

        public Enumerator GetEnumerator() {
            return new Enumerator(Value);
        }

        public struct Enumerator : IEnumerator<TileDirection> {

            private byte m_Mask;
            private int m_Offset;
            private TileDirection m_Last;

            internal Enumerator(byte mask) {
                m_Mask = mask;
                m_Offset = 0;
                m_Last = (TileDirection) 255;
            }

            public TileDirection Current { get { return m_Last; } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() {
            }

            public bool MoveNext() {
                while(m_Mask != 0) {
                    if ((m_Mask & 1) == 1) {
                        m_Last = (TileDirection) m_Offset;
                        m_Offset++;
                        m_Mask >>= 1;
                        return true;
                    }

                    m_Offset++;
                    m_Mask >>= 1;
                }
                
                return false;
            }

            public void Reset() {
            }
        }

        #endregion // Enumerator

        #region Overrides

        public override bool Equals(object obj) {
            if (obj is TileAdjacencyMask) {
                return Equals((TileAdjacencyMask) obj);
            }
            
            return false;
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public bool Equals(TileAdjacencyMask mask) {
            return Value == mask.Value;
        }

        public override string ToString() {
            if (Value == 0) {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            for(TileDirection i = 0; i < TileDirection.COUNT; i++) {
                if (this[i]) {
                    sb.Append(i).Append(" | ");
                }
            }
            if (sb.Length > 0) {
                sb.Length -= 3;
            }
            return sb.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static public TileAdjacencyMask operator|(TileAdjacencyMask a, TileAdjacencyMask b) {
            return new TileAdjacencyMask(a.Value | b.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static public TileAdjacencyMask operator&(TileAdjacencyMask a, TileAdjacencyMask b) {
            return new TileAdjacencyMask(a.Value & b.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static public TileAdjacencyMask operator|(TileAdjacencyMask a, TileDirection b) {
            return new TileAdjacencyMask(a.Value | (1 << (int) b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static public TileAdjacencyMask operator&(TileAdjacencyMask a, TileDirection b) {
            return new TileAdjacencyMask(a.Value & (1 << (int) b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public TileAdjacencyMask operator~(TileAdjacencyMask a) {
            return new TileAdjacencyMask(~a.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static public bool operator==(TileAdjacencyMask a, TileAdjacencyMask b) {
            return a.Value == b.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        static public bool operator!=(TileAdjacencyMask a, TileAdjacencyMask b) {
            return a.Value == b.Value;
        }

        #endregion // Overrides
    }

    /// <summary>
    /// Data mapped to a tile and its adjacent tiles, referenced by direction.
    /// </summary>
    public struct TileAdjacencyDataSet<T> where T : unmanaged {
        public TileAdjacencyMask m_Mask;
        private T m_Self;
        private T m_SW;
        private T m_S;
        private T m_SE;
        private T m_NE;
        private T m_N;
        private T m_NW;

        /// <summary>
        /// Returns if this set is empty.
        /// </summary>
        public bool IsEmpty {
            get { return m_Mask.Value == 0; }
        }

        /// <summary>
        /// Gets/sets the data stored for the given direction.
        /// </summary>
        public T this[TileDirection direction] {
            get {
                Assert.True(m_Mask.Has(direction), "Value for index '{0}' is not set", direction);
                unsafe {
                    fixed(T* data = &m_Self) {
                        return data[(int) direction];
                    }
                }
            }
            set {
                Set(direction, value);
            }
        }

        /// <summary>
        /// Returns how many tiles are represented by this set.
        /// </summary>
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return m_Mask.Count; }
        }

        /// <summary>
        /// Returns how many connecting tiles are represented by this set.
        /// </summary>
        public int OutgoingCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return m_Mask.OutgoingCount; }
        }

        /// <summary>
        /// Returns if the given direction is present in the set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public bool Has(TileDirection direction) {
            return m_Mask.Has(direction);
        }

        /// <summary>
        /// Removes the data at the given direction.
        /// </summary>
        public bool Remove(TileDirection direction) {
            if (m_Mask.Has(direction)) {
                m_Mask[direction] = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the data at the given direction.
        /// </summary>
        public void Set(TileDirection direction, T value) {
            m_Mask.Value |= (byte) (1 << (int) direction);
            unsafe {
                fixed(T* data = &m_Self) {
                    data[(int) direction] = value;
                }
            }
        }

        /// <summary>
        /// Attempts to get the data from the given direction.
        /// </summary>
        public bool TryGet(TileDirection direction, out T value) {
            if (m_Mask.Has(direction)) {
                unsafe {
                    fixed(T* data = &m_Self) {
                        value = data[(int) direction];
                        return true;
                    }
                }
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Clears all data from the set.
        /// </summary>
        public void Clear() {
            m_Mask.Clear();
        }

        #region Enumerator

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        /// <summary>
        /// Enumerator for iterating over processed data.
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TileDirection, T>> {
            private TileAdjacencyMask.Enumerator m_MaskEnumerator;
            private TileAdjacencyDataSet<T> m_SourceData;

            internal Enumerator(TileAdjacencyDataSet<T> source) {
                m_SourceData = source;
                m_MaskEnumerator = source.m_Mask.GetEnumerator();
            }

            public KeyValuePair<TileDirection, T> Current { get { return new KeyValuePair<TileDirection, T>(m_MaskEnumerator.Current, m_SourceData[m_MaskEnumerator.Current]); }}

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() {
                m_MaskEnumerator.Dispose();
                m_SourceData = default;
            }

            public bool MoveNext() {
                return m_MaskEnumerator.MoveNext();
            }

            public void Reset() {
            }
        }

        #endregion // Enumerator
    }

    static public class Tile {
        public delegate bool TileDataPredicate<TTile>(in TTile centerTile, in TTile adjacentTile) where TTile : struct;
        public delegate bool TileDataMapDelegate<TTile, TMap>(in TTile centerTile, in TTile adjacentTile, out TMap adjMap);

        public const ushort InvalidIndex16 = ushort.MaxValue;
        public const uint InvalidIndex32 = uint.MaxValue;

        #region Adjacency Mask

        /// <summary>
        /// Gathers a mask of tile directions whose data passes a given predicate.
        /// </summary>
        /// <param name="index">Grid index</param>
        /// <param name="tileBuffer">Buffer of tile data</param>
        /// <param name="tileBufferLength">Length of buffer</param>
        /// <param name="gridSize">Grid size parameters</param>
        /// <param name="predicate">Mask predicate</param>
        static public unsafe TileAdjacencyMask GatherAdjacencyMask<TTile>(int index, TTile* tileBuffer, int tileBufferLength, in HexGridSize gridSize, TileDataPredicate<TTile> predicate)
            where TTile : unmanaged {
            TTile center = tileBuffer[index];
            HexVector pos = gridSize.FastIndexToPos(index);
            TileAdjacencyMask mask = default(TileAdjacencyMask);
            for(TileDirection dir = (TileDirection) 1; dir < TileDirection.COUNT; dir++) {
                HexVector adjPos = gridSize.OffsetPosFrom(pos, dir);
                if (!gridSize.IsValidPos(adjPos)) {
                    continue;
                }
                int adjIdx = gridSize.FastPosToIndex(adjPos);
                if (predicate(center, tileBuffer[adjIdx])) {
                    mask |= dir;
                }
            }

            return mask;
        }

        /// <summary>
        /// Gathers a mask of tile directions whose data passes a given predicate.
        /// </summary>
        /// <param name="point">Grid point</param>
        /// <param name="tileBuffer">Buffer of tile data</param>
        /// <param name="tileBufferLength">Length of buffer</param>
        /// <param name="gridSize">Grid size parameters</param>
        /// <param name="predicate">Mask predicate</param>
        static public unsafe TileAdjacencyMask GatherAdjacencyMask<TTile>(HexVector point, TTile* tileBuffer, int tileBufferLength, in HexGridSize gridSize, TileDataPredicate<TTile> predicate)
            where TTile : unmanaged {
            return GatherAdjacencyMask<TTile>(gridSize.PosToIndex(point), tileBuffer, tileBufferLength, gridSize, predicate);
        }

        #endregion // Adjacency Mask

        #region Adjacency Set

        static public unsafe TileAdjacencyDataSet<TMap> GatherAdjacencySet<TTile, TMap>(int index, TTile* tileBuffer, int tileBufferLength, in HexGridSize gridSize, TileDataMapDelegate<TTile, TMap> setMap)
            where TTile : unmanaged
            where TMap : unmanaged {
                TTile center = tileBuffer[index];
                HexVector pos = gridSize.FastIndexToPos(index);
                TileAdjacencyDataSet<TMap> mapSet = default(TileAdjacencyDataSet<TMap>);
                for(TileDirection dir = (TileDirection) 1; dir < TileDirection.COUNT; dir++) {
                    HexVector adjPos = gridSize.OffsetPosFrom(pos, dir);
                    if (!gridSize.IsValidPos(adjPos)) {
                        continue;
                    }

                    int adjIndex = gridSize.FastPosToIndex(adjPos);
                    if (setMap(center, tileBuffer[adjIndex], out TMap mapped)) {
                        mapSet.Set(dir, mapped);
                    }
                }

                return mapSet;
        }

        static public unsafe TileAdjacencyDataSet<TMap> GatherAdjacencySet<TTile, TMap>(HexVector point, TTile* tileBuffer, int tileBufferLength, in HexGridSize gridSize, TileDataMapDelegate<TTile, TMap> setMap)
            where TTile : unmanaged
            where TMap : unmanaged {
                return GatherAdjacencySet<TTile, TMap>(gridSize.PosToIndex(point), tileBuffer, tileBufferLength, gridSize, setMap);
        }

        #endregion // Adjacency Set
    }
}