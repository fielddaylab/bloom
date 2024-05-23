using BeauRoutine;
using BeauUtil;
using FieldDay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;
using Zavala.UI.Info;

namespace Zavala.UI
{
    public class ProductionEfficiencyGroup : MonoBehaviour
    {
        public GameObject Root;
        public RectTransform BGRect;
        public TMP_Text EfficiencyText;
        public Image InputIcon;
        public Image OutputIcon1;
        public GameObject AndIcon;
        public Image OutputIcon2;
        public Image OutputIconMoney;
        public TMP_Text OutputMoneyText;

        public Image[] EfficiencyIcons;

        [Space(5)]
        public Sprite ManureSprite;
        public Sprite MFertilizerSprite, DFertilizerSprite, GrainSprite, MilkSprite;
    }

    public static class ProductionEfficiencyUtility
    {
        /// <summary>
        /// Sets up to 1 input
        /// </summary>
        /// <param name="group"></param>
        /// <param name="input"></param>
        public static void SetInput(ProductionEfficiencyGroup group, ResourceMask input)
        {
            group.InputIcon.sprite = null;

            if ((input & ResourceMask.Manure) != 0)
            {
                group.InputIcon.sprite = group.ManureSprite;
            }
            else if ((input & ResourceMask.MFertilizer) != 0)
            {
                group.InputIcon.sprite = group.MFertilizerSprite;
            }
            else if ((input & ResourceMask.DFertilizer) != 0)
            {
                group.InputIcon.sprite = group.DFertilizerSprite;
            }
            else if ((input & ResourceMask.Grain) != 0)
            {
                group.InputIcon.sprite = group.GrainSprite;
            }
            else if ((input & ResourceMask.Milk) != 0)
            {
                group.InputIcon.sprite = group.MilkSprite;
            }
        }

        /// <summary>
        /// Sets up to 2 outputs
        /// </summary>
        /// <param name="group"></param>
        /// <param name="output"></param>
        public static void SetOutput(ProductionEfficiencyGroup group, ResourceMask output, int amt)
        {
            // Resource Outputs
            group.OutputIcon1.gameObject.SetActive(false);
            group.OutputIcon2.gameObject.SetActive(false);
            group.AndIcon.SetActive(false);

            int numOutputs = 0;
            Image nextToSet = group.OutputIcon1;

            if ((output & ResourceMask.Manure) != 0)
            {
                if (numOutputs > 0)
                {
                    nextToSet = group.OutputIcon2;
                    group.AndIcon.SetActive(true);
                }
                nextToSet.sprite = group.ManureSprite;
                nextToSet.gameObject.SetActive(true);
                numOutputs++;
            }
            if ((output & ResourceMask.MFertilizer) != 0)
            {
                if (numOutputs > 0)
                {
                    nextToSet = group.OutputIcon2;
                    group.AndIcon.SetActive(true);
                }
                nextToSet.sprite = group.MFertilizerSprite;
                nextToSet.gameObject.SetActive(true);
                numOutputs++;
            }
            if ((output & ResourceMask.DFertilizer) != 0)
            {
                if (numOutputs > 0)
                {
                    nextToSet = group.OutputIcon2;
                    group.AndIcon.SetActive(true);
                }
                nextToSet.sprite = group.DFertilizerSprite;
                nextToSet.gameObject.SetActive(true);
                numOutputs++;
            }
            if ((output & ResourceMask.Grain) != 0)
            {
                if (numOutputs > 0)
                {
                    nextToSet = group.OutputIcon2;
                    group.AndIcon.SetActive(true);
                }
                nextToSet.sprite = group.GrainSprite;
                nextToSet.gameObject.SetActive(true);
                numOutputs++;
            }
            if ((output & ResourceMask.Milk) != 0)
            {
                if (numOutputs > 0)
                {
                    nextToSet = group.OutputIcon2;
                    group.AndIcon.SetActive(true);
                }
                nextToSet.sprite = group.MilkSprite;
                nextToSet.gameObject.SetActive(true);
                numOutputs++;
            }

            // Money outputs
            group.OutputIconMoney.gameObject.SetActive(false);
            group.OutputMoneyText.gameObject.SetActive(false);

            if (amt != 0)
            {
                group.OutputIconMoney.gameObject.SetActive(true);

                group.OutputMoneyText.SetText(amt.ToStringLookup());
            }
        }

        /// <summary>
        /// set the efficiecny to a level between 0 and 2, inclusive
        /// </summary>
        /// <param name="group"></param>
        /// <param name="level"></param>
        public static void SetEfficiencyLevel(ProductionEfficiencyGroup group, OperationState opState, bool pairedWithText)
        {
            int level = (int)opState;
            for (int i = 0; i < group.EfficiencyIcons.Length; i++)
            {
                if (i <= level)
                {
                    group.EfficiencyIcons[i].SetAlpha(1);
                }
                else
                {
                    group.EfficiencyIcons[i].SetAlpha(0.5f);

                }
            }

            group.EfficiencyText.gameObject.SetActive(pairedWithText);
            if (pairedWithText)
            {
                group.BGRect.sizeDelta = new Vector2(290, group.BGRect.sizeDelta.y);
            }
            else
            {
                group.BGRect.sizeDelta = new Vector2(112, group.BGRect.sizeDelta.y);
            }
        }

        public static void SetEfficiencyLevelAndText(ProductionEfficiencyGroup group, OperationState opState, LocationDescription location)
        {
            StringBuilder builder = new StringBuilder();
            ScriptCharacterDB charDB = Game.SharedState.Get<ScriptCharacterDB>();
            ScriptCharacterDef charDef = ScriptCharacterDBUtility.Get(charDB, location.CharacterId);
            builder.Append(charDef.NameId);
            builder.Append(" is producing <i>");
            if (opState == OperationState.Bad) { builder.Append("slowly"); }
            if (opState == OperationState.Okay) { builder.Append("decently"); }
            if (opState == OperationState.Great) { builder.Append("quickly"); }
            builder.Append("</i>");

            group.EfficiencyText.SetText(builder);
            SetEfficiencyLevel(group, opState, true);
        }
    }
}