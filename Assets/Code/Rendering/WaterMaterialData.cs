using BeauUtil;
using FieldDay;
using UnityEditor;
using UnityEngine;
using Zavala.Sim;
using System;
using FieldDay.Components;
using FieldDay.SharedState;
using System.Collections;
using BeauRoutine;

namespace Zavala.World {
    public class WaterMaterialData : SharedStateComponent {
        public InterpolatedMaterial TopMaterial;
        public InterpolatedMaterial WaterfallMaterial;

        private void Awake() {
            TopMaterial.Load();
            WaterfallMaterial.Load();
        }
        
    }
}