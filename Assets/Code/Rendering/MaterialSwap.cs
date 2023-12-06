using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Rendering
{
    public class MaterialSwap : BatchedComponent
    {
        [SerializeField] private MeshRenderer m_Renderer;
        private Material m_OriginalMat;

        #region Unity Callbacks

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_Renderer && !m_OriginalMat) { m_OriginalMat = m_Renderer.sharedMaterial; }

            Game.Events.Deregister(GameEvents.BlueprintModeEnded, HandleBlueprintModeEnded);
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleBlueprintModeEnded);
        }

        #endregion // Unity Callbacks

        public void SetMaterial(Material newMat)
        {
            m_Renderer.sharedMaterial = newMat;
        }

        public void ResetMaterial()
        {
            m_Renderer.sharedMaterial = m_OriginalMat;
        }

        #region Handlers

        private void HandleBlueprintModeEnded()
        {
            ResetMaterial();
        }


        #endregion // Handlers


    }
}