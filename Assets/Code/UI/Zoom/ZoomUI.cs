using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Input;
using Zavala.World;

namespace Zavala.UI {
    public class ZoomUI : SharedPanel, IScenePreload {

        [Range(0.5f, 4.0f)]
        [SerializeField] private float zoomPerClick;
        [SerializeField] private Button m_ZoomIn;
        [SerializeField] private Button m_ZoomOut;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            m_ZoomIn.onClick.AddListener(OnZoomInClicked);
            m_ZoomOut.onClick.AddListener(OnZoomOutClicked);
            return null;
        }

        private void OnZoomInClicked() {
            WorldCameraUtility.ZoomCamera(zoomPerClick, false);
        }

        private void OnZoomOutClicked() {
            WorldCameraUtility.ZoomCamera(-zoomPerClick, false);
        }

        private void SetScrollWheelDelta(float num) {
            Game.SharedState.Get<InputState>().ScrollWheel.y = num;
        }
    }
}