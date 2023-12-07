using BeauUtil.Debugger;
using FieldDay;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.World;

namespace Zavala.UI {
    public class EcolDialogueModule : DialogueModuleBase, IDialogueModule {

        [SerializeField] private Button m_PipsButton;
        private bool ShowingPips;

        #region IDialogueModule
        public override void Activate(bool allowReactivate) {
            base.Activate(allowReactivate);
            m_PipsButton.gameObject.SetActive(true);
            ShowingPips = false;
            m_PipsButton.onClick.AddListener(HandlePipsToggleClicked);
        }

        public override void Deactivate() {
            base.Deactivate();
            SetPipsVisible(false);
            SetColorPressed(m_PipsButton, false);
            m_PipsButton.onClick.RemoveAllListeners();
        }
        #endregion // IDialogueModule

        #region Handlers
        private void HandlePipsToggleClicked() {
            ShowingPips = !ShowingPips;
            SetPipsVisible(ShowingPips);
            SetColorPressed(m_PipsButton, ShowingPips);
        }

        private void SetPipsVisible(bool vis) {
            Game.SharedState.Get<SimWorldState>().Overlays =
                vis ? SimWorldOverlayMask.Phosphorus : SimWorldOverlayMask.None;
        }

        #endregion //Handlers

    }
}
