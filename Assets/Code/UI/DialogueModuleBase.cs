using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Scripting;

namespace Zavala.UI {
    public abstract class DialogueModuleBase : MonoBehaviour, IDialogueModule
    {
        [SerializeField] private ScriptCharacterRemap[] m_UsedBy; // The categories of characters who use this dialogue module
        [SerializeField] private bool Unlocked;
        [SerializeField] public AdvisorType m_AdvisorType;

        private bool m_IsActive = false;

        #region Unity Callbacks

        private void Start() {
            if (!m_IsActive) {
                gameObject.SetActive(false);
            }
        }

        #endregion // Unity Callbacks

        #region IDialogueModule

        public virtual void Activate(bool allowReactivate) {
            if (m_IsActive && !allowReactivate) {
                return;
            }
            if (!Unlocked) {
                return;
            }

            this.gameObject.SetActive(true);
            m_IsActive = true;
        }

        public virtual void Deactivate() {
            if (!m_IsActive) {
                return;
            }

            this.gameObject.SetActive(false);
            m_IsActive = false;
        }

        /// <summary>
        /// Returns true if the provided character name is found in this module's character categories
        /// </summary>
        /// <param name="charName"></param>
        /// <returns></returns>
        public bool UsedBy(string charName) {
            foreach (var remap in m_UsedBy) {
                if (remap.Contains(charName)) {
                    return true;
                }
            }

            return false;
        }

        public void Unlock() {
            if (Unlocked) {
                BeauUtil.Debugger.Log.Msg("[DialogueModuleBase] Attempted to unlock {0} module, but it's already unlocked!", m_AdvisorType);
            }
            Unlocked = true;
        }

        protected void SetColorPressed(Button button, bool pressed) {
            if (pressed) {
                button.targetGraphic.color = button.colors.pressedColor;
            } else {
                button.targetGraphic.color = button.colors.normalColor;
            }
        }

        #endregion // IDialogueModule

    }
}

