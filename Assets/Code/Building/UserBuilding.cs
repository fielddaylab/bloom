using UnityEngine;
using Zavala.Data;
using static FieldDay.Scenes.PreloadManifest;

namespace Zavala.Building {
    public class UserBuilding : MonoBehaviour, IPersistBuildingComponent {
        public int IndexWithinType;

        void IPersistBuildingComponent.Read(PersistBuilding building, ref ByteReader reader) {
            reader.Read(ref IndexWithinType);
        }

        void IPersistBuildingComponent.Write(PersistBuilding building, ref ByteWriter writer) {
            writer.Write(IndexWithinType);
        }
    }
}