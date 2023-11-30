using BeauUtil;

namespace FieldDay.UI {
    /// <summary>
    /// Interface panel.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface IGuiPanel {
        void Show();
        void Hide();

        bool IsShowing();
        bool IsTransitioning();
        bool IsVisible();
    }

    /// <summary>
    /// Singleton interface panel.
    /// </summary>
    public interface ISharedGuiPanel : IGuiPanel {  }
}