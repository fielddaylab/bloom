using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine;

namespace FieldDay.Debugging {
    /// <summary>
    /// Simple framerate counter.
    /// </summary>
    [DefaultExecutionOrder(-50000)]
    [RequireComponent(typeof(RectTransform))]
    public class FramerateDisplay : MonoBehaviour {
        #region Inspector

        [Header("Layout")]
        [SerializeField] private TMP_Text m_TextDisplay;
        [SerializeField] private Vector2 m_BuildOffset;
        [SerializeField] private Color m_CriticalTextColor = Color.red;
        [SerializeField] private Color m_WarningTextColor = Color.yellow;
        [SerializeField] private bool m_ForceEnabled = false;

        [Header("Framerate")]
        [SerializeField] private int m_TargetFramerate = 60;
        [SerializeField] private int m_AveragingFrames = 8;

        #endregion // Inspector

        private StringBuilder m_TextBuilder = new StringBuilder(8);
        [NonSerialized] private long m_FrameAccumulation;
        [NonSerialized] private Color m_DefaultTextColor;
        [NonSerialized] private int m_FrameCount;
        [NonSerialized] private long m_LastTimestamp;

        static private FramerateDisplay s_Instance;
        static private bool s_Initialized;

        #region Unity Events

        private void Awake() {
            if (s_Instance != null && s_Instance != this) {
                Log.Warn("[FramerateDisplay] Multiple instances of FramerateDisplay detected!");
            } else {
                s_Instance = this;
            }

            if (transform.parent == null) {
                DontDestroyOnLoad(gameObject);
            }

            m_DefaultTextColor = m_TextDisplay.color;
        }

        private void Start() {
            if (!s_Initialized && !m_ForceEnabled && !Application.isEditor && !UnityEngine.Debug.isDebugBuild) {
                gameObject.SetActive(false);
            }

            if (!Application.isEditor) {
                GetComponent<RectTransform>().anchoredPosition += m_BuildOffset;
            }
        }

        private void OnEnable() {
            m_TextDisplay.SetText("-.-");
            m_TextDisplay.color = m_DefaultTextColor;
        }

        private void OnDisable() {
            m_FrameAccumulation = 0;
            m_FrameCount = 0;
        }

        private void OnDestroy() {
            if (s_Instance == this) {
                s_Instance = null;
            }
        }

        private void LateUpdate() {
            long timestamp = Stopwatch.GetTimestamp();
            if (m_LastTimestamp != 0) {
                m_FrameAccumulation += timestamp - m_LastTimestamp;
                m_FrameCount++;
                if (m_FrameCount >= m_AveragingFrames) {
                    double framerate = m_FrameCount * (double)Stopwatch.Frequency / m_FrameAccumulation;
                    m_FrameAccumulation = 0;
                    m_FrameCount = 0;

                    m_TextBuilder.Clear().AppendNoAlloc(framerate, 1);
                    m_TextDisplay.SetText(m_TextBuilder);

                    double framerateFraction = framerate / m_TargetFramerate;
                    if (framerateFraction <= 0.5) {
                        m_TextDisplay.color = m_CriticalTextColor;
                    } else if (framerateFraction <= 0.8) {
                        m_TextDisplay.color = m_WarningTextColor;
                    } else {
                        m_TextDisplay.color = m_DefaultTextColor;
                    }
                }
            }
            m_LastTimestamp = timestamp;
        }

        #endregion // Unity Events

        #region Show/Hide

        static private FramerateDisplay GetInstance() {
            if (!s_Instance) {
                s_Instance = FindObjectOfType<FramerateDisplay>();
            }
            return s_Instance;
        }

        /// <summary>
        /// Shows the current framerate counter.
        /// </summary>
        static public void Show() {
            s_Initialized = true;
            FramerateDisplay inst = GetInstance();
            if (inst) {
                inst.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the current framerate counter.
        /// </summary>
        static public void Hide() {
            s_Initialized = true;
            FramerateDisplay inst = GetInstance();
            if (inst) {
                inst.gameObject.SetActive(false);
            }
        }

        #endregion // Show/Hide
    }
}