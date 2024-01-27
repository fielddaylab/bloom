using System;
using BeauRoutine;
using BeauRoutine.Splines;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Economy {
    public enum FulfillerType {
        Truck,
        Airship,
        Parcel
    }

    public enum FulfillerState {
        Init,
        Traveling,
        Arrived
    }

    [DisallowMultipleComponent]
    public sealed class RequestFulfiller : BatchedComponent {
        public FulfillerType FulfillerType;

        // TODO: only expose this if FulfillerType is Truck?
        [Header("Trucks Only")]
        [SerializeField] public MeshFilter TruckMesh;
        [SerializeField] public MeshRenderer TruckRenderer;

        [NonSerialized] public ResourceSupplier Source;
        [NonSerialized] public ResourceBlock Carrying;
        [NonSerialized] public ResourceRequester Target;
        [NonSerialized] public GeneratedTaxRevenue Revenue;

        // target positions
        [NonSerialized] public FulfillerState State;
        [NonSerialized] public int SourceTileIndex;
        [NonSerialized] public int TargetTileIndex;
        [NonSerialized] public Vector3 SourceWorldPos;
        [NonSerialized] public Vector3 TargetWorldPos;
        [NonSerialized] public RingBuffer<ushort> NodeQueue = new RingBuffer<ushort>(16, RingBufferMode.Expand);
        [NonSerialized] public Vector3 NextNodePos;
        [NonSerialized] public SimpleSpline NextNodeSpline;

        [NonSerialized] public bool IsIntermediary; // true if this fulfiller confers responsibility along a chain (e.g. export depot)
        [NonSerialized] public bool AtTransitionPoint; // true if this fulfiller is ready to change (i.e. from truck to blimp, or when completing delivery)
        [NonSerialized] public bool ExternalSrc; 
    }

    static public class FulfillerUtility {
        static public void InitializeFulfiller(RequestFulfiller unit, MarketActiveRequestInfo request, RoadPathSummary path, bool isExternal = false) {
            unit.Source = request.Supplier;
            unit.Carrying = request.Supplied;
            unit.Target = request.Requester;
            unit.Revenue = request.Revenue;

            unit.State = FulfillerState.Init;

            unit.TargetWorldPos = unit.Target.transform.position;
            unit.SourceWorldPos = unit.Source.transform.position;

            unit.transform.position = unit.SourceWorldPos;

            unit.SourceTileIndex = unit.Source.Position.TileIndex;
            unit.TargetTileIndex = unit.Target.Position.TileIndex;

            unit.ExternalSrc = isExternal;

            unit.NodeQueue.Clear();

            // ensure capacity
            if (unit.NodeQueue.Capacity < path.Tiles.Length - 1) {
                unit.NodeQueue.SetCapacity(Mathf.NextPowerOfTwo(path.Tiles.Length - 1));
            }

            int start, length = path.Tiles.Length - 1;
            if ((path.Flags & RoadPathFlags.Reversed) != 0) {
                start = 0;
            } else {
                start = 1;
            }

            // push path
            for (int i = 0; i < length; i++) {
                unit.NodeQueue.PushBack(path.Tiles[start + i]);
            }

            if ((path.Flags & RoadPathFlags.Reversed) != 0) {
                unit.NodeQueue.Reverse();
            }

            unit.IsIntermediary = false;
        }

        static public void InitializeFulfiller(RequestFulfiller unit, MarketActiveRequestInfo request, Vector3 sourceWorldPos, bool isExternal = false) {
            unit.Source = request.Supplier;
            unit.Carrying = request.Supplied;
            unit.Target = request.Requester;
            unit.Revenue = request.Revenue;

            unit.State = FulfillerState.Init;

            unit.TargetWorldPos = unit.Target.transform.position;
            unit.SourceWorldPos = sourceWorldPos;

            unit.transform.position = unit.SourceWorldPos;

            // unit.SourceTileIndex = unit.Source.Position.TileIndex;
            unit.TargetTileIndex = unit.Target.Position.TileIndex;
            unit.NodeQueue.Clear();

            unit.IsIntermediary = false;

            unit.ExternalSrc = isExternal;
        }
    }
}