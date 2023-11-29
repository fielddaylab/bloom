using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;

namespace Zavala.UI {
    public class ShopItemButton : MonoBehaviour
    {
        public bool StartUnlocked;

        public Button Button;
        [HideInInspector] public int Cost;
        [HideInInspector] public bool CanAfford;
        public UserBuildTool BuildTool;

        public TMP_Text CostText;
        public Image CostBG;
        public Image ButtonBG;

        public Color SelectedColor;
        public Color UnselectedColor;
    }
}