using System.Collections;
using BeauUtil.Tags;
using FieldDay.UI;
using Leaf.Defaults;
using TMPro;
using UnityEngine.UI;

namespace Zavala.UI {
    public class CutscenePanel : SharedRoutinePanel, ITextDisplayer {
        public LayoutGroup FrameLayout;
        public CutsceneFrame[] Frames;
        public TMP_Text Text;

        #region ITextDisplayer

        public IEnumerator CompleteLine() {
            return null;
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
            yield return null;
        }

        #endregion // ITextDisplayer
    }
}