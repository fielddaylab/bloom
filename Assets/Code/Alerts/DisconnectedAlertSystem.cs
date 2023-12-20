using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Roads;
using Zavala.Scripting;
using Zavala.UI;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 10)] // After MarketSystem
    public class DisconnectedAlertSystem : ComponentSystemBehaviour<RequiresConnection, EventActor, OccupiesTile>, IRegistrationCallbacks
    {
        private MarketData m_Market;
        private RoadNetwork m_Network;

        public void OnRegister()
        {

        }

        public void OnDeregister()
        {
        }

        public override void ProcessWorkForComponent(RequiresConnection requiresConnection, EventActor actor, OccupiesTile tile, float deltaTime) {

            if (tile.IsExternal)
            {
                return;
            }

            m_Market = Game.SharedState.Get<MarketData>();
            if (!m_Market.MarketTimer.HasAdvanced())
            {
                return;
            }

            m_Network = Game.SharedState.Get<RoadNetwork>();

            bool currentlyQueued = EventActorUtility.IsAlertEventQueued(actor, EventActorAlertType.Disconnected);

            // check if no outgoing flow mask
            if (m_Network.Roads.Info[tile.TileIndex].FlowMask.OutgoingCount == 0 && !currentlyQueued)
            {
                EventActorUtility.QueueAlert(actor, EventActorAlertType.Disconnected, tile.TileIndex, tile.RegionIndex);
            }
            else if (m_Network.Roads.Info[tile.TileIndex].FlowMask.OutgoingCount != 0 && currentlyQueued)
            {
                // cancel the current display
                EventActorUtility.CancelEventType(actor, EventActorAlertType.Disconnected);
                UIPools pools = Game.SharedState.Get<UIPools>();
                if (actor.DisplayingEvent)
                {
                    UIAlertUtility.FreeAlert(actor.DisplayingEvent);
                }
            }
        }
    }
}