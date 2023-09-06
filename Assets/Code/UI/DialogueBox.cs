using System.Collections;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Tags;
using FieldDay;
using FieldDay.Scripting;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
using UnityEngine.Windows;
using Zavala.Scripting;

namespace Zavala.UI {
    public class DialogueBox : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {
        #region Inspector

        public DialogueBoxContents Contents;
        [SerializeField] RectTransform m_ButtonContainer = null;
        [SerializeField] private Button m_Button = null;

        [SerializeField] private RectTransform m_Rect;

        [Header("Behavior")]
        [SerializeField] private LineEndBehavior m_EndBehavior = LineEndBehavior.WaitForInput;

        [Space(5)]
        [Header("Policies")]
        [SerializeField] private GameObject m_PolicyExpansionContainer;
        [SerializeField] private Button m_PolicyCloseButton;

        #endregion // Inspector

        private Routine m_TransitionRoutine;

        private enum LineEndBehavior
        {
            WaitForInput,
            WaitFixedDuration
        }

        private void Start() {
            m_PolicyExpansionContainer.SetActive(false);    
        }

        #region Display

        public IEnumerator CompleteLine() {
            switch (m_EndBehavior) {
                case LineEndBehavior.WaitForInput: {
                        yield return WaitForInput();
                        break;
                    }

                case LineEndBehavior.WaitFixedDuration: {
                        yield return 4;
                        break;
                    }
            }
        }

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (inString.RichText.Length > 0) {
                StringHash32 character = ScriptUtility.FindCharacterId(inString);
                if (!character.IsEmpty) {
                    UIUtility.ApplyOffsets(Contents.TextBox, Contents.BoxWithPortraitOffset);
                } else {
                    UIUtility.ApplyOffsets(Contents.TextBox, Contents.BoxDefaultOffset);
                }
                ScriptCharacterDB charDB = Game.SharedState.Get<ScriptCharacterDB>();
                ScriptCharacterDef charDef = ScriptCharacterDBUtility.Get(charDB, character);
                string header, subheader;
                Sprite portraitBG, portraitImg;
                Color boxColor, nameColor, titleColor, textColor;
                if (charDef != null) {
                    header = charDef.NameId;
                    subheader = charDef.TitleId;
                    portraitBG = charDef.PortraitBackground;
                    portraitImg = charDef.PortraitArt;
                    boxColor = charDef.BackgroundColor;
                    nameColor = charDef.NameColor;
                    titleColor = charDef.TitleColor;
                    textColor = charDef.TextColor;
                }
                else {
                    header = subheader = "";
                    portraitBG = portraitImg = null;
                    boxColor = Color.clear;
                    nameColor = Color.white;
                    titleColor = Color.black;
                    textColor = Color.black;
                }
                DialogueUIUtility.PopulateBoxText(Contents, m_Button.targetGraphic, header, subheader, inString.RichText, portraitBG, portraitImg, boxColor, nameColor, titleColor, textColor);
                Contents.Contents.maxVisibleCharacters = 0;
            }

            m_TransitionRoutine.Replace(ShowRoutine());

            return null;
        }

        public IEnumerator ShowChoice(LeafChoice inChoice, LeafThreadState inThread, ILeafPlugin inPlugin) {
            //throw new System.NotImplementedException();
            yield break;
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            Contents.Contents.maxVisibleCharacters += inType.VisibleCharacterCount;
            yield return 0.1f;
        }

        #endregion // Display

        #region Leaf Flag Interactions

        public void ExpandPolicyUI() { 
            m_TransitionRoutine.Replace(ExpandPolicyUIRoutine());
        }

        public void HideAdvisorUI() {
            m_TransitionRoutine.Replace(HideRoutine());
        }

        #endregion // Leaf Flag Interactions

        #region Routines

        private IEnumerator ShowRoutine() {
            this.gameObject.SetActive(true);

            yield return m_Rect.AnchorPosTo(0, 0.3f, Axis.Y).Ease(Curve.CubeIn);

            yield return null;
        }

        private IEnumerator HideRoutine() {
            if (m_PolicyExpansionContainer.activeSelf) {
                // TODO: perform policy collapse routine
                m_PolicyExpansionContainer.SetActive(false);
            }

            yield return m_Rect.AnchorPosTo(-500, 0.3f, Axis.Y).Ease(Curve.CubeIn);

            this.gameObject.SetActive(false);

            yield return null;
        }

        private IEnumerator WaitForInput() {
            if (!m_ButtonContainer) {
                yield break;
            }

            m_ButtonContainer.gameObject.SetActive(true);
            yield return Routine.Race(
                m_Button == null ? null : m_Button.onClick.WaitForInvoke()
            );
            m_ButtonContainer.gameObject.SetActive(false);
            yield break;
        }

        private IEnumerator ExpandPolicyUIRoutine() {
            m_PolicyCloseButton.onClick.RemoveAllListeners();
            m_PolicyCloseButton.onClick.AddListener(OnPolicyClose);

            yield return m_Rect.AnchorPosTo(-100, 0.3f, Axis.Y).Ease(Curve.CubeIn);

            m_PolicyExpansionContainer.SetActive(true);

            // TODO: populate with default localized text? Or do we like having the last line spoken displayed?
            // "Here are the current policies you can put in place, you can modify them at any time."

            yield return null;
        }

        #endregion // Routines

        #region Handlers

        private void OnPolicyClose() {
            m_TransitionRoutine.Replace(HideRoutine());
        }

        #endregion // Handlers
    }
}