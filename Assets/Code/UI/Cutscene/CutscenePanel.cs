using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.UI;
using Leaf.Defaults;
using Leaf.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Input;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.UI {
    public class CutscenePanel : SharedRoutinePanel, ITextDisplayer {
        public Canvas Canvas;
        public LayoutGroup FrameLayout;
        public CutsceneFrame[] Frames;
        public TMP_Text Text;
        public GameObject NextHint;

        [NonSerialized] private int m_LoadedFrames;
        [NonSerialized] private int m_DisplayedFrames;
        [NonSerialized] private Routine m_PrepareRoutine;
        [NonSerialized] private Routine m_NextRoutine;

        protected override void Awake() {
            base.Awake();

            Assert.True(Frames.Length == 3);
            foreach (var frame in Frames) {
                frame.Clear();
            }
            Text.SetText(string.Empty);

            NextHint.gameObject.SetActive(false);
        }

        #region ITextDisplayer

        public IEnumerator CompleteLine() {
            InputState input = Game.SharedState.Get<InputState>();

            NextHint.gameObject.SetActive(true);
            while(!input.ButtonPressed(InputButton.PrimaryMouse)) {
                yield return null;
            }
            input.ConsumedButtons |= InputButton.PrimaryMouse;
            NextHint.gameObject.SetActive(false);
        }

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (string.IsNullOrEmpty(inString.RichText)) {
                return null;
            }

            Text.SetText(inString.RichText);
            Text.maxVisibleCharacters = 0;
            return null;
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            int chars = inType.VisibleCharacterCount;

            InputState input = Game.SharedState.Get<InputState>();
            while (chars-- > 0) {
                Text.maxVisibleCharacters += 1;

                float delay = 0.02f;
                while(delay > 0) {
                    delay -= Routine.DeltaTime;
                    if (input.ButtonPressed(InputButton.PrimaryMouse)) {
                        delay = 0;
                        Text.maxVisibleCharacters += chars;
                        chars = 0;
                        break;
                    }
                    yield return null;
                }
            }
        }

        #endregion // ITextDisplayer

        #region Frames

        public IEnumerator PrepareFrames(StringSlice frameA, StringSlice frameB, StringSlice frameC) {
            return (m_PrepareRoutine.Replace(this, PrepareFramesInternal(frameA, frameB, frameC))).Wait();
        }

        private IEnumerator PrepareFramesInternal(StringSlice frameA, StringSlice frameB, StringSlice frameC) {
            yield return CloseAllFramesInternal();

            int frameCount = 0;

            if (!frameA.IsEmpty) {
                Frames[frameCount].gameObject.SetActive(true);
                Frames[frameCount].Texture.Path = "cutscene/" + frameA.ToString();
                Frames[frameCount].Texture.Preload();
                frameCount++;
            }

            if (!frameB.IsEmpty) {
                Frames[frameCount].gameObject.SetActive(true);
                Frames[frameCount].Texture.Path = "cutscene/" + frameB.ToString();
                Frames[frameCount].Texture.Preload();
                frameCount++;
            }

            if (!frameC.IsEmpty) {
                Frames[frameCount].gameObject.SetActive(true);
                Frames[frameCount].Texture.Path = "cutscene/" + frameC.ToString();
                Frames[frameCount].Texture.Preload();
                frameCount++;
            }

            m_LoadedFrames = frameCount;

            for (int i = frameCount; i < Frames.Length; i++) {
                Frames[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < frameCount; i++) {
                while (Frames[i].Texture.IsLoading()) {
                    yield return null;
                }
            }

            FrameLayout.ForceRebuild();
        }

        public IEnumerator CloseAllFrames() {
            return (m_PrepareRoutine.Replace(this, CloseAllFramesInternal())).Wait();
        }

        private IEnumerator CloseAllFramesInternal() {
            m_NextRoutine.Stop();

            if (m_LoadedFrames == 0) {
                m_DisplayedFrames = 0;
                yield break;
            }

            IEnumerator[] hideRoutines = new IEnumerator[m_DisplayedFrames];
            for (int i = 0; i < m_LoadedFrames; i++) {
                hideRoutines[i] = Frames[i].AnimateOff((m_DisplayedFrames - i - 1) * 0.1f);
            }
            yield return Routine.Combine(hideRoutines);

            m_LoadedFrames = m_DisplayedFrames = 0;
            ClearText();
        }

        public void AdvanceFrame() {
            if (m_DisplayedFrames < m_LoadedFrames) {
                CutsceneFrame frame = Frames[m_DisplayedFrames++];
                frame.Animation.Replace(this, frame.AnimateOn(0));
            }
        }

        public void AdvanceRemainingFrames(float delayPerFrame) {
            float delay = 0;
            while (m_DisplayedFrames < m_LoadedFrames) {
                CutsceneFrame frame = Frames[m_DisplayedFrames++];
                frame.Animation.Replace(this, frame.AnimateOn(delay));
                delay += delayPerFrame;
            }
        }

        public void ClearText() {
            Text.SetText(string.Empty);
            Text.maxVisibleCharacters = 0;
            NextHint.gameObject.SetActive(false);
        }

        #endregion // Frames

        #region Panel

        protected override IEnumerator TransitionToHide() {
            yield return CanvasGroup.FadeTo(0, 0.3f);
            Root.gameObject.SetActive(false);
        }

        protected override IEnumerator TransitionToShow() {
            Root.gameObject.SetActive(true);
            yield return CanvasGroup.FadeTo(1, 0.3f);
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            Canvas.enabled = true;
            ScriptUtility.MountDisplayer(this);
            SimTimeUtility.Pause(SimPauseFlags.FullscreenCutscene, ZavalaGame.SimTime);
        }

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShowComplete(inbInstant);

            //Game.SharedState.Get<SimWorldCamera>().Camera.enabled = false;
        }

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);

            if (Game.IsShuttingDown) {
                return;
            }

            ScriptUtility.UnmountDisplayer(this);
            m_PrepareRoutine.Stop();
            m_NextRoutine.Stop();
            //Game.SharedState.Get<SimWorldCamera>().Camera.enabled = true;
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHideComplete(inbInstant);

            CanvasGroup.alpha = 0;
            ClearText();

            if (WasShowing()) {
                m_LoadedFrames = m_DisplayedFrames = 0;
                foreach (var obj in Frames) {
                    obj.Clear();
                    obj.gameObject.SetActive(false);
                }

                if (!Game.IsShuttingDown) {
                    SimTimeUtility.Resume(SimPauseFlags.FullscreenCutscene, ZavalaGame.SimTime);
                }
            }

            Canvas.enabled = false;
        }

        #endregion // Panel

        #region Leaf

        [LeafMember("CutsceneBegin")]
        static public void Begin() {
            Game.Gui.GetShared<CutscenePanel>().Show();
        }

        [LeafMember("CutscenePrepareImages")]
        static public IEnumerator LoadImages(StringSlice frameA = default, StringSlice frameB = default, StringSlice frameC = default) {
            return Game.Gui.GetShared<CutscenePanel>().PrepareFrames(frameA, frameB, frameC);
        }

        [LeafMember("CutsceneAllImages")]
        static public void AdvanceImage(float delayBetweenImages) {
            Game.Gui.GetShared<CutscenePanel>().AdvanceRemainingFrames(delayBetweenImages);
        }

        [LeafMember("CutsceneNextImage")]
        static public void AdvanceImage() {
            Game.Gui.GetShared<CutscenePanel>().AdvanceFrame();
        }

        [LeafMember("CutsceneClearText")]
        static private void LeafClearText() {
            Game.Gui.GetShared<CutscenePanel>().ClearText();
        }

        [LeafMember("CutsceneClearImages")]
        static public IEnumerator ClearImages() {
            return Game.Gui.GetShared<CutscenePanel>().CloseAllFrames();
        }

        [LeafMember("CutsceneEnd")]
        static public void End() {
            
            Game.Gui.GetShared<CutscenePanel>().Hide();
        }

        #endregion // Leaf
    }
}