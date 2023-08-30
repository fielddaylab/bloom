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
using UnityEngine.UI;
using UnityEngine.Windows;
using Zavala.Scripting;

namespace Zavala.UI {
    public class DialogueBox : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {
        public DialogueBoxContents Contents;
        [SerializeField] RectTransform m_ButtonContainer = null;
        [SerializeField] private Button m_Button = null;

        [SerializeField] private RectTransform m_Rect;

        [Header("Behavior")]
        [SerializeField] private LineEndBehavior m_EndBehavior = LineEndBehavior.WaitForInput;


        private Routine m_TransitionRoutine;

        private enum LineEndBehavior
        {
            WaitForInput,
            WaitFixedDuration
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

            m_TransitionRoutine.Replace(HideRoutine());
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
                string header = "";
                string subheader = "";
                if (charDef != null) {
                    header = charDef.NameId;
                    subheader = charDef.TitleId;
                }
                DialogueUIUtility.PopulateBoxText(Contents, header, subheader, inString.RichText);
                Contents.Contents.maxVisibleCharacters = 0;
            }

            m_TransitionRoutine.Replace(ShowRoutine());

            return inBaseHandler;
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

        #region Routines

        private IEnumerator ShowRoutine() {
            this.gameObject.SetActive(true);

            yield return m_Rect.AnchorPosTo(0, 0.3f, Axis.Y).Ease(Curve.CubeIn);

            yield return null;
        }

        private IEnumerator HideRoutine() {
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

        #endregion // Routines
    }
}