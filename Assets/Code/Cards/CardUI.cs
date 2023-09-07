using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.Cards {
    public class CardUI : MonoBehaviour
    {
        [HideInInspector] public int PolicyIndex; // Which severity index this card corresponds to (also index from left to right)
        public Button Button;

        private void OnDisable() {
            Button.onClick.RemoveAllListeners();
        }
    }
}
