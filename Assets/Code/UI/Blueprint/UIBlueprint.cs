using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.UI
{
    public class UIBlueprint : MonoBehaviour, IScenePreload
    {
        #region Inspector

        [SerializeField] private ShopToggleButton m_ShopToggle;
        [SerializeField] private CanvasGroup m_ReceiptGroup;

        [Header("Top Bar")]
        [SerializeField] private RectGraphic m_TopBarBG;
        [SerializeField] private Color m_TBDefault;
        [SerializeField] private Color m_TBBlueprint;

        private Routine m_TopBarRoutine;

        #endregion // Inspector

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            Game.Events.Register(GameEvents.BlueprintModeStarted, HandleStartBlueprintMode);
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleEndBlueprintMode);

            m_ReceiptGroup.alpha = 0;
            return null;
        }

        #region Handlers

        private void HandleStartBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, AppearanceTransition(true));
        }

        private void HandleEndBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, AppearanceTransition(false));
        }

        #endregion // Handlers

        #region Routines

        private IEnumerator AppearanceTransition(bool inBMode)
        {
            if (inBMode)
            {
                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBBlueprint, 0.1f),
                    m_ReceiptGroup.FadeTo(1, .1f)
                    );
            }
            else
            {
                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBDefault, 0.1f),
                    m_ReceiptGroup.FadeTo(0, .1f)
                    );
            }
        }

        #endregion // Routines
    }
}