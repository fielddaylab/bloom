using System;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FieldDay.UI {
    /// <summary>
    /// Selectable state change handler.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public abstract class SelectableStateAnimator : MonoBehaviour, IOnGuiUpdate {
        [SerializeField, HideInInspector] private Selectable m_Selectable;
        [NonSerialized] private SelectionState m_LastState = SelectionState.Disabled;

        #region Unity Events

        protected virtual void Awake() {
            this.CacheComponent(ref m_Selectable);
        }

        protected virtual void OnEnable() {
            Game.Gui.RegisterUpdate(this);
        }

        protected virtual void OnDisable() {
            m_LastState = SelectionState.Disabled;
            Game.Gui?.DeregisterUpdate(this);
        }

        #endregion // Unity Events

        #region Handlers
        public abstract void HandleStateChanged(SelectionState state);

        public virtual void OnGuiUpdate() {
            if (Ref.ReplaceEnum(ref m_LastState, m_Selectable.GetSelectionState())) {
                HandleStateChanged(m_LastState);
            }
        }

        #endregion // Handlers

        #region IBaked

#if UNITY_EDITOR

        protected virtual void Reset() {
            m_Selectable = GetComponent<Selectable>();
        }

        protected virtual void OnValidate() {
            this.CacheComponent(ref m_Selectable);
        }

        public int Order { get { return 100; } }

        public virtual bool Bake(BakeFlags flags, BakeContext context) {
            m_Selectable = GetComponent<Selectable>();
            return true;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}