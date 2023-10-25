using BeauRoutine;
using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Movement
{
    public class AirshipInstance : BatchedComponent
    {
        public enum State : byte { 
            Entering,
            EnRoute,
            Exiting
        }

        public bool IsExternal = false;

        [NonSerialized] public State MoveState = State.Entering;
        [NonSerialized] public Routine MovementRoutine;
    }
}