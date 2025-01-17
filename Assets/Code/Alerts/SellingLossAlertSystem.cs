using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;
using Zavala.UI;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public class SellingLossAlertSystem : ComponentSystemBehaviour<ResourceSupplier, ActorTimer, EventActor, StressableActor>
    {
        public override void ProcessWorkForComponent(ResourceSupplier supplier, ActorTimer timer, EventActor actor, StressableActor stressable, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            
            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.SellingLoss)) {
                if (stressable.StressImproving[(int)StressCategory.Financial])
                    // UIAlertUtility.ClearAlert(actor.DisplayingEvent);
                    EventActorUtility.ClearAndPopAlert(actor);
                    // EventActorUtility.CancelEventType(actor, EventActorAlertType.SellingLoss);
                return;
            }
            // Check if a supplier sold at a loss

            if (stressable.CurrentStress[StressCategory.Financial] >= stressable.OperationThresholds[OperationState.Okay] && !stressable.StressImproving[(int)StressCategory.Financial])
            {
                // if so, create alert on this tile
                bool sellsGrain = (supplier.ShippingMask & ResourceMask.Grain) != 0;
                OccupiesTile tile = supplier.Position;
                EventActorUtility.QueueAlert(actor, EventActorAlertType.SellingLoss, tile.TileIndex, tile.RegionIndex,
                    new NamedVariant("isFromGrainFarm", sellsGrain));
                // secondary argument for differentiating between grain farm and cafo selling at a loss
            }
        }
    }
}