using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public class SellingLossAlertSystem : ComponentSystemBehaviour<ResourceSupplier, ActorTimer, EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(ResourceSupplier supplier, ActorTimer timer, EventActor actor, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            StressableActor stressable = supplier.GetComponent<StressableActor>();
            if (!stressable) { return; }

            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.SellingLoss)) {
                if (stressable.StressImproving)
                    EventActorUtility.CancelEventType(actor, EventActorAlertType.SellingLoss);
                return;
            }
            // Check if a supplier sold at a loss

            if (stressable.CurrentStress[StressCategory.Financial] >= stressable.OperationThresholds[OperationState.Okay])
            {
                // if so, create alert on this tile
                bool sellsGrain = (supplier.ShippingMask & ResourceMask.Grain) != 0;
                EventActorUtility.QueueAlert(actor, EventActorAlertType.SellingLoss, tile.TileIndex, tile.RegionIndex,
                    new NamedVariant("isFromGrainFarm", sellsGrain));
                // secondary argument for differentiating between grain farm and cafo selling at a loss
            }
        }
    }
}