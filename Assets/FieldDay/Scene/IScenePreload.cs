using System.Collections;
using BeauUtil;

namespace FieldDay.Scenes {
    /// <summary>
    /// Contains a scene preload callback.
    /// </summary>
    public interface IScenePreload {
        IEnumerator Preload(SceneBinding scene, object context);
    }
}