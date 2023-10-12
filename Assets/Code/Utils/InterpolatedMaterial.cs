using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using UnityEngine;

namespace Zavala {
    [CreateAssetMenu(menuName = "Zavala/Interpolated Material")]
    public class InterpolatedMaterial : ScriptableObject {
        [EditModeOnly] public Material Start;
        [EditModeOnly] public Material End;
        [EditModeOnly] public int Increments = 32;

        [NonSerialized] private Material[] m_Range;
        [NonSerialized] private AsyncHandle m_LoadHandle;

        public void Load() {
            if (m_Range == null) {
                if (!Start || !End) {
                    return;
                }
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (!Frame.IsActive(this)) {
                        return;
                    }
                    m_Range = Generate(Start, End, Increments);
                    return;
                }
#endif // UNITY_EDITOR
                m_LoadHandle.Cancel();
                m_Range = GenerateAsync(Start, End, Increments, out m_LoadHandle);
                m_LoadHandle.OnStop(() => m_LoadHandle = default);
            }
        }

        public bool IsLoaded() {
            return m_Range != null && !m_LoadHandle.IsRunning();
        }

        public Material Find(float t) {
            Assert.NotNull(m_Range, "InterpolatedMaterial has not yet been loaded");
            if (m_LoadHandle.IsRunning()) {
                Log.Warn("[InterpolatedMaterial] Material interpolation range not fully generated");
            }
            return m_Range[(int) ((Mathf.Clamp01(t) * Increments) + 0.5f)];
        }

        private void OnEnable() {
            Load();
        }

        private void OnDestroy() {
            m_LoadHandle.Cancel();
            if (m_Range != null) {
                for(int i = 1; i < m_Range.Length - 1; i++) {
                    Material.DestroyImmediate(m_Range[i]);
                }
                m_Range = null;
            }
        }

#if UNITY_EDITOR

        [ContextMenu("Force Reload")]
        private void EditorReload() {
            OnDestroy();
            Load();
        }

        private void OnValidate() {
            if (Increments < 8) {
                Increments = 8;
            }
        }

#endif // UNITY_EDITOR

        #region Generation

        static private Material[] GenerateAsync(Material start, Material end, int increments, out AsyncHandle loadHandle) {
            Material[] fill = new Material[increments + 1];
            for(int i = 0; i < increments; i++) {
                fill[i] = new Material(start);
            }
            fill[increments] = end;
            loadHandle = Async.Schedule(GenerateAsyncRoutine(start, end, fill), AsyncFlags.MainThreadOnly);
            return fill;
        }

        static private Material[] Generate(Material start, Material end, int increments) {
            Material[] fill = new Material[increments + 1];
            fill[0] = start;
            for (int i = 1; i < increments; i++) {
                (fill[i] = new Material(start)).Lerp(start, end, (float) i / increments);
            }
            fill[increments] = end;
            return fill;
        }

        static private IEnumerator GenerateAsyncRoutine(Material start, Material end, Material[] fill) {
            int increments = fill.Length - 1;
            for(int i = 1; i < increments; i++) {
                fill[i].Lerp(start, end, (float) i / increments);
                yield return null;
            }
        }

        #endregion // Generation
    }
}