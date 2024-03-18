using System;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine;

namespace FieldDayDebugging {
    /// <summary>
    /// Simple framerate counter.
    /// </summary>
    [DefaultExecutionOrder(32000)]
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

        [Header("Framerate Drop Warning")]
        [SerializeField] private GameObject m_FramerateDropWarning;
        [SerializeField, Range(0, 3)] private float m_FramerateDropTolerance = 0;
        [SerializeField] private float m_FramerateDropWarningDuration = 3;

        #endregion // Inspector

        private StringBuilder m_TextBuilder = new StringBuilder(8);
        [NonSerialized] private long m_FrameAccumulation;
        [NonSerialized] private Color m_DefaultTextColor;
        [NonSerialized] private int m_FrameCount;
        [NonSerialized] private long m_LastTimestamp;
        [NonSerialized] private int m_FrameCooldown;

        [NonSerialized] private long m_WarningThreshold;
        [NonSerialized] private float m_WarningTimeLeft = 0;

        static private FramerateDisplay s_Instance;
        static private bool s_Initialized;

        #region Unity Events

        private void Awake() {
            if (s_Instance != null && s_Instance != this) {
                UnityEngine.Debug.LogWarning("[FramerateDisplay] Multiple instances of FramerateDisplay detected!");
            } else {
                s_Instance = this;
            }

            if (transform.parent == null) {
                DontDestroyOnLoad(gameObject);
            }

            if (m_FramerateDropWarning != null) {
                m_FramerateDropWarning.SetActive(false);
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

            m_WarningThreshold = (long) (Stopwatch.Frequency / (m_TargetFramerate - m_FramerateDropTolerance));
        }

        private void OnEnable() {
            m_TextDisplay.SetText("-.-");
            m_TextDisplay.color = m_DefaultTextColor;
            m_FrameCooldown = 2;
        }

        private void OnDisable() {
            m_FrameAccumulation = 0;
            m_FrameCount = 0;
            m_LastTimestamp = 0;
            m_WarningTimeLeft = 0;
            if (m_FramerateDropWarning != null) {
                m_FramerateDropWarning.SetActive(false);
            }
        }

        private void OnDestroy() {
            if (s_Instance == this) {
                s_Instance = null;
            }
        }

        private void OnApplicationPause(bool pause) {
            if (pause) {
                m_FrameAccumulation = 0;
                m_FrameCount = 0;
                m_LastTimestamp = 0;
            }
        }

        private void LateUpdate() {
            long timestamp = Stopwatch.GetTimestamp();

            if (m_FrameCooldown > 0) {
                m_FrameCooldown--;
                return;
            }

            if (m_LastTimestamp != 0) {
                long amt = timestamp - m_LastTimestamp;
                m_FrameAccumulation += amt;
                m_FrameCount++;
                if (m_FrameCount >= m_AveragingFrames) {
                    double framerate = m_FrameCount * (double)Stopwatch.Frequency / m_FrameAccumulation;
                    m_FrameAccumulation = 0;
                    m_FrameCount = 0;

                    m_TextBuilder.Clear().Append(framerate);
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

                if (m_FramerateDropWarning != null) {
                    if (amt > m_WarningThreshold) {
                        m_FramerateDropWarning.SetActive(true);
                        m_WarningTimeLeft = m_FramerateDropWarningDuration;
                    } else if (m_WarningTimeLeft > 0) {
                        m_WarningTimeLeft -= Time.unscaledDeltaTime;
                        if (m_WarningTimeLeft <= 0) {
                            m_FramerateDropWarning.gameObject.SetActive(false);
                        }
                    }
                }
            }
            m_LastTimestamp = timestamp;
        }

        #endregion // Unity Events

        #region Show/Hide

        static private FramerateDisplay GetInstance() {
            if (!s_Instance) {
                s_Instance = FindAnyObjectByType<FramerateDisplay>();
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

        /// <summary>
        /// Returns if the framerate counter is being displayed.
        /// </summary>
        static public bool IsShowing() {
            FramerateDisplay inst = GetInstance();
            if (inst) {
                return inst.gameObject.activeSelf;
            } else {
                return false;
            }
        }

        #endregion // Show/Hide
    }
}