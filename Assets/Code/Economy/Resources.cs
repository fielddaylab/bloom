using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using Zavala.Data;

namespace Zavala.Economy {
    /// <summary>
    /// Resource identifier.
    /// </summary>
    [LabeledEnum]
    public enum ResourceId : ushort {
        Manure,
        MFertilizer,
        DFertilizer,
        Grain,
        Milk,

        [Hidden]
        COUNT
    }

    /// <summary>
    /// Resource mask.
    /// </summary>
    [Flags]
    public enum ResourceMask : uint {
        Manure = 1 << ResourceId.Manure,
        MFertilizer = 1 << ResourceId.MFertilizer,
        DFertilizer = 1 << ResourceId.DFertilizer,
        Grain = 1 << ResourceId.Grain,
        Milk = 1 << ResourceId.Milk,

        [Hidden] Phosphorus = Manure | MFertilizer | DFertilizer
    }

    [Serializable]
    public struct MarketPriceBlock
    {
        public int Phosphorus;
        public int Grain;
        public int Milk;

        /// <summary>
        /// Sets the value of all resources.
        /// </summary>
        public void SetAll(int value)
        {
            Phosphorus = Grain = Milk = value;
        }

        public int this[int marketIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Assert.True(marketIndex >= 0 && marketIndex < MarketUtility.NumMarkets, "Market index out of range");
                unsafe
                {
                    fixed (int* start = &Phosphorus)
                    {
                        return *(start + (int)marketIndex);
                    }
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Assert.True(marketIndex >= 0 && marketIndex < MarketUtility.NumMarkets, "Market index out of range");
                unsafe
                {
                    fixed (int* start = &Phosphorus)
                    {
                        *(start + (int)marketIndex) = value;
                    }
                }
            }
        }

        static public MarketPriceBlock operator &(in MarketPriceBlock a, ResourceMask mask)
        {
            unsafe
            {
                MarketPriceBlock block = a;
                int* ptr = &block.Phosphorus;
                int idx = 0;
                uint idxMask = 1;
                bool anyPhosph = false;
                bool match = false;
                while (idx < ResourceUtility.Count)
                {
                    match = (idxMask & (uint)mask) != 0;
                    if (idx < (int)ResourceId.Grain)
                    {
                        if (match)
                        {
                            anyPhosph = true;
                        }
                        if (idx == (int)ResourceId.DFertilizer)
                        {
                            if (!anyPhosph)
                            {
                                *ptr = 0;
                            }
                            ptr++;
                        }
                    }
                    else
                    {
                        if (!match)
                        {
                            *ptr = 0;
                        }
                        ptr++;
                    }
                    idx++;
                    idxMask <<= 1;
                }
                return block;
            }
        }

        static public MarketPriceBlock operator +(in MarketPriceBlock a, in MarketPriceBlock b)
        {
            return new MarketPriceBlock()
            {
                Phosphorus = a.Phosphorus + b.Phosphorus,
                Grain = a.Grain + b.Grain,
                Milk = a.Milk + b.Milk
            };
        }
    }

        /// <summary>
        /// Block of 32-bit resource values.
        /// </summary>
        [Serializable]
    public struct ResourceBlock {
        public int Manure;
        public int MFertilizer;
        public int DFertilizer;
        public int Grain;
        public int Milk;

        /// <summary>
        /// Gets or sets the resource count for the given resource.
        /// </summary>
        public int this[ResourceId id] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                Assert.True(id >= 0 && id < ResourceId.COUNT, "ResourceId out of range");
                unsafe {
                    fixed (int* start = &Manure) {
                        return *(start + (int) id);
                    }
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                Assert.True(id >= 0 && id < ResourceId.COUNT, "ResourceId out of range");
                unsafe {
                    fixed (int* start = &Manure) {
                        *(start + (int) id) = value;
                    }
                }
            }
        }
        /// <summary>
        /// Sets the value of all resources.
        /// </summary>
        public void SetAll(int value) {
            Manure = MFertilizer = DFertilizer = Grain = Milk = value;
        }

        /// <summary>
        /// Gets a mask of all resources with a non-zero value.
        /// </summary>
        public ResourceMask NonZeroMask {
            get {
                ResourceMask mask = default;
                if (Manure != 0) {
                    mask |= ResourceMask.Manure;
                }
                if (MFertilizer != 0) {
                    mask |= ResourceMask.MFertilizer;
                }
                if (DFertilizer != 0) {
                    mask |= ResourceMask.DFertilizer;
                }
                if (Grain != 0) {
                    mask |= ResourceMask.Grain;
                }
                if (Milk != 0) {
                    mask |= ResourceMask.Milk;
                }
                return mask;
            }
        }

        /// <summary>
        /// Gets a mask of all resources with a positive value.
        /// </summary>
        public ResourceMask PositiveMask {
            get {
                ResourceMask mask = default;
                if (Manure > 0) {
                    mask |= ResourceMask.Manure;
                }
                if (MFertilizer > 0) {
                    mask |= ResourceMask.MFertilizer;
                }
                if (DFertilizer > 0) {
                    mask |= ResourceMask.DFertilizer;
                }
                if (Grain > 0) {
                    mask |= ResourceMask.Grain;
                }
                if (Milk > 0) {
                    mask |= ResourceMask.Milk;
                }
                return mask;
            }
        }

        /// <summary>
        /// Gets a mask of all resources with a negative value.
        /// </summary>
        public ResourceMask NegativeMask {
            get {
                ResourceMask mask = default;
                if (Manure < 0) {
                    mask |= ResourceMask.Manure;
                }
                if (MFertilizer < 0) {
                    mask |= ResourceMask.MFertilizer;
                }
                if (DFertilizer < 0) {
                    mask |= ResourceMask.DFertilizer;
                }
                if (Grain < 0) {
                    mask |= ResourceMask.Grain;
                }
                if (Milk < 0) {
                    mask |= ResourceMask.Milk;
                }
                return mask;
            }
        }

        /// <summary>
        /// Returns if this resource block is empty.
        /// </summary>
        public bool IsZero {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                return Manure == 0 && MFertilizer == 0 && DFertilizer == 0
                    && Grain == 0 && Milk == 0;
            }
        }

        /// <summary>
        /// Returns if this resource block has at least one positive component.
        /// </summary>
        public bool IsPositive {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                return Manure > 0 || MFertilizer > 0 || DFertilizer > 0
                    || Grain > 0 || Milk > 0;
            }
        }

        /// <summary>
        /// Returns the total number of resources in this block.
        /// </summary>
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Manure + MFertilizer + DFertilizer + Grain + Milk; }
        }

        /// <summary>
        /// Returns the total number of phosphorus resources in this block.
        /// </summary>
        public int PhosphorusCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Manure + MFertilizer + DFertilizer; }
        }

        #region Utilities

        /// <summary>
        /// Returns if a resource block can fulfill a given request.
        /// </summary>
        static public bool Fulfills(in ResourceBlock source, in ResourceBlock request) {
            return source.PhosphorusCount >= request.PhosphorusCount
                && source.Grain >= request.Grain && source.Milk >= request.Milk;
        }

        /// <summary>
        /// Returns if a resource block can be partially added to the given source block.
        /// </summary>
        static public bool CanAddPartial(in ResourceBlock source, ref ResourceBlock production, in ResourceBlock capacity) {
            if (source.Manure + production.Manure > capacity.Manure) {
                production.Manure = capacity.Manure - source.Manure;
            }
            if (source.MFertilizer + production.MFertilizer > capacity.MFertilizer) {
                production.MFertilizer = capacity.MFertilizer - source.MFertilizer;
            }
            if (source.DFertilizer + production.DFertilizer > capacity.DFertilizer) {
                production.DFertilizer = capacity.DFertilizer - source.DFertilizer;
            }
            if (source.Grain + production.Grain > capacity.Grain) {
                production.Grain = capacity.Grain - source.Grain;
            }
            if (source.Milk + production.Milk > capacity.Milk) {
                production.Milk = capacity.Milk - source.Milk;
            }
            return production.IsPositive;
        }

        /// <summary>
        /// Returns if a resource block can be fully added to the given source block.
        /// </summary>
        static public bool CanAddFull(in ResourceBlock source, in ResourceBlock production, in ResourceBlock capacity) {
            bool eval =  source.Manure + production.Manure <= capacity.Manure
                && source.MFertilizer + production.MFertilizer <= capacity.MFertilizer
                && source.DFertilizer + production.DFertilizer <= capacity.DFertilizer
                && source.Grain + production.Grain <= capacity.Grain
                && source.Milk + production.Milk <= capacity.Milk;
            // Log.Msg("[ResourceUtility] CanAddFull {0} + {1} < {2}? {3}", source, production, capacity, eval);
            return eval;
        }

        /// <summary>
        /// Returns if a resource block is over the specified capacity.
        /// </summary>
        static public bool IsOverCapacity(in ResourceBlock source, in ResourceBlock capacity) {
            return source.Manure > capacity.Manure && source.MFertilizer > capacity.MFertilizer && source.DFertilizer > capacity.DFertilizer
                && source.Grain > capacity.Grain && source.Milk > capacity.Milk;
        }

        /// <summary>
        /// Attempts to clamp the given block to the given capacity, returns true if anything was clamped.
        /// </summary>
        static public bool TryClamp(ref ResourceBlock source, in ResourceBlock capacity) {
            bool somethingClamped = false;
            if (source.Manure > capacity.Manure) {
                somethingClamped = true;
                source.Manure = capacity.Manure;
            }
            if (source.MFertilizer > capacity.MFertilizer) {
                somethingClamped = true;
                source.MFertilizer = capacity.MFertilizer;
            }
            if (source.DFertilizer> capacity.DFertilizer) {
                somethingClamped = true;
                source.DFertilizer = capacity.DFertilizer;
            }
            if (source.Grain > capacity.Grain) {
                somethingClamped = true;
                source.Grain = capacity.Grain;
            }
            if (source.Milk > capacity.Milk) {
                somethingClamped = true;
                source.Milk = capacity.Milk;
            }
            return somethingClamped;
        }

        /// <summary>
        /// Clamps the given block to the given capacity, outputting the amount over capacity.
        /// </summary>
        static public void Clamp(ref ResourceBlock source, in ResourceBlock capacity, out ResourceBlock overflow) {
            overflow = default;

            if (source.Manure > capacity.Manure) {
                overflow.Manure = source.Manure - capacity.Manure;
                source.Manure = capacity.Manure;
            }
            if (source.MFertilizer > capacity.MFertilizer) {
                overflow.MFertilizer = source.MFertilizer - capacity.MFertilizer;
                source.MFertilizer = capacity.MFertilizer;
            }
            if (source.DFertilizer > capacity.DFertilizer) {
                overflow.DFertilizer = source.DFertilizer - capacity.DFertilizer;
                source.DFertilizer = capacity.DFertilizer;
            }
            if (source.Grain > capacity.Grain) {
                overflow.Grain = source.Grain - capacity.Grain;
                source.Grain = capacity.Grain;
            }
            if (source.Milk > capacity.Milk) {
                overflow.Milk = source.Milk - capacity.Milk;
                source.Milk = capacity.Milk;
            }
        }

        /// <summary>
        /// Consumess the given resource block from the given source.
        /// </summary>
        static public bool Consume(ref ResourceBlock source, ref ResourceBlock request) {
            if (source.PhosphorusCount >= request.PhosphorusCount
                && source.Grain >= request.Grain && source.Milk >= request.Milk) {
                GatherPhosphorusPrioritized(source, request.PhosphorusCount, out ResourceBlock consumed);
                consumed.Grain = request.Grain;
                consumed.Milk = request.Milk;
                source -= consumed;
                request = consumed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumess the given resource block from the given source.
        /// </summary>
        static public ResourceBlock Consume(ref ResourceBlock source, ResourceBlock request) {
            if (source.PhosphorusCount >= request.PhosphorusCount
                && source.Grain >= request.Grain && source.Milk >= request.Milk) {
                GatherPhosphorusPrioritized(source, request.PhosphorusCount, out ResourceBlock consumed);
                consumed.Grain = request.Grain;
                consumed.Milk = request.Milk;
                source -= consumed;
                return consumed;
            }

            return new ResourceBlock();
        }

        /// <summary>
        /// Consumess the given resource block from the given source.
        /// </summary>
        static public ResourceBlock FulfillInfinite(ResourceMask mask, ResourceBlock request) {
            ResourceBlock consumed = default;
            
            int requestedPhosphorus = request.PhosphorusCount;
            if ((mask & ResourceMask.DFertilizer) != 0) {
                consumed.DFertilizer = requestedPhosphorus;
            } else if ((mask & ResourceMask.MFertilizer) != 0) {
                consumed.MFertilizer = requestedPhosphorus;
            } else if ((mask & ResourceMask.Manure) != 0) {
                consumed.Manure = requestedPhosphorus;
            }

            consumed.Milk = request.Milk;
            consumed.Grain = request.Grain;
            return consumed;
        }

        /// <summary>
        /// Gathers phosphorus from the given source, prioritizing digested fertilizer, then mineral fertilizer, and finally manure.
        /// Returns if it was able to gather all requested phosphorus.
        /// </summary>
        static public bool GatherPhosphorusPrioritized(in ResourceBlock source, int phosphorusRequest, out ResourceBlock gathered) {
            gathered = default;

            int digested = Math.Min(phosphorusRequest, source.DFertilizer);
            gathered.DFertilizer = digested;
            phosphorusRequest -= digested;
            
            int mineral = Math.Min(phosphorusRequest, source.MFertilizer);
            gathered.MFertilizer = mineral;
            phosphorusRequest -= mineral;

            int manure = Math.Min(phosphorusRequest, source.Manure);
            gathered.Manure = manure;
            phosphorusRequest -= manure;

            return phosphorusRequest == 0;
        }

        #endregion // Utilities

        #region Operators

        static public ResourceBlock operator+(in ResourceBlock a, in ResourceBlock b) {
            return new ResourceBlock() {
                Manure = a.Manure + b.Manure,
                MFertilizer = a.MFertilizer + b.MFertilizer,
                DFertilizer = a.DFertilizer + b.DFertilizer,
                Grain = a.Grain + b.Grain,
                Milk = a.Milk + b.Milk
            };
        }

        static public ResourceBlock operator -(in ResourceBlock a, in ResourceBlock b) {
            return new ResourceBlock() {
                Manure = a.Manure - b.Manure,
                MFertilizer = a.MFertilizer - b.MFertilizer,
                DFertilizer = a.DFertilizer - b.DFertilizer,
                Grain = a.Grain - b.Grain,
                Milk = a.Milk - b.Milk
            };
        }

        static public ResourceBlock operator *(in ResourceBlock a, float multiplier) {
            return new ResourceBlock() {
                Manure = (int) (a.Manure * multiplier),
                MFertilizer = (int) (a.MFertilizer * multiplier),
                DFertilizer = (int) (a.DFertilizer * multiplier),
                Grain = (int) (a.Grain * multiplier),
                Milk = (int) (a.Milk * multiplier)
            };
        }

        static public ResourceBlock operator *(in ResourceBlock a, in ResourceBlock b) {
            return new ResourceBlock() {
                Manure = (int) (a.Manure * b.Manure),
                MFertilizer = (int) (a.MFertilizer * b.MFertilizer),
                DFertilizer = (int) (a.DFertilizer * b.DFertilizer),
                Grain = (int) (a.Grain * b.Grain),
                Milk = (int) (a.Milk * b.Grain)
            };
        }

        static public ResourceBlock operator &(in ResourceBlock a, ResourceMask mask) {
            unsafe {
                ResourceBlock block = a;
                int* ptr = &block.Manure;
                int idx = 0;
                uint idxMask = 1;
                while(idx < ResourceUtility.Count) {
                    if ((idxMask & (uint) mask) == 0) {
                        *ptr = 0;
                    }
                    ptr++;
                    idx++;
                    idxMask <<= 1;
                }
                return block;
            }
        }

        public override string ToString() {
            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                psb.Builder.Append('[');
                if (Manure != 0) {
                    psb.Builder.Append("Manure=").AppendNoAlloc(Manure).Append(", ");
                }
                if (MFertilizer != 0) {
                    psb.Builder.Append("MFertilizer=").AppendNoAlloc(MFertilizer).Append(", ");
                }
                if (DFertilizer != 0) {
                    psb.Builder.Append("DFertilizer=").AppendNoAlloc(DFertilizer).Append(", ");
                }
                if (Grain != 0) {
                    psb.Builder.Append("Grain=").AppendNoAlloc(Grain).Append(", ");
                }
                if (Milk != 0) {
                    psb.Builder.Append("Milk=").AppendNoAlloc(Milk).Append(", ");
                }
                if (psb.Builder.Length > 1) {
                    return psb.Builder.TrimEnd(StringTrimChars).Append(']').Flush();
                } else {
                    return "[empty]";
                }
            }
        }

        static private readonly char[] StringTrimChars = new char[] { ',', ' ' };

        #endregion // Operators

        #region Serialization

        public void Write(ref ByteWriter writer) {
            writer.Write((short) Manure);
            writer.Write((short) MFertilizer);
            writer.Write((short) DFertilizer);
            writer.Write((short) Grain);
            writer.Write((short) Milk);
        }

        public void Read(ref ByteReader reader) {
            Manure = reader.Read<short>();
            MFertilizer = reader.Read<short>();
            DFertilizer = reader.Read<short>();
            Grain = reader.Read<short>();
            Milk = reader.Read<short>();
        }

        public void Write8(ref ByteWriter writer) {
            writer.Write((sbyte) Manure);
            writer.Write((sbyte) MFertilizer);
            writer.Write((sbyte) DFertilizer);
            writer.Write((sbyte) Grain);
            writer.Write((sbyte) Milk);
        }

        public void Read8(ref ByteReader reader) {
            Manure = reader.Read<sbyte>();
            MFertilizer = reader.Read<sbyte>();
            DFertilizer = reader.Read<sbyte>();
            Grain = reader.Read<sbyte>();
            Milk = reader.Read<sbyte>();
        }

        #endregion // Serialization
    }

    /// <summary>
    /// Resources utilities.
    /// </summary>
    static public class ResourceUtility {
        /// <summary>
        /// Number of resource types.
        /// </summary>
        public const int Count = (int) ResourceId.COUNT;

        /// <summary>
        /// Returns the first set ResourceId for the given mask.
        /// </summary
        static public ResourceId FirstResource(ResourceMask mask) {
            uint maskCasted = (uint) mask;
            for(int i = 0; i < Count; i++) {
                if ((maskCasted & (1u << i)) != 0) {
                    return (ResourceId) i;
                }
            }

            return ResourceId.COUNT;
        }

        /// <summary>
        /// Return the first set ResourceId in the given ResourceBlock
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        static public ResourceId FirstResource(ResourceBlock block) {
            if (block.Manure > 0) {
                return ResourceId.Manure;
            }
            if (block.MFertilizer > 0) {
                return ResourceId.MFertilizer;
            }
            if (block.DFertilizer > 0) {
                return ResourceId.DFertilizer;
            }
            if (block.Grain > 0) {
                return ResourceId.Grain;
            }
            if (block.Milk > 0) {
                return ResourceId.Milk;
            }
            return ResourceId.COUNT;
        }

        /// <summary>
        /// Returns all ResourceIds for the given mask.
        /// </summary
        static public EnumBitEnumerator<ResourceId> AllResources(ResourceMask mask)
        {
            return Bits.Enumerate<ResourceMask, ResourceId>(mask);
        }
    }
}