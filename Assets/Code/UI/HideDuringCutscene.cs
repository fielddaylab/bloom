using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.UI {
    [RequireComponent(typeof(CanvasGroup))]
    public class HideDuringCutscene : MonoBehaviour {
        private const SimPauseFlags HideFlags = (SimPauseFlags.Cutscene | SimPauseFlags.FullscreenCutscene | SimPauseFlags.DialogBox);

        [NonSerialized] private CanvasGroup m_Group;
        [NonSerialized] private bool m_LastState;
        
        private Routine m_StateTransition;

        private readonly Action<SimPauseFlags> OnFlagsUpdatedDelegate;

        private HideDuringCutscene() {
            OnFlagsUpdatedDelegate = OnFlagsUpdated;
        }

        private void Awake() {
            this.CacheComponent(ref m_Group);
            m_LastState = m_Group.alpha > 0;
        }

        private void OnEnable() {
            SimTimeUtility.OnPauseUpdated.Register(OnFlagsUpdatedDelegate);
            
            bool show = (ZavalaGame.SimTime.Paused & HideFlags) == 0;
            m_LastState = show;
            m_Group.alpha = show ? 1 : 0;
            m_Group.blocksRaycasts = show;
        }

        private void OnDisable() {
            SimTimeUtility.OnPauseUpdated.Deregister(OnFlagsUpdatedDelegate);
            m_StateTransition.Stop();
            if (!m_LastState) {
                m_Group.alpha = 0;
                m_Group.blocksRaycasts = false;
            }
        }

        private void OnFlagsUpdated(SimPauseFlags flags) {
            bool show = (flags & HideFlags) == 0;
            if (show == m_LastState) {
                return;
            }

            m_LastState = show;
            if (show) {
                m_Group.blocksRaycasts = true;
                m_StateTransition.Replace(this, m_Group.FadeTo(1, 0.15f));
            } else {
                m_Group.blocksRaycasts = false;
                m_StateTransition.Replace(this, m_Group.FadeTo(0, 0.15f));
            }
        }
    }
}