using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;

namespace Zavala.Rendering
{
    public class TilePreviewState : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public Vector2 PrevMousePosition;

        [NonSerialized] public OccupiesTile LeadDestroyIcon;
        [NonSerialized] public SnapToTile LeadDestroySnap;
        [NonSerialized] public List<OccupiesTile> ActiveDestroyIcons;

        public void OnRegister()
        {
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleEndBlueprintMode);
        }

        public void OnDeregister() { }

        #region Handlers

        private void HandleEndBlueprintMode()
        {
            PrevMousePosition = Vector2.negativeInfinity;
        }

        #endregion // Handlers
    }
}