using System;
using UnityEditor;
using UnityEngine;

namespace Zavala.Editor {
    public class RegionEditor : EditorWindow {
        public RegionAsset EditingAsset;

        [SerializeField] private Vector2 m_ScrollAmount;
        [SerializeField] private float m_ScrollZoom;

        #region Unity Events

        private void OnEnable() {
            
        }

        private void OnDestroy() {
            
        }

        private void OnGUI() {
            
        }

        #endregion // Unity Events

        #region Menu

        [MenuItem("Zavala/Region Editor")]
        static private void Create() {
            var window = EditorWindow.GetWindow<RegionEditor>();
            window.Show();
        }

        #endregion // Menu
    }
}