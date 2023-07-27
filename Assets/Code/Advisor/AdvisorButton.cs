using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.World;

namespace Zavala.Advisor {
    public class AdvisorButton : MonoBehaviour {
        [SerializeField] public AdvisorType ButtonAdvisorType;
        [SerializeField] public GameObject PoliciesButton;
        [SerializeField] public GameObject PoliciesWindow;

        public void ToggleAdvisor(bool toggle) {
            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            SimWorldState world = ZavalaGame.SimWorld;

            PoliciesButton.SetActive(toggle);
            PoliciesButton.TryGetComponent<Toggle>(out Toggle advisorToggle);
            advisorToggle.SetIsOnWithoutNotify(false);
            TogglePolicies(false);

            if (toggle) {
                if (advisorState.UpdateAdvisor(ButtonAdvisorType) == AdvisorType.Environment) {
                    world.Overlays = SimWorldOverlayMask.Phosphorus;
                } else {
                    world.Overlays = SimWorldOverlayMask.None;
                }
            } else {
                advisorState.UpdateAdvisor(AdvisorType.None);
                world.Overlays = SimWorldOverlayMask.None;
            }
        }

        public void TogglePolicies(bool toggle) {
            PoliciesWindow.SetActive(toggle);
        }


    }


}