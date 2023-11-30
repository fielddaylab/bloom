using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Input
{
    [Flags]
    public enum InteractionMask : byte { 
        Dialogue = 1 << 0,
        UI = 1 << 1,
        Sim = 1 << 2,
        Cutscene = 1 << 3,
        Movement = 1 << 4, // Moving camera

        [Hidden] None = 0,
        [Hidden] All = Dialogue | UI | Sim | Cutscene | Movement,
    }


    public class InteractionState : SharedStateComponent, IRegistrationCallbacks
    {
        [AutoEnum] public InteractionMask AllowedInteractions;
        [NonSerialized] public bool InteractionUpdated;

        [NonSerialized] public RingBuffer<InteractFilter> Filters;

        public void OnRegister()
        {
            Filters = new RingBuffer<InteractFilter>(32, RingBufferMode.Expand);
            InteractionUpdated = true;
        }

        public void OnDeregister()
        {
        }
    }

    static public class InteractionUtility
    {
        static public void SetInteractions(InteractionState interactState, InteractionMask newMask)
        {
            interactState.AllowedInteractions = newMask;
            interactState.InteractionUpdated = true;
        }

        static public void EnableInteraction(InteractionState interactState, InteractionMask updateMask)
        {
            interactState.AllowedInteractions |= updateMask;
            interactState.InteractionUpdated = true;

        }

        static public void DisableInteraction(InteractionState interactState, InteractionMask updateMask)
        {
            interactState.AllowedInteractions |= ~updateMask;
            interactState.InteractionUpdated = true;
        }

        static public void RegisterFilter(InteractFilter filter)
        {
            InteractionState interact = Game.SharedState.Get<InteractionState>();

            // Save to list of filters
            interact.Filters.PushBack(filter);
        }

        static public void DeregisterFilter(InteractFilter filter)
        {
            InteractionState interact = Game.SharedState.Get<InteractionState>();

            // Save to list of filters
            interact.Filters.Remove(filter);
        }

        #region Leaf

        /// <summary>
        /// Forces player to interact with dialgue by disabling other interactions (except movement)
        /// </summary>
        [LeafMember("ForceDialogueInteraction")]
        public static void ForceDialogueInteraction()
        {
            InteractionState interactions = Game.SharedState.Get<InteractionState>();
            SetInteractions(interactions, InteractionMask.Dialogue | InteractionMask.Movement);
        }

        /// <summary>
        /// Allows player to interact with non-dialogue things
        /// </summary>
        [LeafMember("ReleaseDialogueInteraction")]
        public static void ReleaseDialogueInteraction()
        {
            InteractionState interactions = Game.SharedState.Get<InteractionState>();
            SetInteractions(interactions, InteractionMask.All);
        }

        #endregion // Leaf
    }
}