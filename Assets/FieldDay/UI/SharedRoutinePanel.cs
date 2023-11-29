using BeauRoutine.Extensions;
using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.UI {
    /// <summary>
    /// Shared beauroutine panel.
    /// </summary>
    [DefaultExecutionOrder(SharedPanel.DefaultExecutionOrder)]
    public class SharedRoutinePanel : BasePanel, ISharedGuiPanel {

        protected override void Awake() {
            base.Awake();

            Game.Gui.Register(this);
        }

        protected virtual void OnDestroy() {
            if (!Game.IsShuttingDown) {
                Game.Gui.Deregister(this);
            }
        }

        #region ISharedGuiPanel

        public void Hide() {
            Hide(0);
        }

        public bool IsVisible() {
            return IsTransitioning() || IsShowing();
        }

        public void Show() {
            Show(0);
        }

        #endregion // ISharedGuiPanel
    }
}