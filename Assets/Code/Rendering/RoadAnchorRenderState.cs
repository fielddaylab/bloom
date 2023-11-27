using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala;

public class RoadAnchorRenderState : SharedStateComponent, IRegistrationCallbacks
{
    [NonSerialized] public bool BuildToolUpdated;
    [NonSerialized] public List<SpriteRenderer> AnchorRenderers;

    public void OnRegister()
    {
        AnchorRenderers = new List<SpriteRenderer>();

        Game.Events.Register(GameEvents.BuildToolSelected, HandleBuildToolSelected);
        Game.Events.Register(GameEvents.BuildToolDeselected, HandleBuildToolDeselected);
    }

    public void OnDeregister()
    {

    }

    #region Handlers

    private void HandleBuildToolSelected()
    {
        BuildToolUpdated = true;
    }

    private void HandleBuildToolDeselected()
    {
        BuildToolUpdated = true;
    }

    #endregion // Handlers
}
