#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using UnityEngine;

namespace Zavala {
    [DefaultExecutionOrder(-22900)]
    public class BootParams : MonoBehaviour
    {
        private bool m_HasPersisted = false;

        private void Awake()
        {
            m_HasPersisted = true;
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