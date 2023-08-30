using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using Zavala.Scripting;

namespace Zavala {
    public class AssetsService : ServiceBehaviour {
        #region Inspector

        /*
        [Header("Databases")]

        [Header("Fonts")]

        [Header("Shaders")]
        */

        [Header("Streaming")]
        [SerializeField, Range(1, 32)] private float m_StreamedTextureMem = 8;
        [SerializeField, Range(1, 32)] private float m_StreamedAudioMem = 8;

        [Header("Sprites")]
        [SerializeField, Required] private Sprite m_DefaultSquare = null;

        #endregion // Inspector

        private Unsafe.ArenaHandle m_DecompressionBuffer;


        protected override void Initialize() {
            base.Initialize();

            SharedCanvasResources.DefaultWhiteSprite = m_DefaultSquare;

            Streaming.TextureMemoryBudget = (long) (m_StreamedTextureMem * 1024 * 1024);
            Streaming.AudioMemoryBudget = (long) (m_StreamedAudioMem * 1024 * 1024);
            m_DecompressionBuffer = Unsafe.CreateArena(1024 * 1024 * 2, "Decompression");

            // Assets.Assign(this, m_DecompressionBuffer);
            Routine.Start(this, StreamingManagementRoutine());
        }

        private IEnumerator StreamingManagementRoutine() {
            object bigWait = 60f;
            object smallWait = 5f;

            while (true) {
                yield return bigWait;
                while (Streaming.IsUnloading()) {
                    yield return smallWait;
                }

                Streaming.UnloadUnusedAsync(60f);
                while(Streaming.IsUnloading()) {
                    yield return null;
                }
            }
        }

        protected override void Shutdown() {
            Streaming.UnloadAll();

            Unsafe.TryDestroyArena(ref m_DecompressionBuffer);

            base.Shutdown();
        }
    }
}