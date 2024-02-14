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

            ZavalaGame.Events
                .Register<AdvisorType>(GameEvents.AdvisorButtonClicked, HandleAdvisorButtonClicked)
                .Register(GameEvents.DialogueClosing, OnDialogueClosing);
        }

        private void OnDestroy() {
            if (!Game.IsShuttingDown) {
                Game.Events.Deregister(GameEvents.DialogueClosing, OnDialogueClosing);
            }
        }

        #region Handlers

        private void HandleButtonClicked() {
            // AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("advisorType", ButtonAdvisorType.ToString());
                ScriptUtility.Trigger(GameTriggers.AdvisorOpened, varTable);
            }

            // regardless of on or off, advisor was clicked
            ZavalaGame.Events.Dispatch(GameEvents.AdvisorButtonClicked, ButtonAdvisorType);
            // advisorState.AdvisorButtonClicked?.Invoke(ButtonAdvisorType);
        }

        private void HandleAdvisorButtonClicked(AdvisorType advisorType) { 
            if (ButtonAdvisorType == advisorType) {
                return;
            }

            //m_Container.HideAdvisorButtons();
            //PoliciesButton.SetActive(false);
        }

        private void OnDialogueClosing() {
            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();

            advisorState.UpdateAdvisor(AdvisorType.None);
        }

        #endregion // Handlers
    }
}