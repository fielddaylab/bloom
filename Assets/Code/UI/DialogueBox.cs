using System.Collections;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Tags;
using FieldDay.Scripting;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class DialogueBox : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {
        public DialogueBoxContents Contents;

        #region Display

        public IEnumerator CompleteLine() {
            yield return 4;
        }

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (inString.RichText.Length > 0) {
                StringHash32 character = ScriptUtility.FindCharacterId(inString);
                if (!character.IsEmpty) {
                    UIUtility.ApplyOffsets(Contents.TextBox, Contents.BoxWithPortraitOffset);
                } else {
                    UIUtility.ApplyOffsets(Contents.TextBox, Contents.BoxDefaultOffset);
                }
                Contents.Contents.TryPopulate(inString.RichText);
                Contents.Contents.maxVisibleCharacters = 0;
            }

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
    }
}