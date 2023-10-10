using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Scripting;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.World;

namespace Zavala.Advisor
{
    public class AdvisorButton : MonoBehaviour
    {
        [SerializeField] public AdvisorType ButtonAdvisorType;
        //[SerializeField] public GameObject PoliciesButton;
        [SerializeField] private Button m_Button;
        [SerializeField] private AdvisorButtonContainer m_Container;

        private void Start() {
            // ToggleAdvisor(false);
            //PoliciesButton.SetActive(false);

            m_Button.onClick.AddListener(HandleButtonClicked);

            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            advisorState.AdvisorButtonClicked.Register(HandleAdvisorButtonClicked);
        }

        #region Handlers

        private void HandleButtonClicked() {
            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            SimWorldState world = ZavalaGame.SimWorld;

            bool activating = advisorState.ActiveAdvisor != ButtonAdvisorType;

            //PoliciesButton.SetActive(activating);
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("advisorType", ButtonAdvisorType.ToString());
                ScriptUtility.Trigger(GameTriggers.AdvisorOpened, varTable);
            }

            if (activating) {
                AdvisorType newAdvisor = advisorState.UpdateAdvisor(ButtonAdvisorType);
                if (newAdvisor == AdvisorType.Ecology) {
                    world.Overlays = SimWorldOverlayMask.Phosphorus;
                }
                else {
                    world.Overlays = SimWorldOverlayMask.None;
                }
            }
            else {
                advisorState.UpdateAdvisor(AdvisorType.None);
                world.Overlays = SimWorldOverlayMask.None;
                ScriptUtility.Trigger(GameTriggers.AdvisorClosed);
            }

            // regardless of on or off, advisor was clicked
            advisorState.AdvisorButtonClicked?.Invoke(ButtonAdvisorType);
        }

        private void HandleAdvisorButtonClicked(AdvisorType advisorType) { 
            if (ButtonAdvisorType == advisorType) {
                return;
            }

            //m_Container.HideAdvisorButtons();
            //PoliciesButton.SetActive(false);
        }

        #endregion // Handlers
    }
}