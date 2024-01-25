using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeauRoutine.Extensions;
using Zavala.Cards;
using TMPro;
using BeauUtil;
using System.Text;
using System;
using BeauUtil.Debugger;

namespace Zavala.UI
{
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

        private List<CreditsBlockData> m_CreditsBlockDatas;
        private List<CreditsBlock> m_CreditsBlocks;

        #endregion // Inspector

        private Routine m_TransitionRoutine;

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
            }
        }

        #endregion // Unity Callbacks

        #region Button Handlers

        private void HandleReturnClicked()
        {
            m_TransitionRoutine.Replace(this, ExitRoutine());
        }

        #endregion // Button Handlers

        #region External

        public void OpenPanel()
        {
            m_TransitionRoutine.Replace(this, EnterRoutine());
        }

        #endregion // External

        #region Routines

        private IEnumerator EnterRoutine()
        {
            m_ScrollTimer = m_ScrollTime;
            yield return m_Rect.AnchorPosTo(0, TRANSITION_TIME, Axis.X).Ease(Curve.CubeOut);
            m_Scrolling = true;
        }

        private IEnumerator ExitRoutine()
        {
            yield return m_Rect.AnchorPosTo(TRANSITION_X, TRANSITION_TIME, Axis.X).Ease(Curve.CubeOut);

            ResetLayout();
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
    }
}
