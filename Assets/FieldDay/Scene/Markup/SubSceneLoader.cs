using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Scenes {
    /// <summary>
    /// Loads another scene upon scene load.
    /// </summary>
    public sealed class SubSceneLoader : MonoBehaviour {
        public SceneReference Scene;
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IRuntimeSubSceneLoader {
        IEnumerable<SceneReference> GetSubscenes(SceneReference scene, object context);
    }
}