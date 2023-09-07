using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.World;

namespace Zavala.Advisor {
    public class PolicyButton : MonoBehaviour {
        public AdvisorButton ParentButton;
        [SerializeField] private Button m_Button;

        // TODO: collapse menus when background clicked
        // TODO: propagate menu collapse through children

        private void Start() {
            m_Button.onClick.AddListener(HandleButtonClicked);
        }

        #region Handlers

        private void HandleButtonClicked() {
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("advisorType", ParentButton.ButtonAdvisorType.ToString());
                ScriptUtility.Trigger(GameTriggers.AdvisorOpened, varTable);
            }

            // Hide this button
            this.gameObject.SetActive(false);
        }

        #endregion // Handlers


    }

}