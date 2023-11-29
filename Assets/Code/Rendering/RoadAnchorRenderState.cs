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
    [NonSerialized] public List<SpriteRenderer> AnchorRenderers;

    public void OnRegister()
    {
        AnchorRenderers = new List<SpriteRenderer>();
    }

    public void OnDeregister()
    {

    }
}
