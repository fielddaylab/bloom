using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zavala.Sim;

namespace Zavala.UI {
    public class LoadFader : SharedPanel {
        public Canvas Canvas;
        public Graphic Graphic;
        public GraphicRaycaster Raycaster;

        [NonSerialized] private bool m_Showing;
        private Routine m_FadeRoutine;

        protected override void Awake() {
            base.Awake();

            m_Showing = Canvas.enabled;

            Game.Scenes.RegisterTransitionHandlers(HandleUnload, HandleLoad);
        }

        public override bool IsShowing() {
            return m_Showing;
        }

        public override bool IsVisible() {
            return Canvas.enabled;
        }

        public override bool IsTransitioning() {
            return m_FadeRoutine;
        }

        public override void Show() {
            if (!m_Showing) {
                m_Showing = true;
                BeginShow();
                m_FadeRoutine.Replace(this, FadeIn()).TryManuallyUpdate(0);
            }
        }

        public override void Hide() {
            if (m_Showing) {
                m_Showing = false;
                m_FadeRoutine.Replace(this, FadeOut());
            }
        }

        #region Routines

        private IEnumerator HandleUnload(Scene scene, StringHash32 tag) {
            if (ZavalaGame.SimTime) {
                SimTimeUtility.Pause(SimPauseFlags.Loading, ZavalaGame.SimTime);
            }
            GameLoop.SuspendUpdates(ZavalaGame.SimulationUpdateMask);
            Game.Input.PauseRaycasts();
            Show();
            yield return m_FadeRoutine.Wait();
            yield return 0.1f;
        }

        private IEnumerator HandleLoad(Scene scene, StringHash32 tag) {
            Game.Input.ResumeRaycasts();
            if (ZavalaGame.SimTime) {
                SimTimeUtility.Resume(SimPauseFlags.Loading, ZavalaGame.SimTime);
                if (!SimTimeUtility.IsPaused(SimPauseFlags.FullscreenCutscene, ZavalaGame.SimTime)) {
                    Hide();
                }
            } else {
                Hide();
            }
            return null;
        }

        private void BeginShow() {
            Graphic.gameObject.SetActive(true);
            Canvas.enabled = true;
            Raycaster.enabled = true;
        }

        private void FinishHide() {
            Canvas.enabled = false;
            Graphic.SetAlpha(0);
            Graphic.gameObject.SetActive(false);
            Raycaster.enabled = false;
        }

        private IEnumerator FadeIn() {
            return Graphic.FadeTo(1, (1 - Graphic.GetAlpha()) * 0.3f);
        }

        private IEnumerator FadeOut() {
            yield return Graphic.FadeTo(0, Graphic.GetAlpha() * 0.3f);
            FinishHide();
        }

        #endregion // Routines
    }
}