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
            Exiting,
            Finished
        }

        public bool IsExternal = false;
        public MeshRenderer Mesh;

        [NonSerialized] public State MoveState = State.Entering;
        [NonSerialized] public Routine MovementRoutine;

        private void OnEnable() {
            // Initialize whenever pool allocated
            MoveState = State.Entering;

            // start transparent
            Color color = Mesh.material.color;
            color.a = 0;
            Mesh.material.color = color;
        }
    }
}