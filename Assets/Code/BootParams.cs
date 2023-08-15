#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using System.Globalization;
using BeauUtil;
using Zavala.Debugging;
using UnityEngine;
using System.Runtime.CompilerServices;
using BeauUtil.Variants;
using System;
using BeauUtil.Debugger;
using System.Collections.Generic;
using BeauRoutine;
using System.Text;

namespace Zavala
{
    [DefaultExecutionOrder(int.MinValue)]
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