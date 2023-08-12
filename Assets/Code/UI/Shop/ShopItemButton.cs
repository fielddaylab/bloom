using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;

namespace Zavala.UI {
    public class ShopItemButton : MonoBehaviour
    {
        public Button Button;
        public int Cost;
        public UserBuildTool BuildTool;

        public TMP_Text CostText;
        public Image CostBG;
    }
}