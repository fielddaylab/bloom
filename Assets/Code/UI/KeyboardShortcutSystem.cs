using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using FieldDay.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    [SysUpdate(GameLoopPhase.UnscaledUpdate, 1000)]
    public class KeyboardShortcutSystem : ComponentSystemBehaviour<KeyboardShortcut> {

        public override void ProcessWork(float deltaTime) {
            foreach(var comp in m_Components) {
                if (Game.Input.IsKeyPressed(comp.Key) || Game.Input.IsKeyPressed(comp.KeyAlt)) {
                    if (Game.Input.ExecuteClick(comp.gameObject)) {
                        // TODO: ???
                    }
                }
            }
        }
    }
}