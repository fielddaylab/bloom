using System.Diagnostics;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FieldDay.HID {
    public sealed class InputMgr {
        public const float DefaultDoubleClickBuffer = 0.8f;

        private struct InputTimestamp {
            public ushort FrameIndex;
            public long Ticks;

            static internal InputTimestamp Now() {
                return new InputTimestamp() {
                    FrameIndex = Frame.Index,
                    Ticks = Frame.Timestamp()
                };
            }
        }

        private EventSystem m_EventSystem;
        private ExposedPointerInputModule m_ExposedInputModule;
        private uint m_ForceClickRecursion;
        private RingBuffer<InputTimestamp> m_ClickTimestampBuffer = new RingBuffer<InputTimestamp>(2, RingBufferMode.Overwrite);

        internal InputMgr() { }

        #region Clicks

        /// <summary>
        /// Attempts to execute click handlers on the given object.
        /// </summary>
        public bool ExecuteClick(GameObject root) {
            Assert.NotNull(root);

            RectTransform rect = root.transform as RectTransform;
            if (rect && !rect.IsPointerInteractable()) {
                return false;
            }

            return ForceClick(root);
        }

        /// <summary>
        /// Forces the execution of click handlers on the given object.
        /// </summary>
        public bool ForceClick(GameObject root) {
            Assert.NotNull(m_ExposedInputModule);

            m_ForceClickRecursion++;
            bool success = ExecuteEvents.Execute(root, m_ExposedInputModule.GetPointerEventData(), ExecuteEvents.pointerClickHandler);
            m_ForceClickRecursion--;
            return success;
        }

        /// <summary>
        /// Returns if a forced input is being executed.
        /// </summary>
        public bool IsForcingInput() {
            return m_ForceClickRecursion > 0;
        }

        /// <summary>
        /// Returns if a double click has occured recently.
        /// </summary>
        public bool DoubleClick(float buffer = DefaultDoubleClickBuffer) {
            if (m_ClickTimestampBuffer.Count < 2) {
                return false;
            }

            long timeSince = m_ClickTimestampBuffer[0].Ticks - m_ClickTimestampBuffer[1].Ticks;
            long bufferTicks = (long) (buffer * Stopwatch.Frequency);
            return m_ClickTimestampBuffer[0].FrameIndex == Frame.Index && timeSince <= bufferTicks;
        }

        #endregion // Clicks

        #region Raycasts

        /// <summary>
        /// Returns the object the pointer is currently over.
        /// </summary>
        public GameObject CurrentPointerOver() {
            return m_ExposedInputModule ? m_ExposedInputModule.CurrentPointerOver() : m_EventSystem.currentSelectedGameObject;
        }

        /// <summary>
        /// Returns if the pointer is over a canvas.
        /// </summary>
        public bool IsPointerOverCanvas() {
            if (m_ExposedInputModule != null) {
                return m_ExposedInputModule.IsPointerOverCanvas();
            } else {
                return m_EventSystem.IsPointerOverGameObject();
            }
        }

        /// <summary>
        /// Returns if the pointer is over a given hierarchy.
        /// </summary>
        public bool IsPointerOverHierarchy(Transform root) {
            if (root == null) {
                return false;
            }
            GameObject over = CurrentPointerOver();
            return over != null && over.transform.IsChildOf(root);
        }

        /// <summary>
        /// Returns if the pointer is over a given layer.
        /// </summary>
        public bool IsPointerOverLayer(LayerMask layerMask) {
            GameObject over = CurrentPointerOver();
            return over != null && (layerMask & (1 << over.layer)) != 0;
        }

        #endregion // Raycasts

        #region Events

        internal void Initialize() {
            m_EventSystem = EventSystem.current;
            m_ExposedInputModule = m_EventSystem.currentInputModule as ExposedPointerInputModule;

            if (!m_ExposedInputModule) {
                Log.Warn("[InputMgr] Could not find ExposedInputInputModule");
            }
        }

        internal void UpdateDoubleClickBuffer() {
            if (Input.GetMouseButtonDown(0)) {
                m_ClickTimestampBuffer.PushFront(InputTimestamp.Now());
            }
        }

        internal void Shutdown() {
            m_EventSystem = null;
            m_ExposedInputModule = null;
        }

        #endregion // Events
    }
}