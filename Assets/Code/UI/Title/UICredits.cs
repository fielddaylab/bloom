using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using System;
using BeauUtil.Debugger;
using Zavala.Data;
using EasyAssetStreaming;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Zavala.UI {
    public struct CreditsBlockData
    {
        public string Header;
        public string Names;
    }

    public class UICredits : MonoBehaviour
    {
        private static string CREDITS_DELIM = "::";

        private static float TRANSITION_X = 1060;
        private static float TRANSITION_TIME = 0.5f;

        #region Inspector

        [SerializeField] private Button m_ReturnButton;
        [SerializeField] private RectTransform m_Rect;
        [SerializeField] private StreamingUGUITexture m_ArtImg;
        [SerializeField] private string[] m_ArtPaths;

        [Space(5)]
        [Header("Text")]
        [SerializeField] private TextAsset m_CreditsTextAsset;
        [SerializeField] private RectTransform m_TextContainer;
        [SerializeField] private LayoutGroup m_TextLayout;

        [SerializeField] private GameObject m_CreditsBlockPrefab;

        [Space(5)]
        [Header("Settings")]
        [SerializeField] private float m_ScrollTime;
        [SerializeField] private float m_ScrollTimeBuffer;

        private bool m_Scrolling;
        private float m_ScrollSpeed;
        private float m_ScrollTimer;

        private float m_RecycleImgTime;
        private float m_RecycleImgTimer;
        private int m_CurrArtIndex;
        private int m_ArtDirLength = "Assets/StreamingAssets/".Length;

        private Routine m_ConceptRoutine;

        private List<CreditsBlockData> m_CreditsBlockDatas;
        private List<CreditsBlock> m_CreditsBlocks;

        #endregion // Inspector

        private Routine m_TransitionRoutine;
        private bool m_Transitioning;

        #region Unity Callbacks

        private void OnEnable()
        {
            m_ReturnButton.onClick.AddListener(HandleReturnClicked);

            ParseCredits();
            CreditsBlocksToText();

            // Calculate scroll speed based on scroll time and distance to travel
            Assert.True(m_ScrollTime > 0);
            if (m_CreditsBlocks.Count > 0) {
                m_ScrollSpeed = (m_CreditsBlocks[0].transform.position.y - m_CreditsBlocks[m_CreditsBlocks.Count - 1].transform.position.y + m_TextContainer.rect.size.y) / (m_ScrollTime);
            }

            m_RecycleImgTime = m_RecycleImgTimer = m_ScrollTime / m_ArtPaths.Length;

            ResetLayout();
        }

        private void Update()
        {
            if (m_Scrolling && m_ScrollTimer > -m_ScrollTimeBuffer)
            {
                m_TextContainer.transform.position = new Vector3(
                    m_TextContainer.transform.position.x,
                    m_TextContainer.transform.position.y + m_ScrollSpeed * Time.deltaTime,
                    m_TextContainer.transform.position.z
                    );

                m_ScrollTimer -= Time.deltaTime;
                m_RecycleImgTimer -= Time.deltaTime;

                if (m_RecycleImgTimer <= 0)
                {
                    m_ConceptRoutine.Replace(AdvanceConceptArtRoutine());
                }
            }
        }

        #endregion // Unity Callbacks

        #region Button Handlers

        private void HandleReturnClicked()
        {
            ZavalaGame.Events.Dispatch(GameEvents.MainMenuInteraction, MenuInteractionType.CreditsExited);
            if (m_Transitioning) { return; }
            m_TransitionRoutine.Replace(this, ExitRoutine());
        }

        #endregion // Button Handlers

        #region External

        public void OpenPanel()
        {
            if (m_Transitioning) { return; }
            m_TransitionRoutine.Replace(this, EnterRoutine(TRANSITION_TIME));
        }

        public void OpenPanelImmediate()
        {
            if (m_Transitioning) { return; }
            m_TransitionRoutine.Replace(this, EnterRoutine(0));
        }

        #endregion // External

        #region Routines

        private IEnumerator EnterRoutine(float enterTime)
        {
            m_Transitioning = true;
            m_ScrollTimer = m_ScrollTime;
            if (m_CurrArtIndex != 0)
            {
                m_CurrArtIndex = 0;
                m_ArtImg.Path = m_ArtPaths[m_CurrArtIndex];
                m_ArtImg.Preload();
                while (m_ArtImg.IsLoading())
                {
                    yield return null;
                }
            }
            yield return m_Rect.AnchorPosTo(0, TRANSITION_TIME, Axis.X).Ease(Curve.CubeOut);
            m_Scrolling = true;
            m_Transitioning = false;
        }

        private IEnumerator ExitRoutine()
        {
            m_Transitioning = true;
            yield return m_Rect.AnchorPosTo(TRANSITION_X, TRANSITION_TIME, Axis.X).Ease(Curve.CubeOut);

            ResetLayout();

            if (m_CurrArtIndex != 0)
            {
                m_CurrArtIndex = 0;
                m_ArtImg.Path = m_ArtPaths[m_CurrArtIndex];
                m_ArtImg.Preload();
                while (m_ArtImg.IsLoading())
                {
                    yield return null;
                }
            }

            m_Transitioning = false;
            ZavalaGame.Events.Dispatch(GameEvents.CreditsExited);
        }

        private IEnumerator AdvanceConceptArtRoutine()
        {
            m_RecycleImgTimer = m_RecycleImgTime;

            if (m_CurrArtIndex == m_ArtPaths.Length) { yield break; }

            m_ArtImg.Path = m_ArtPaths[m_CurrArtIndex];
            m_ArtImg.Preload();
            while (m_ArtImg.IsLoading())
            {
                yield return null;
            }

            m_CurrArtIndex++;
        }

        #endregion // Routines

        private void ResetLayout()
        {
            // Text position
            m_TextContainer.transform.localPosition = new Vector3(
                m_TextContainer.transform.localPosition.x,
                0 - m_TextContainer.rect.size.y,
                m_TextContainer.transform.localPosition.z
                );

            m_Scrolling = false;

            // Image Sequence

        }

        #region Credits Parsing

        private void ParseCredits()
        {
            List<string> blocksToParse = TextIO.TextAssetToList(m_CreditsTextAsset, CREDITS_DELIM);
            m_CreditsBlockDatas = new List<CreditsBlockData>();

            for (int i = 0; i < blocksToParse.Count; i++)
            {
                // skip first space
                if (i == 0) { continue; }

                m_CreditsBlockDatas.Add(ParseCreditsBlock(blocksToParse[i]));
            }
        }

        private CreditsBlockData ParseCreditsBlock(string blocktoParse)
        {
            CreditsBlockData newBlock = new CreditsBlockData();

            // Trim leading delim
            int startIdx = blocktoParse.IndexOf(CREDITS_DELIM) + CREDITS_DELIM.Length;

            // Split at first newline
            int newlineIdx;
            
            int nIndex = blocktoParse.IndexOf("\n");
            int rIndex = blocktoParse.IndexOf("\r");

            newlineIdx = Math.Max(nIndex, rIndex);
            
            newBlock.Header = blocktoParse.Substring(startIdx, newlineIdx - startIdx).Trim();
            newBlock.Names = blocktoParse.Substring(newlineIdx + 1, blocktoParse.Length - newlineIdx - 1);

            return newBlock;
        }

        private void CreditsBlocksToText()
        {
            m_CreditsBlocks = new List<CreditsBlock>();

            for (int i = 0; i < m_CreditsBlockDatas.Count; i++)
            {
                var newTextBlock = Instantiate(m_CreditsBlockPrefab, m_TextContainer.transform).GetComponent<CreditsBlock>();
                newTextBlock.Header.SetText(m_CreditsBlockDatas[i].Header);
                newTextBlock.Names.SetText(m_CreditsBlockDatas[i].Names);

                m_CreditsBlocks.Add(newTextBlock);
            }

            m_TextLayout.ForceRebuild();
        }

        #endregion // Credits Parsing

#if UNITY_EDITOR

        #region Editor

        [ContextMenu("Refresh Concept Art Images")]
        private void RefreshImages()
        {
            string[] assetGuids = AssetDatabase.FindAssets("*", new string[] { "Assets/StreamingAssets/credits" });

            m_ArtPaths = new string[assetGuids.Length];
            int imgIndex = 0;
            foreach (var guid in assetGuids)
            {
                string fullPath = AssetDatabase.GUIDToAssetPath(guid);
                m_ArtPaths[imgIndex] = fullPath.Substring(m_ArtDirLength);
                imgIndex++;
            }
        }

        #endregion // Editor

#endif // UNITY_EDITOR
    }
}
