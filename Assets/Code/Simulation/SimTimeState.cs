using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UIElements;
using BeauPools;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using BeauUtil.Editor;
#endif // UNITY_EDITOR

namespace Zavala.Sim {
    /// <summary>
    /// Shared simulation time state.
    /// </summary>
    public sealed class SimTimeState : SharedStateComponent {
        /// <summary>
        /// Time scale at which the simulation elements should animate and update.
        /// </summary>
        public float TimeScale = 1;

        /// <summary>
        /// What has paused the simulation.
        /// </summary>
        [AutoEnum] public SimPauseFlags Paused = 0;
        public Graphic PauseOverlay;
    }

    [Flags]
    public enum SimPauseFlags : uint {
        User = 0x01,
        Event = 0x02
    }

    /// <summary>
    /// Lightweight struct representing a timer bound to the simulation state.
    /// </summary>
    [Serializable]
    public struct SimTimer {
        public float Period;
        [NonSerialized] public float Accumulator;
        [NonSerialized] public uint PeriodIndex;
        [NonSerialized] public ushort AdvancedOnFrame;

        public SimTimer(float period) {
            Period = period;
            Accumulator = 0;
            PeriodIndex = 0;
            AdvancedOnFrame = Frame.InvalidIndex;
        }

        /// <summary>
        /// Advances this timer by a simulation-scaled delta time.l
        /// Returns if this timer advance to its next period.
        /// </summary>
        public bool Advance(float deltaTime, SimTimeState timeState) {
            if (SimTimeUtility.AdvanceCycle(ref Accumulator, Period, deltaTime, timeState)) {
                PeriodIndex++;
                AdvancedOnFrame = Frame.Index;
                return true;
            }

            AdvancedOnFrame = Frame.InvalidIndex;
            return false;
        }

        /// <summary>
        /// Has this timer advanced this frame?
        /// </summary>
        public bool HasAdvanced() {
            return AdvancedOnFrame == Frame.Index;
        }

        /// <summary>
        /// Clears the advanced flag.
        /// </summary>
        public void ClearAdvancedOnFrame() {
            AdvancedOnFrame = Frame.InvalidIndex;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SimTimer))]
        private class TimerPropertyDrawer : PropertyDrawer {
            private const float InfoDisplayWidth = 80;

            private GUIStyle m_InfoStyle;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                bool shouldDrawInfo = EditorApplication.isPlaying;
                bool isPainting = Event.current.type == EventType.Repaint;

                label = EditorGUI.BeginProperty(position, label, property);
                SerializedProperty parentProp = property.Copy();

                property.Next(true);
                Rect periodRect = position;
                if (shouldDrawInfo) {
                    periodRect.width -= InfoDisplayWidth;
                }

                EditorGUI.PropertyField(periodRect, property, label);

                if (shouldDrawInfo) {
                    if (m_InfoStyle == null) {
                        m_InfoStyle = new GUIStyle(EditorStyles.label);
                        m_InfoStyle.normal.textColor = Color.gray;
                        m_InfoStyle.alignment = TextAnchor.MiddleRight;
                    }

                    int lastIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    Rect infoRect = new Rect(periodRect.xMax + 4, periodRect.y, InfoDisplayWidth - 8, periodRect.height);
                    if (property.serializedObject.targetObjects.Length > 1) {
                        EditorGUI.LabelField(infoRect, "Multiple objects selected", m_InfoStyle);
                    } else {
                        if (isPainting) {
                            SimTimer timer = (SimTimer) parentProp.FindObject();
                            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                                psb.Builder.AppendNoAlloc(timer.Accumulator, 3);
                                psb.Builder.Append(" [").AppendNoAlloc(timer.PeriodIndex % 256).Append(']');
                                EditorGUI.LabelField(infoRect, psb.Builder.Flush(), m_InfoStyle);
                            }
                            EditorUtility.SetDirty(property.serializedObject.targetObject);
                        } else {
                            EditorGUI.LabelField(infoRect, " ", m_InfoStyle);
                        }
                    }

                    EditorGUI.indentLevel = lastIndent;
                }

                EditorGUI.EndProperty();
            }
        }
#endif // UNITY_EDITOR
    }

    /// <summary>
    /// Simulation time utility methods.
    /// </summary>
    static public class SimTimeUtility {
        /// <summary>
        /// Advances a timer by simulation-scaled delta time.
        /// Returns if the timer advanced to its next period.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool AdvanceCycle(ref float cycleTimer, float cyclePeriod, float deltaTime, SimTimeState timeState) {
            deltaTime *= timeState.TimeScale;
            if (timeState.Paused != 0 || deltaTime <= 0) {
                return false;
            }

            cycleTimer += deltaTime;
            if (cycleTimer > cyclePeriod) {
                cycleTimer -= cyclePeriod;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Advances a timer by simulation-scaled delta time.
        /// Returns if the timer advanced to its next period.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float AdjustedDeltaTime(float deltaTime, SimTimeState timeState) {
            if (timeState.Paused != 0) {
                return 0;
            }

            return deltaTime * timeState.TimeScale;
        }
    }
}