#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using UnityEngine;

namespace Zavala {
    [DefaultExecutionOrder(-23500)]
    public class BootParams : MonoBehaviour
    {
        private bool m_HasPersisted = false;

        private void Awake()
        {
            Services.AutoSetup(gameObject);
        }

        private void OnDestroy()
        {
            if (m_HasPersisted)
            {
                Services.Shutdown();
            }
        }
    }
}