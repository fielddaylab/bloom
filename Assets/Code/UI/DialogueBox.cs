using System;
using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.Input;
using Zavala.Scripting;
using Zavala.Sim;
using Zavala.World;
using Zavala.Data;
using Zavala.UI.Info;

namespace Zavala.UI {
    public class DialogueBox : MonoBehaviour, ITextDisplayer, IChoiceDisplayer, IScenePreload {
        #region Inspector

        public DialogueBoxContents Contents;
        public DialogueBoxPin Pin;
        [SerializeField] RectTransform m_ButtonContainer = null;
        [SerializeField] private Button m_Button = null;
        [SerializeField] private TMP_Text m_ButtonText;
        [SerializeField] private Button m_CloseButton = null;
        [SerializeField] private AdvisorButtonContainer m_AdvisorButtons = null;

        [SerializeField] private RectTransform m_Rect;

        [Space(5)]
        [Header("Policies")]
        [SerializeField] private RectTransform m_PolicyExpansionContainer;
        [SerializeField] private Graphic m_PolicyBackground;
        [SerializeField] private PolicySlot[] m_PolicySlots;
        [SerializeField] private Button m_PolicyCloseButton;

        [Space(5)]

        [Space(5)]
        [Header("Animation")]
        [SerializeField] private float m_OffscreenY = -270;
        [SerializeField] private float m_OnscreenY = 0;
        [SerializeField] private float m_OnscreenPolicyY = -20;
        [SerializeField] private float m_OnscreenPanelY = 0;
        [SerializeField] private float m_OffscreenPanelY = -270;

        #endregion // Inspector

        private Routine m_TransitionRoutine;
        private TagStringEventHandler m_LocalHandler;
        [NonSerialized] private ScriptCharacterDef m_CurrentDef;
        [NonSerialized] private bool m_FullyExpanded = false;
        [NonSerialized] private bool m_IsActive;
        [NonSerialized] public AdvisorType ForceAdvisorPolicies = AdvisorType.None;
        [NonSerialized] public bool ShowHand = false;
        [NonSerialized] public PolicyType CardsToShow;
        [NonSerialized] private bool m_PoliciesActive;

        private bool m_NodeExiting;

        private void Start() {
            m_PolicyExpansionContainer.gameObject.SetActive(false);

            //AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            //advisorState.AdvisorButtonClicked.Register(HandleAdvisorButtonClicked);
            ZavalaGame.Events.Register<AdvisorType>(GameEvents.AdvisorButtonClicked, HandleAdvisorButtonClicked);
            ZavalaGame.Events.Register<PolicyType>(GameEvents.PolicyButtonClicked, HandlePolicyButtonClicked);

            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            policyState.PolicyCloseButtonClicked.Register(HandlePolicyCloseClicked);
            m_CloseButton.onClick.AddListener(() => { 
                policyState.PolicyCloseButtonClicked?.Invoke();
                InputUtility.ConsumeButton(InputButton.PrimaryMouse);
            });

            m_LocalHandler = new TagStringEventHandler();
            m_LocalHandler.Register(LeafUtils.Events.Character, (d, o) => {
                LeafEvalContext evalContext = LeafEvalContext.FromObject(o);
                if (evalContext.Thread.Actor == null || m_CurrentDef == null || m_CurrentDef.IsAdvisor) {
                    Pin.Unpin();
                } else {
                    // only pin if inspector is not showing
                    if (((EventActor)evalContext.Thread.Actor).DisplayingPopup == null) {
                        Pin.PinTo(((EventActor)evalContext.Thread.Actor).transform);
                    }
                    else {
                        Pin.Unpin();
                    }
                }
            });
            m_LocalHandler.Register("ViewPolicies", () => {
               m_ButtonText.TryPopulate("View Policies");
            });

            m_Rect.SetAnchorPos(m_OffscreenY, Axis.Y);

            SimTimeUtility.OnPauseUpdated.Register(HandlePauseFlagsUpdated);

            m_NodeExiting = false;
        }

        private void OnDestroy() {
            m_TransitionRoutine.Stop();
        }

        #region Display

        public IEnumerator CompleteLine() {
            // Log.Msg("[DialogueBox] Line Complete");
            return WaitForInput();
        }

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (inString.RichText.Length > 0) {
                StringHash32 character = ScriptUtility.FindCharacterId(inString);
                ScriptCharacterDB charDB = Game.SharedState.Get<ScriptCharacterDB>();
                ScriptCharacterDef charDef = ScriptCharacterDBUtility.Get(charDB, character);
                string header, subheader;
                Sprite portraitImg;
                Texture2D panelBG = null;
                // Sprite portraitBG;
                Color boxColor, panelColor, highlightColor, nameColor, titleColor, textColor;
                bool isAdvisor;              
                if (charDef != null) {
                    header = charDef.NameId;
                    subheader = charDef.TitleId;
                    if (charDef == m_CurrentDef) { // same character as previous - just refresh text
                        DialogueUIUtility.PopulateBoxText(Contents, header, subheader, inString.RichText);
                    } else { // different character - refresh everything 
                        isAdvisor = charDef.IsAdvisor;
                        portraitImg = charDef.PortraitArt;
                        boxColor = charDef.BackgroundColor;
                        panelColor = charDef.PanelColor;
                        highlightColor = charDef.HighlightColor;
                        nameColor = charDef.NameColor;
                        titleColor = charDef.TitleColor;
                        textColor = charDef.TextColor;
                        if (isAdvisor) {
                            panelBG = charDef.PanelBackground;
                            foreach (PolicySlot slot in m_PolicySlots) {
                                slot.SetColors(charDef.IsEcon, panelColor);
                                m_PolicyBackground.color = panelColor;
                            }
                        }
                        if (m_PoliciesActive) CollapsePolicyUI(); // a new character with no policies will collapse policies
                        DialogueUIUtility.PopulateBoxText(Contents, m_Button.targetGraphic, header, subheader, inString.RichText, panelBG, portraitImg, !isAdvisor, boxColor, highlightColor, nameColor, titleColor, textColor);
                        m_CurrentDef = charDef;
                    }
                } else { // charDef is null
                    header = "Error";
                    subheader = "Null Character";
                    if (charDef == m_CurrentDef) {
                        DialogueUIUtility.PopulateBoxText(Contents, header, subheader, inString.RichText);
                    } else { // charDef is different
                        // portraitBG = null;
                        portraitImg = null;
                        boxColor = Color.red;
                        // panelColor = Color.white;
                        highlightColor = Color.blue;
                        nameColor = Color.green;
                        titleColor = Color.green;
                        textColor = Color.green;
                        isAdvisor = true;
                        DialogueUIUtility.PopulateBoxText(Contents, m_Button.targetGraphic, header, subheader, inString.RichText, portraitImg, !isAdvisor, boxColor, highlightColor, nameColor, titleColor, textColor);
                        m_CurrentDef = charDef;
                    }
                }
                Contents.Contents.maxVisibleCharacters = 0;
                ZavalaGame.Events.Dispatch(GameEvents.DialogueDisplayed, new Data.DialogueLineData(charDef.NameId, charDef.TitleId, inString.VisibleText));
                m_TransitionRoutine.Replace(this, ShowRoutine()).ExecuteWhileDisabled();
            } else {
                // no string to prepare...
                // m_TransitionRoutine.Replace(HideRoutine());
            }
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

        private void SetCutsceneMode(bool cutscene) {
            m_CloseButton.gameObject.SetActive(!cutscene);
            // m_PolicyCloseButton.gameObject.SetActive(!cutscene);
        }

        #endregion // Display

        #region Leaf Flag Interactions

        public void ForceExpandPolicyUI(AdvisorType aType, bool nodeExiting = false) {
            ForceAdvisorPolicies = aType;
            // collapse any current, will force expand when closed
            CollapsePolicyUI();
        }

        public void ExpandPolicyUI(AdvisorType advisorType) {
            // Load relevant policy slot types according to advisor type
            // TODO: room for more flexibility here, such as an adaptive number of slots
            if (advisorType == AdvisorType.None) return;
            m_PoliciesActive = true;
            PopulateSlotsForAdvisor(advisorType);
            HideCardsInstant();
            m_TransitionRoutine.Replace(this, ExpandPolicyUIRoutine()).ExecuteWhileDisabled();
        }

        public void CollapsePolicyUI() {
            HideCardsInstant();
            m_TransitionRoutine.Replace(this, CollapsePolicyUIRoutine()).ExecuteWhileDisabled();
        }

        private void PopulateSlotsForAdvisor(AdvisorType type) {

            PolicyType[] policyTypes = CardsUtility.AdvisorPolicyMap[type];
            for (int i = 0; i < policyTypes.Length; i++) {
                if (i == m_PolicySlots.Length) {
                    // Defined too many policies for the number of slots!
                    break;
                }
                m_PolicySlots[i].PopulateSlot(policyTypes[i]);
            }
        }

        public void HideDialogueUI() {
            if (ForceAdvisorPolicies != AdvisorType.None) return; // don't close until AdvisorPoliciesToShow has been set to none
            HideCardsInstant();
            // m_PoliciesActive = false;
            m_TransitionRoutine.Replace(HideRoutine());
        }

        public void ShowButton(bool show) {
            m_ButtonContainer.gameObject.SetActive(show);
        }

        private void HideCardsInstant() {
            foreach (PolicySlot slot in m_PolicySlots) {
                slot.InstantHideHand();
            }
        }

        public void MarkNodeEntered()
        {
            m_NodeExiting = false;
        }

        public void MarkNodeExited()
        {
            m_NodeExiting = true;
        }

        #endregion // Leaf Flag Interactions

        #region Routines

        private IEnumerator ShowRoutine() {
            m_IsActive = true;
            SimTimeInput.SetPaused(true, SimPauseFlags.DialogBox);
            this.gameObject.SetActive(true);
            m_AdvisorButtons.HideAdvisorButtons();
            m_Button.gameObject.SetActive(true);
            float targetY = ForceAdvisorPolicies == AdvisorType.None ? m_OnscreenY : m_OnscreenPolicyY;
            yield return m_Rect.AnchorPosTo(targetY, 0.3f, Axis.Y).Ease(Curve.CubeIn);
            ExpandPolicyUI(ForceAdvisorPolicies);
            yield return null;
        }

        private IEnumerator HideRoutine() {
            m_FullyExpanded = false;
            m_IsActive = false;
            SimTimeInput.SetPaused(false, SimPauseFlags.DialogBox);
            m_AdvisorButtons.ShowAdvisorButtons();
            if (m_PolicyExpansionContainer.gameObject.activeSelf) {
                //m_PoliciesActive = false;
                //m_PolicyCloseButton.gameObject.SetActive(false);
                //yield return m_PolicyExpansionContainer.AnchorPosTo(m_OffscreenPanelY, 0.1f, Axis.Y).Ease(Curve.CubeIn);
                //m_PolicyExpansionContainer.gameObject.SetActive(false);
                yield return CollapsePolicyUIRoutine();
            }

            CameraInputState camState = Game.SharedState.Get<CameraInputState>();
            camState.LockRegion = Tile.InvalidIndex16;

            yield return m_Rect.AnchorPosTo(m_OffscreenY, 0.3f, Axis.Y).Ease(Curve.CubeIn);
            m_CloseButton.gameObject.SetActive(true);
            this.gameObject.SetActive(false);
            Pin.Unpin();
            SimTimeInput.UnpauseEvent();
            // Game.Events.Dispatch(GameEvents.DialogueClosing);
            if (m_CurrentDef != null && m_CurrentDef.IsAdvisor) {
                ScriptUtility.Trigger(GameTriggers.AdvisorClosed);
            }
            yield return null;
        }

        private IEnumerator WaitForInput() {
            if (!m_ButtonContainer) {
                yield break;
            }
            //Log.Msg("[DialogueBox] Waiting for input...");

            InputState input = Game.SharedState.Get<InputState>();
            //m_ButtonContainer.gameObject.SetActive(true);

            while (ForceAdvisorPolicies != AdvisorType.None) {
                //Log.Msg("   > Advisor forced: {0}", ForceAdvisorPolicies);
                // don't check for click if policies are being opened
                yield return null;
            } 
            if (m_PoliciesActive) {
                while (!input.ButtonPressed(InputButton.PrimaryMouse) || Game.Input.IsPointerOverHierarchy(m_PolicyExpansionContainer)) {
                    //Log.Msg("   > Pointer over hierarchy: {0}", Game.Input.IsPointerOverHierarchy(m_PolicyExpansionContainer));
                    yield return null;
                }
                //Log.Msg("   > Policies active, but clicked!");
            } else {
                while (!input.ButtonPressed(InputButton.PrimaryMouse)) {
                    yield return null;
                }
            }
            input.ConsumedButtons |= InputButton.PrimaryMouse;
            ZavalaGame.Events.Dispatch(GameEvents.DialogueAdvanced);
            //Log.Msg("[DialogueBox] BREAK!");
            yield break;
        }

        private IEnumerator ExpandPolicyUIRoutine() {
            m_PoliciesActive = true;
            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            m_PolicyCloseButton.onClick.RemoveAllListeners();
            m_PolicyCloseButton.onClick.AddListener(() => { policyState.PolicyCloseButtonClicked?.Invoke(); });

            yield return m_Rect.AnchorPosTo(m_OnscreenPolicyY, 0.1f, Axis.Y).Ease(Curve.CubeIn);
            // yield return
            m_PolicyExpansionContainer.gameObject.SetActive(true);
            m_PolicyCloseButton.gameObject.SetActive(true);
            m_CloseButton.gameObject.SetActive(false);
            yield return m_PolicyExpansionContainer.AnchorPosTo(m_OnscreenPanelY, 0.2f, Axis.Y).Ease(Curve.CubeIn);
            // TODO: populate with default localized text? Or do we like having the last line spoken displayed?
            // "Here are the current policies you can put in place, you can modify them at any time."
            if (ShowHand) {      
                policyState.PolicySlotClicked?.Invoke(CardsToShow);
                ShowHand = false;
            }
            m_FullyExpanded = true;
            CameraInputState camState = Game.SharedState.Get<CameraInputState>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();
            camState.LockRegion = grid.CurrRegionIndex;

            SimWorldState world = Game.SharedState.Get<SimWorldState>();
            Bounds b = world.RegionBounds[camState.LockRegion];
            b.Expand(0.25f);

            Vector3 bMin = b.min, bMax = b.max;
            camState.LockedBounds = Rect.MinMaxRect(bMin.x, bMin.z, bMax.x, bMax.z);

            ForceAdvisorPolicies = AdvisorType.None;

            // Check if leaf has next line
            if (m_NodeExiting)
            {
                // if not, add manual next listener
                Debug.Log("[Close Next] Node exiting");
                m_Button.onClick.AddListener(OnManualNext);
            }
            else
            {
                Debug.Log("[Close Next] Node not exiting");
            }
            m_ButtonText.TryPopulate("Next");


            yield return null;
        }

        private IEnumerator CollapsePolicyUIRoutine() {
            if (m_PoliciesActive) {
                m_PolicyCloseButton.gameObject.SetActive(false);
                yield return m_PolicyExpansionContainer.AnchorPosTo(m_OffscreenPanelY, 0.1f, Axis.Y).Ease(Curve.CubeIn);
                m_PolicyExpansionContainer.gameObject.SetActive(false);
                m_PoliciesActive = false;
            }
            if (ForceAdvisorPolicies != AdvisorType.None) {
                ExpandPolicyUI(ForceAdvisorPolicies);
            }
            yield return null;
        }
        #endregion // Routines

        #region Handlers

        private void HandlePauseFlagsUpdated(SimPauseFlags pauseFlags) {
            SetCutsceneMode((pauseFlags & (SimPauseFlags.Scripted | SimPauseFlags.Cutscene)) != 0);
        }

        private void HandlePolicyCloseClicked() {
            m_TransitionRoutine.Replace(HideRoutine());
            Input.InteractionUtility.ReleaseDialogueInteraction();
        }

        private void HandleAdvisorButtonClicked(AdvisorType advisorType) {
            // TODO: should probably just disable advisor buttons when dialogue is showing

            // If dialogue has completed when advisor button is clicked, hide this
            if (m_FullyExpanded) {
                m_TransitionRoutine.Replace(HideRoutine());
            }
        }

        private void HandlePolicyButtonClicked(PolicyType pType) {
            if (m_FullyExpanded) {
                m_TransitionRoutine.Replace(HideRoutine());
            }
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            m_AdvisorButtons = FindAnyObjectByType<AdvisorButtonContainer>(FindObjectsInactive.Include);
            return null;
        }

        /// <summary>
        /// Click next button after force policy
        /// </summary>
        private void OnManualNext()
        {
            HideDialogueUI();
            m_Button.onClick.RemoveListener(OnManualNext);
        }

        #endregion // Handlers
    }
}