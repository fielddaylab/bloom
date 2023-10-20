using BeauUtil;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace FieldDay.UI {
    [RequireComponent(typeof(Canvas)), ExecuteAlways]
    public class AutoAttachToCamera : MonoBehaviour {
        private void OnEnable() {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;
#endif // UNITY_EDITOR

            if (TransformHelper.TryGetCameraFromLayer(transform, out Camera camera)) {
                var c = GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
            }
        }

        private void OnDisable() {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;
#endif // UNITY_EDITOR

            GetComponent<Canvas>().worldCamera = null;
        }
    }
}