using FieldDay.Components;
using UnityEngine;
using Zavala.Data;

namespace Zavala.Building {
    [RequireComponent(typeof(OccupiesTile))]
    public class PersistBuilding : BatchedComponent {
        public OccupiesTile Position;
        public Component[] PersistentComponents;

#if UNITY_EDITOR
        private void Reset() {
            Position = GetComponent<OccupiesTile>();
            PersistentComponents = GetComponents(typeof(IPersistBuildingComponent));
        }
#endif // UNITY_EDITOR
    }

    public interface IPersistBuildingComponent {
        void Write(PersistBuilding building, ref ByteWriter writer);
        void Read(PersistBuilding building, ref ByteReader reader);
    }
}