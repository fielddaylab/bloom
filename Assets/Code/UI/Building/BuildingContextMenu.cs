using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class BuildingContextMenu : MonoBehaviour {

        [SerializeField] private TextMeshProUGUI Title;
        [SerializeField] private Image Icon;
        [SerializeField] private TextMeshProUGUI Description;
        [SerializeField] private Button Button1;
        private Action Button1Action;
        [SerializeField] private Button Button2;
        private Action Button2Action;

        private void Awake() {
            Close();
        }

        public void PressButton1() {
            Button1Action?.Invoke();
            Close();
        }

        public void PressButton2() {
            Button2Action?.Invoke();
            Close();
        }

        public void Close() {
            this.gameObject.SetActive(false);
        }

        public void ShowMenu(string title, Sprite image, string description, Action Button1Action, Action Button2Action) {
            
        }

    }
}

