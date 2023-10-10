using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.Scripting;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.UI {
    public class DialogueBox : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {
        #region Inspector

        public DialogueBoxContents Contents;
        public DialogueBoxPin Pin;
        [SerializeField] RectTransform m_ButtonContainer = null;
        [SerializeField] private Button m_Button = null;
        [SerializeField] private Button m_CloseButton = null;
        [SerializeField] private AdvisorButtonContainer m_AdvisorButtons = null;

        [SerializeField] private RectTransform m_Rect;

        [Header("Behavior")]
        [SerializeField] private LineEndBehavior m_EndBehavior = LineEndBehavior.WaitForInput;

        [Space(5)]
        [Header("Policies")]
        [SerializeField] private GameObject m_PolicyExpansionContainer;
        [SerializeField] private Graphic m_PolicyBackground;
        [SerializeField] private PolicySlot[] m_PolicySlots;
        [SerializeField] private Button m_PolicyCloseButton;

        [Space(5)]
        [Header("Modules")]
        [SerializeField] private List<DialogueModuleBase> m_Modules;

        [Space(5)]
        [Header("Animation")]
        [SerializeField] private float m_OffscreenY = -500;
        [SerializeField] private float m_OnscreenY = -230;
        [SerializeField] private float m_OnscreenPolicyY = -250;

        #endregion // Inspector

        private Routine m_TransitionRoutine;
        private TagStringEventHandler m_LocalHandler;
        [NonSerialized] private ScriptCharacterDef m_CurrentDef;
        [NonSerialized] private bool m_FullyExpanded = false;

        private enum LineEndBehavior
        {
            WaitForInput,
            WaitFixedDuration
        }

        private void Start() {
            m_PolicyExpansionContainer.SetActive(false);

            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            advisorState.AdvisorButtonClicked.Register(HandleAdvisorButtonClicked);

            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            policyState.PolicyCloseButtonClicked.Register(HandlePolicyCloseClicked);
            m_CloseButton.onClick.AddListener(() => { policyState.PolicyCloseButtonClicked?.Invoke(); });

            m_LocalHandler = new TagStringEventHandler();
            m_LocalHandler.Register(LeafUtils.Events.Character, (d, o) => {
                LeafEvalContext evalContext = LeafEvalContext.FromObject(o);
                if (evalContext.Thread.Actor == null || m_CurrentDef == null || m_CurrentDef.IsAdvisor) {
                    Pin.Unpin();
                } else {
                    Pin.PinTo(((EventActor) evalContext.Thread.Actor).transform);
                }
            });

            m_Rect.SetAnchorPos(m_OffscreenY, Axis.Y);
        }

        private void RefreshModules(string charName) {
            foreach(var module in m_Modules) {
                if (module.UsedBy(charName)) {
                    module.Activate(false);
                }
                else {
                    module.Deactivate();
                }
            }
        }

        private void DeactivateModules() {
            foreach (var module in m_Modules) {
                module.Deactivate();
            }
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
            DeactivateModules();

            if (inString.RichText.Length > 0) {
                StringHash32 character = ScriptUtility.FindCharacterId(inString);
                if (!character.IsEmpty) {
                    UIUtility.ApplyOffsets(Contents.TextBox, Contents.BoxWithPortraitOffset);
                } else {
                    UIUtility.ApplyOffsets(Contents.TextBox, Contents.BoxDefaultOffset);
                }
                ScriptCharacterDB charDB = Game.SharedState.Get<ScriptCharacterDB>();
                ScriptCharacterDef charDef = ScriptCharacterDBUtility.Get(charDB, character);
                m_CurrentDef = charDef;
                string header, subheader;
                Sprite portraitBG, portraitImg;
                Color boxColor, panelColor, highlightColor, nameColor, titleColor, textColor;
                if (charDef != null) {
                    header = charDef.NameId;
                    subheader = charDef.TitleId;
                    portraitBG = charDef.PortraitBackground;
                    portraitImg = charDef.PortraitArt;
                    boxColor = charDef.BackgroundColor;
                    panelColor = charDef.PanelColor;
                    highlightColor = charDef.HighlightColor;
                    nameColor = charDef.NameColor;
                    titleColor = charDef.TitleColor;
                    textColor = charDef.TextColor;

                    RefreshModules(charDef.name);
                }
                else {
                    header = subheader = "";
                    portraitBG = portraitImg = null;
                    boxColor = Color.clear;
                    panelColor = Color.white;
                    highlightColor = Color.gray;
                    nameColor = Color.white;
                    titleColor = Color.black;
                    textColor = Color.black;
                }
                DialogueUIUtility.PopulateBoxText(Contents, m_Button.targetGraphic, header, subheader, inString.RichText, portraitBG, portraitImg, boxColor, highlightColor, nameColor, titleColor, textColor);
                // TODO: is it unnecessary overhead to refresh this with every PrepareLine?
                foreach (PolicySlot slot in m_PolicySlots) {
                    slot.SetColors(highlightColor, panelColor, boxColor);
                }
                m_PolicyBackground.color = panelColor;
                Contents.Contents.maxVisibleCharacters = 0;
            }

            m_TransitionRoutine.Replace(this, ShowRoutine());
            return m_LocalHandler;
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

        public void ExpandPolicyUI(AdvisorType advisorType) {
            // Load relevant policy slot types according to advisor type
            // TODO: room for more flexibility here, such as an adaptive number of slots

            PolicyType[] policyTypes = CardsUtility.AdvisorPolicyMap[advisorType];

            for (int i = 0; i < policyTypes.Count(); i++) {
                if (i == m_PolicySlots.Count()) {
                    // Defined too many policies for the number of slots!
                    break;
                }
                m_PolicySlots[i].PopulateSlot(policyTypes[i]);
            }

            m_TransitionRoutine.Replace(ExpandPolicyUIRoutine());
        }

        public void HideAdvisorUI() {
            m_TransitionRoutine.Replace(HideRoutine());
        }

        #endregion // Leaf Flag Interactions

        #region Routines

        private IEnumerator ShowRoutine() {
            this.gameObject.SetActive(true);
            m_AdvisorButtons.HideAdvisorButtons();
            m_Button.gameObject.SetActive(true);
            yield return m_Rect.AnchorPosTo(m_OnscreenY, 0.3f, Axis.Y).Ease(Curve.CubeIn);

            yield return null;
        }

        private IEnumerator HideRoutine() {
            m_FullyExpanded = false;
            m_AdvisorButtons.ShowAdvisorButtons();
            if (m_PolicyExpansionContainer.activeSelf) {
                // TODO: perform policy collapse routine
                m_PolicyExpansionContainer.SetActive(false);
            }

            yield return m_Rect.AnchorPosTo(m_OffscreenY, 0.3f, Axis.Y).Ease(Curve.CubeIn);
            m_CloseButton.gameObject.SetActive(true);
            this.gameObject.SetActive(false);
            DeactivateModules();
            Pin.Unpin();
            SimTimeInput.UnpauseEvent();

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
            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            m_PolicyCloseButton.onClick.RemoveAllListeners();
            m_PolicyCloseButton.onClick.AddListener(() => { policyState.PolicyCloseButtonClicked?.Invoke(); });

            yield return m_Rect.AnchorPosTo(m_OnscreenPolicyY, 0.1f, Axis.Y).Ease(Curve.CubeIn);

            m_PolicyExpansionContainer.SetActive(true);
            m_CloseButton.gameObject.SetActive(false);
            // TODO: populate with default localized text? Or do we like having the last line spoken displayed?
            // "Here are the current policies you can put in place, you can modify them at any time."

            m_FullyExpanded = true;

            yield return null;
        }

        #endregion // Routines

        #region Handlers

        private void HandlePolicyCloseClicked() {
            m_TransitionRoutine.Replace(HideRoutine());
            
        }

        private void HandleAdvisorButtonClicked(AdvisorType advisorType) {
            // TODO: should probably just disable advisor buttons when dialogue is showing

            // If dialogue has completed when advisor button is clicked, hide this
            if (m_FullyExpanded) {
                m_TransitionRoutine.Replace(HideRoutine());
            }
        }

        #endregion // Handlers
    }
}