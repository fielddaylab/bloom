using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zavala.Input;
using Zavala.Sim;

namespace Zavala.UI.Info {
    [SysUpdate(GameLoopPhase.Update, 100)]
    public class InfoPopupSystem : SharedStateSystemBehaviour<InputState, SimTimeState> {
        private const SimPauseFlags DisallowFlags = SimPauseFlags.Cutscene | SimPauseFlags.Blueprints | SimPauseFlags.Scripted;
        
        public override void ProcessWork(float deltaTime) {
            InfoPopup popupUI = Game.Gui.GetShared<InfoPopup>();

            if (popupUI == null) {
                return;
            }

            if ((m_StateB.Paused & DisallowFlags) != 0) {
                popupUI.Hide();
                return;
            }

            if (m_StateA.ButtonPressed(InputButton.PrimaryMouse)) {
                HasInfoPopup infoPopup = GetInfoPopup(m_StateA.ViewportMouseRay);
                if (infoPopup != null) {
                    popupUI.LoadTarget(infoPopup);
                } else if (!EventSystem.current.IsPointerOverGameObject()) {
                    popupUI.Hide();
                }
            }
        }

        static private HasInfoPopup GetInfoPopup(Ray mouseRay) {
            if (Physics.Raycast(mouseRay, out RaycastHit hit, 200, LayerMasks.Building_Mask)) {
                return hit.collider.GetComponent<HasInfoPopup>();
            } else {
                return null;
            }
        }
    }
}