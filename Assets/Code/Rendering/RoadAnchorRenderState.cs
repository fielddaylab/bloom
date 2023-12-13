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
    [NonSerialized] public List<ParticleSystem> AnchorRenderers;

    public void OnRegister()
    {
        AnchorRenderers = new List<ParticleSystem>();
    }

    public void OnDeregister()
    {

    }
}
