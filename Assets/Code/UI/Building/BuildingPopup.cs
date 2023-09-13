using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    /// <summary>
    /// Singleton for showing and handling the building context menu.
    /// Based on https://www.youtube.com/watch?v=SzQABx2YTJA
    /// </summary>
    public class BuildingPopup : MonoBehaviour {
        public static BuildingPopup instance;

        [SerializeField] private TextMeshProUGUI m_Title;
        [SerializeField] private Image m_Icon;
        [SerializeField] private TextMeshProUGUI m_Description;
        [SerializeField] private Button m_Button1;
        [SerializeField] private TextMeshProUGUI m_Button1Text;
        private Action m_Button1Action;
        [SerializeField] private Button m_Button2;
        [SerializeField] private TextMeshProUGUI m_Button2Text;
        private Action m_Button2Action;
        [SerializeField] private Button m_CloseButton;

        private void Awake() {
            instance = this;
            CloseMenu();
        }

        public void PressButton1() {
            m_Button1Action?.Invoke();
            CloseMenu();
        }

        public void PressButton2() {
            m_Button2Action?.Invoke();
            CloseMenu();
        }

        public void CloseMenu() {
            // TODO: put animations here later
            this.gameObject.SetActive(false);
        }

        public void OpenMenu() {
            // TODO: put animations here later
            this.gameObject.SetActive(true);
        }

        public void ShowMenu(Vector3 pos, string title, Sprite icon, string description, string button1Text, Action button1Action, string button2Text, Action button2Action) {
            transform.position = pos;
            m_Title.text = title;
            m_Description.text = description;

            if (icon == null) {
                m_Icon.gameObject.SetActive(false);
            } else {
                m_Icon.sprite = icon;
            }

            m_Button1Text.text = button1Text;
            if (button1Action != null) {
                m_Button1Action = button1Action;
            }
            m_Button2Text.text = button2Text;
            if (button2Action != null) {
                m_Button2Action = button2Action;
            }
            
            OpenMenu();
        }

        public void ShowDestroyMenu(Vector3 pos, string title, Sprite icon, string description, Action button1Action, Action button2Action) {
            ShowMenu(pos, title, icon, description, "Destroy", button1Action, "Nevermind", button2Action);
        }

    }
}

