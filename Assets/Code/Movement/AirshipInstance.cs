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

        protected override void OnEnable() {
            base.OnEnable();

            // Initialize whenever pool allocated
            MoveState = State.Entering;

            // start transparent
            /* NOTE: disabled until we use a shader that supports transparency
            Color color = Mesh.material.color;
            color.a = 0;
            Mesh.material.color = color;
            */
        }
    }
}