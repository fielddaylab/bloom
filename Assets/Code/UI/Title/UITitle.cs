using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Zavala.UI
{
    public class UITitle : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_StartButton; // New Game
        [SerializeField] private Button m_LoadButton;
        [SerializeField] private Button m_CreditsButton;
        [SerializeField] private Button m_SettingsButton;

        [SerializeField] private UISettings m_SettingsPanel;
        [SerializeField] private UICredits m_CreditsPanel;

        #endregion // Inspector

        #region Unity Callbacks

        private void OnEnable()
        {
            m_StartButton.onClick.AddListener(HandleStartClicked);
            m_CreditsButton.onClick.AddListener(HandleCreditsClicked);
            m_SettingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        #endregion // Unity Callbacks

        #region Button Handlers

        private void HandleStartClicked()
        {
            SceneManager.LoadScene("MainScene");
        }

        private void HandleCreditsClicked()
        {
            m_CreditsPanel.OpenPanel();
        }

        private void HandleSettingsClicked()
        {
            m_SettingsPanel.OpenPanel();
        }

        #endregion // Button Handlers
    }
}
