using System;
using System.Runtime.CompilerServices;
using BeauUtil;

namespace FieldDay {
    /// <summary>
    /// Game loop phase buckets.
    /// </summary>
    static internal class PhaseBuckets {
        private const GameLoopPhase MinPhase = GameLoopPhase.PreUpdate;
        private const GameLoopPhase MaxPhase = GameLoopPhase.FrameAdvance;

        /// <summary>
        /// Maximum number of buckets.
        /// </summary>
        public const int MaxBuckets = (int) (MaxPhase - MinPhase) + 1;

        #region Mapping

        /// <summary>
        /// Phase to Index
        /// </summary>
        static public int PhaseToIndex(GameLoopPhase phase) {
            return (int) (phase - MinPhase);
        }

        /// <summary>
        /// Phase mask to mask
        /// </summary>
        static public int PhaseMaskShift(GameLoopPhaseMask phase) {
            return ((int) phase >> (int) MinPhase);
        }

        /// <summary>
        /// Index to Phase.
        /// </summary>
        static public GameLoopPhase IndexToPhase(int index) {
            return (GameLoopPhase) ((int) MinPhase + index);
        }

        /// <summary>
        /// Returns if the given phase has a valid bucket.
        /// </summary>
        static public bool IsTracked(GameLoopPhase phase) {
            return phase >= MinPhase && phase <= MaxPhase;
        }

        #endregion // Mapping

        #region Registration/Deregistration

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool SwitchBuckets<TData>(ref PhaseBuckets<TData> buckets, TData data, ref GameLoopPhaseMask current, GameLoopPhaseMask target) {
            if (current == target) {
                return false;
            }

            GameLoopPhaseMask overlap = current & target;
            GameLoopPhaseMask toRemove = current & ~overlap;
            GameLoopPhaseMask toAdd = target & ~overlap;

            buckets.MarkBucketsDirty(toRemove | toAdd);

            if ((toRemove & GameLoopPhaseMask.PreUpdate) != 0) {
                buckets[GameLoopPhase.PreUpdate].FastRemove(data);
            } else if ((toAdd & GameLoopPhaseMask.PreUpdate) != 0) {
                buckets[GameLoopPhase.PreUpdate].PushBack(data);
            }

            if ((toRemove & GameLoopPhaseMask.FixedUpdate) != 0) {
                buckets[GameLoopPhase.FixedUpdate].FastRemove(data);
            } else if ((toAdd & GameLoopPhaseMask.FixedUpdate) != 0) {
                buckets[GameLoopPhase.FixedUpdate].PushBack(data);
            }

            if ((toRemove & GameLoopPhaseMask.Update) != 0) {
                buckets[GameLoopPhase.Update].FastRemove(data);
            } else if ((toAdd & GameLoopPhaseMask.Update) != 0) {
                buckets[GameLoopPhase.Update].PushBack(data);
            }

            if ((toRemove & GameLoopPhaseMask.UnscaledUpdate) != 0) {
                buckets[GameLoopPhase.UnscaledUpdate].FastRemove(data);
            } else if ((toAdd & GameLoopPhaseMask.UnscaledUpdate) != 0) {
                buckets[GameLoopPhase.UnscaledUpdate].PushBack(data);
            }

            if ((toRemove & GameLoopPhaseMask.LateUpdate) != 0) {
                buckets[GameLoopPhase.LateUpdate].FastRemove(data);
            } else if ((toAdd & GameLoopPhaseMask.LateUpdate) != 0) {
                buckets[GameLoopPhase.LateUpdate].PushBack(data);
            }

            if ((toRemove & GameLoopPhaseMask.UnscaledLateUpdate) != 0) {
                buckets[GameLoopPhase.UnscaledLateUpdate].FastRemove(data);
            } else if ((toAdd & GameLoopPhaseMask.UnscaledLateUpdate) != 0) {
                buckets[GameLoopPhase.UnscaledLateUpdate].PushBack(data);
            }

            current = target;
            return true;
        }

        #endregion // Registration/Deregistration
    }

    /// <summary>
    /// Map of data for each game loop phase.
    /// </summary>
    internal struct PhaseMap<TData> {
        private TData[] m_Buckets;

        /// <summary>
        /// Initializes backing data.
        /// </summary>
        public void Create() {
            m_Buckets = new TData[PhaseBuckets.MaxBuckets];
        }

        /// <summary>
        /// Clears all data.
        /// </summary>
        public void Clear() {
            Array.Clear(m_Buckets, 0, PhaseBuckets.MaxBuckets);
        }

        /// <summary>
        /// Number of slots.
        /// </summary>
        public int Length {
            get { return PhaseBuckets.MaxBuckets; }
        }

        /// <summary>
        /// Returns the data for the given index.
        /// </summary>
        public ref TData this[int index] {
            get { return ref m_Buckets[index]; }
        }

        /// <summary>
        /// Returns the data for the given index.
        /// </summary>
        public ref TData this[GameLoopPhase phase] {
            get { return ref m_Buckets[PhaseBuckets.PhaseToIndex(phase)]; }
        }
    }

    /// <summary>
    /// Buckets associated with each game loop phase.
    /// </summary>
    internal struct PhaseBuckets<TData> {
        private RingBuffer<TData>[] m_Buckets;
        private int m_DefaultCapacity;
        private BitSet32 m_BucketDirty;
        private BitSet32 m_BucketInit;

        public PhaseBuckets(int defaultCapacity) : this() {
            Create(defaultCapacity);
        }

        /// <summary>
        /// Creates buckets.
        /// </summary>
        public void Create(int defaultCapacity) {
            m_Buckets = new RingBuffer<TData>[PhaseBuckets.MaxBuckets];
            m_DefaultCapacity = defaultCapacity;
            m_BucketDirty.Clear();
            m_BucketInit.Clear();
        }

        /// <summary>
        /// Clears all data from 
        /// </summary>
        public void Clear() {
            for(int i = 0; i < PhaseBuckets.MaxBuckets; i++) {
                m_Buckets[i]?.Clear();
                m_Buckets[i] = null;
            }
            m_BucketDirty.Clear();
            m_BucketInit.Clear();
        }

        /// <summary>
        /// Number of buckets.
        /// </summary>
        public int Length {
            get { return PhaseBuckets.MaxBuckets; }
        }

        #region Get

        /// <summary>
        /// Returns the bucket for the given index.
        /// </summary>
        public RingBuffer<TData> this[int index] {
            get { return GetBucket(index); }
        }

        /// <summary>
        /// Returns the bucket for the given index.
        /// </summary>
        public RingBuffer<TData> this[GameLoopPhase phase] {
            get { return GetBucket(PhaseBuckets.PhaseToIndex(phase)); }
        }

        /// <summary>
        /// Returns if the bucket for the given phase has been initialized.
        /// </summary>
        public bool Has(GameLoopPhase phase) {
            return m_BucketInit.IsSet(PhaseBuckets.PhaseToIndex(phase));
        }

        private RingBuffer<TData> GetBucket(int index) {
            RingBuffer<TData> bucket;
            if (!m_BucketInit.IsSet(index)) {
                bucket = m_Buckets[index] = new RingBuffer<TData>(m_DefaultCapacity, RingBufferMode.Expand);
                m_BucketInit.Set(index);
            } else {
                bucket = m_Buckets[index];
            }
            return bucket;
        }

        #endregion // Get

        #region Dirty

        /// <summary>
        /// Marks the given phase as dirty.
        /// </summary>
        public void MarkBucketDirty(GameLoopPhase phase) {
            m_BucketDirty.Set(PhaseBuckets.PhaseToIndex(phase));
        }

        /// <summary>
        /// Marks the given phase as dirty.
        /// </summary>
        public void MarkBucketsDirty(GameLoopPhaseMask phase) {
            m_BucketDirty |= new BitSet32((uint) PhaseBuckets.PhaseMaskShift(phase));
        }

        /// <summary>
        /// Clears the given phase as dirty.
        /// </summary>
        public void ClearBucketDirty(GameLoopPhase phase) {
            m_BucketDirty.Unset(PhaseBuckets.PhaseToIndex(phase));
        }

        /// <summary>
        /// Pops the given bucket.
        /// </summary>
        public bool PopBucketDirty(GameLoopPhase phase) {
            int index = PhaseBuckets.PhaseToIndex(phase);
            if (m_BucketDirty.IsSet(index)) {
                m_BucketDirty.Unset(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Marks the given phase as dirty.
        /// </summary>
        public bool IsBucketDirty(GameLoopPhase phase) {
            return m_BucketDirty.IsSet(PhaseBuckets.PhaseToIndex(phase));
        }

        #endregion // Dirty
    }
}