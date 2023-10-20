#if UNITY_2019_1_OR_NEWER
#define USE_SRP
#endif // UNITY_2019_1_OR_NEWER

using BeauUtil;
using UnityEngine;
using System;

#if USE_SRP
using UnityEngine.Rendering.Universal;
using UnityEditor;
#endif // USE_SRP

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    [RequireComponent(typeof(Camera)), ExecuteAlways]
    public sealed class AutoAttachOverlayCameras : MonoBehaviour {
        [SerializeField, UnityTag] private string[] m_Tags;
        [NonSerialized] private readonly RingBuffer<Camera> m_CachedAddedCameras = new RingBuffer<Camera>();

        private void OnEnable() {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;

            if (!Application.IsPlaying(this) && EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) {
                return;
            }
#endif // UNITY_EDITOR

            Camera c = GetComponent<Camera>();
            var data = c.GetUniversalAdditionalCameraData();
            var stack = data.cameraStack;

            foreach(var tag in m_Tags) {
                GameObject foundGO = GameObject.FindGameObjectWithTag(tag);
                if (foundGO != null && foundGO.TryGetComponent(out Camera cam) && cam.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay) {
                    if (!stack.Contains(cam)) {
                        stack.Add(cam);
                    }
                    m_CachedAddedCameras.PushBack(cam);
                }
            }
        }

        private void OnDisable() {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;
#endif // UNITY_EDITOR

#if USE_SRP
            Camera c = GetComponent<Camera>();
            var data = c.GetUniversalAdditionalCameraData();
            var stack = data.cameraStack;

            while (m_CachedAddedCameras.TryPopBack(out var overlay)) {
                stack.Remove(overlay);
            }
#endif // USE_SRP
        }
    }
}