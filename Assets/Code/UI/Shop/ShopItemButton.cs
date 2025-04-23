using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using FieldDay.Animation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;

namespace Zavala.UI {
    public class ShopItemButton : MonoBehaviour, ILiteAnimator
    {
        public bool StartUnlocked;

        public LayoutOffset Offset;
        public Button Button;
        [HideInInspector] public int Cost;
        [HideInInspector] public bool CanAfford;
        public UserBuildTool BuildTool;

        public TMP_Text CostText;
        public Image CostBG;
        public Image ButtonBG;
        public Graphic CostFlash;

        public Color SelectedColor;
        public Color UnselectedColor;

        public bool UpdateAnimation(object _, ref LiteAnimatorState state, float deltaTime) {
            state.TimeRemaining = Mathf.Max(0, state.TimeRemaining - deltaTime);
            float amt = state.TimeRemaining / state.Duration;
            if (CanAfford) {
                Offset.Offset1 = new Vector2(0, -4 * amt);
                CostFlash.enabled = false;
            } else {
                Offset.Offset1 = new Vector2(Mathf.Cos(amt * Mathf.PI * 2 * 3) * 2 * amt, 0);
                CostFlash.SetAlpha(amt);
                CostFlash.enabled = amt > 0;
            }
            return state.TimeRemaining > 0;
        }

        public void ResetAnimation(object _, ref LiteAnimatorState state) {
            Offset.Offset1 = default;
            CostFlash.enabled = false;
        }
    }
}