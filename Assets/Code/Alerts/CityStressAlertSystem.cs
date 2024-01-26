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
    public class CityStressAlertSystem : ComponentSystemBehaviour<StressableActor, ActorTimer, EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(StressableActor stressActor, ActorTimer timer, EventActor eventActor, OccupiesTile tile, float deltaTime)
        {
            if (!timer.Timer.HasAdvanced())
            {
                return;
            }
            if (tile.Type != BuildingType.City)
            {
                return;
            }

            // if city is sufficiently stressed...
            if (StressUtility.IsPeakStressed(stressActor, StressCategory.Resource) || StressUtility.IsPeakStressed(stressActor, StressCategory.Bloom) && stressActor.OperationState == OperationState.Bad)
            {
                // and there is not already a declining pop alert...
                if (!EventActorUtility.IsAlertQueued(eventActor, EventActorAlertType.DecliningPop))
                {
                    // queue it
                    EventActorUtility.QueueAlert(eventActor, EventActorAlertType.DecliningPop, tile.TileIndex, tile.RegionIndex);
                    return;
                }
            }
            // else city is not super stressed
            else
            {
                // if an existing declining pop alert exists...
                if (EventActorUtility.IsAlertQueued(eventActor, EventActorAlertType.DecliningPop))
                {
                    // remove it if the stress levels are improving
                    if (stressActor.StressImproving[(int)StressCategory.Resource])
                    {
                        UIAlertUtility.ClearAlert(eventActor.DisplayingEvent);
                    }
                    return;
                }
            }
        }
    }
}