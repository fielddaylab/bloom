using System;
using System.Collections.Generic;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using FieldDay.SharedState;
using Leaf;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Scripting;
using Zavala.Sim;

using Random = System.Random;

namespace FieldDay.Scripting {
    public sealed class ScriptLoader : MonoBehaviour {
        public LeafAsset[] Scripts;

        public void OnEnable() {
            ScriptDatabase db = Game.SharedState.Get<ScriptDatabase>();
            foreach(var script in Scripts) {
                ScriptDatabaseUtility.LoadNow(db, script);
            }
        }

        public void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            ScriptDatabase db = Game.SharedState.Get<ScriptDatabase>();
            foreach (var script in Scripts) {
                ScriptDatabaseUtility.Unload(db, script);
            }
        }
    }
}