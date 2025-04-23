using Leaf;
using UnityEngine;

namespace FieldDay.Scripting {
    public sealed class ScriptLoader : MonoBehaviour {
        public LeafAsset[] Scripts;

        public void OnEnable() {
            ScriptDatabase db = Game.SharedState.Get<ScriptDatabase>();
            foreach(var script in Scripts) {
                ScriptDatabaseUtility.Load(db, script);
            }
        }

        public void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            Game.SharedState.TryGet<ScriptDatabase>(out ScriptDatabase db);
            if (db) {
                foreach (var script in Scripts) {
                    ScriptDatabaseUtility.Unload(db, script);
                }
            }
        }
    }
}