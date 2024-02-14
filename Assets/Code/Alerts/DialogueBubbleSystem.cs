
using BeauUtil.Variants;
using FieldDay.Systems;
using Zavala.Scripting;

namespace Zavala.Sim {

    public class DialogueBubbleSystem : ComponentSystemBehaviour<EventActor, OccupiesTile> {

        public override void ProcessWorkForComponent(EventActor actor, OccupiesTile tile, float deltaTime) {


            //tile.BuildingType
            //tile.Region

            // don't try to create a dialogue bubble if any other events exist
            if (EventActorUtility.IsAlertQueued(actor)) {
                return;
            }

            NamedVariant type = new("buildingType", tile.Type.ToString());
            EventActorUtility.QueueAlert(actor, EventActorAlertType.Dialogue, tile.TileIndex, tile.RegionIndex, type);

        }
    }
}