using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Debugging;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;

namespace Zavala.Sim
{
    public class PhosphorusSkimmerSystem : ComponentSystemBehaviour<PhosphorusSkimmer, ActorTimer, OccupiesTile>
    {
        public override void ProcessWorkForComponent(PhosphorusSkimmer skimmer, ActorTimer timer, OccupiesTile position, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            // Remove P from tile
            SimPhosphorusState pState = Game.SharedState.Get<SimPhosphorusState>();
            int removedAmt = SimPhospohorusUtility.RemovePhosphorus(pState, position.TileIndex, skimmer.SkimAmt);
            Debug.Log("[Skimmer] Removed " + removedAmt + "  units of P");
        }
    }

    static public class PhosphorusSkimmerUtility
    {

    }
}