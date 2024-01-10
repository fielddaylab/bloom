using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Rendering;
using FieldDay.Scripting;
using FieldDay.Systems;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zavala.Input;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.UI.Info {
    [SysUpdate(GameLoopPhase.Update, 100)]
    public class InfoPopupSystem : SharedStateSystemBehaviour<InputState, SimTimeState> {
        private const SimPauseFlags DisallowFlags = SimPauseFlags.Cutscene | SimPauseFlags.Blueprints | SimPauseFlags.Scripted;
        
        public override void ProcessWork(float deltaTime) {
            InfoPopup popupUI = Game.Gui.GetShared<InfoPopup>();

            if (popupUI == null) {
                return;
            }

            if ((m_StateB.Paused & DisallowFlags) != 0 && !popupUI.HoldOpen) {
                popupUI.Hide();
                return;
            }

            if (m_StateA.ButtonPressed(InputButton.PrimaryMouse)) {
                if (SimWorldUtility.IsPointerOverUI() && !popupUI.HoldOpen) {
                    popupUI.Hide();
                } else {
                    HasInfoPopup infoPopup = GetInfoPopup(m_StateA.ViewportMouseRay);
                    if (infoPopup != null) {
                        popupUI.LoadTarget(infoPopup);
                        WorldCameraUtility.PanCameraToTransform(infoPopup.transform);
                    } else if (!popupUI.HoldOpen) {
                        popupUI.Hide();
                    }
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

    public static class InfoPopupUtility {

        [LeafMember("OpenInfoPopup")]
        static public void OpenActorInfo(StringHash32 id, bool holdOpen = true) {
            if (!ScriptUtility.ActorExists(id)) {
                Log.Error("[InfoPopupSystem] Error: tried to open info for nonexistent actor.");
                return;
            }

            if (ScriptUtility.LookupActor(id).TryGetComponent(out HasInfoPopup target)) {
                InfoPopup ip = Game.Gui.GetShared<InfoPopup>();
                ip.LoadTarget(target);
                ip.HoldOpen = holdOpen;
                WorldCameraUtility.PanCameraToTransform(target.transform);
            }
        }

        [LeafMember("CloseInfoPopup")]
        static public void CloseInfoPopup() {
            InfoPopup ip = Game.Gui.GetShared<InfoPopup>();
            ip.HoldOpen = false;
            ip.Hide();
        }
    }
}