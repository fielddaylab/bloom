using BeauUtil.Graph;
using FieldDay;
using FieldDay.SharedState;
using Zavala.Sim;

namespace Zavala.Roads {
    public sealed class RoadNetwork : SharedStateComponent, IRegistrationCallbacks {

        #region Registration

        void IRegistrationCallbacks.OnRegister() {
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        #endregion // Registration
    }

    public struct RoadPathSummary {
        public bool Connected;
        public float Distance;
    }

    static public class RoadUtility {
        static public RoadPathSummary IsConnected(RoadNetwork network, int tileIdxA, int tileIdxB) {
            // TODO: implement
            RoadPathSummary info;
            info.Connected = true;

            HexVector a = ZavalaGame.SimGrid.HexSize.FastIndexToPos(tileIdxA);
            HexVector b = ZavalaGame.SimGrid.HexSize.FastIndexToPos(tileIdxB);
            info.Distance = HexVector.EuclidianDistance(a, b);
            return info;
        }
    }
}