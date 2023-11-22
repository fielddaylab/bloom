using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Rendering
{
    public class MaterialSwap : MonoBehaviour
    {
        [SerializeField] private MeshRenderer m_Renderer;
        private Material m_OriginalMat;

        #region Unity Callbacks

        private void OnEnable()
        {
            if (m_Renderer) { m_OriginalMat = m_Renderer.sharedMaterial; }
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

            // Once put into place, never need this swap again
            Destroy(this);
        }

        #endregion // Handlers


    }
}