using FieldDay;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Zavala.Rendering {
    public class TilePreviewState : SharedStateComponent, IRegistrationCallbacks
    {
        public TilePreviewIcon Icon;

        [Header("Mesh Materials")]
        public Material BuildingMaterialValid;
        public Material BuildingMaterialInvalid;

        [Header("Particle Colors")]
        public Color ValidHexColor;
        public Color InvalidHexColor;
        public Color DeleteHexColor;

        [NonSerialized] public int TileIndex;
        [NonSerialized] public bool Previewing;

        public void OnRegister()
        {
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleEndBlueprintMode);
        }

        public void OnDeregister() { }

        #region Handlers

        private void HandleEndBlueprintMode()
        {
            TileIndex = -1;
        }

        #endregion // Handlers
    }
}