
using UnityEngine;
using FieldDay.Components;
using Zavala.Roads;

namespace Zavala.Economy {
    public sealed class TollBooth : BatchedComponent {
        public OccupiesTile TileA;
        public OccupiesTile TileB;
        public TileDirection AToBDirection;

        protected override void OnEnable() {
            base.OnEnable();

            SetDirection();
            RoadUtility.RegisterSource(TileA, RoadDestinationMask.Tollbooth);
            RoadUtility.RegisterSource(TileB, RoadDestinationMask.Tollbooth);
            RoadUtility.RegisterDestination(TileA, RoadDestinationMask.Tollbooth);
            RoadUtility.RegisterDestination(TileB, RoadDestinationMask.Tollbooth);
        }

        protected override void OnDisable() {
            RoadUtility.DeregisterSource(TileA);
            RoadUtility.DeregisterSource(TileB);
            RoadUtility.DeregisterDestination(TileA);
            RoadUtility.DeregisterDestination(TileB);


            base.OnDisable();
        }

        private void SetDirection() {
            for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                HexVector adjPos = HexVector.Offset(TileA.TileVector, dir);
                if (adjPos != TileB.TileVector) {
                    continue;
                } else {
                    this.AToBDirection = dir;
                }
            }
            RoadNetwork network = ZavalaGame.SharedState.Get<RoadNetwork>();
            network.Roads.Info[TileA.TileIndex].FlowMask[AToBDirection] = true;
            network.Roads.Info[TileB.TileIndex].FlowMask[AToBDirection.Reverse()] = true;
        }
    }


}