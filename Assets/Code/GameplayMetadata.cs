using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala
{
    public class GameplayMetadata : MonoBehaviour
    {
        public static GameplayMetadata Instance;

        public bool GameWinToTitle; // true if player returns to title screen after winning the game

        private bool m_Initialized;

        private void OnEnable()
        {
            if (m_Initialized)
            {
                return;
            }

            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != null)
            {
                Destroy(this.gameObject);
            }

            m_Initialized = true;

            ZavalaGame.Events.Register(GameEvents.GameWon, HandleGameWon);
        }

        #region // Handlers

        private void HandleGameWon()
        {
            GameWinToTitle = true;
        }

        #endregion // Handlers
    }
}