using BeauRoutine;
using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvisorButtonContainer : MonoBehaviour
{
    [SerializeField] private RectTransform m_Rect;
    private Routine m_TransitionRoutine;
    [NonSerialized] private bool m_Showing = true;


    public void HideAdvisorButtons() {
        if (Ref.Replace(ref m_Showing, false)) {
            m_TransitionRoutine.Replace(this, HideRoutine()).ExecuteWhileDisabled();
        }
    }
    public void ShowAdvisorButtons() {
        if (Ref.Replace(ref m_Showing, true)) {
            m_TransitionRoutine.Replace(this, ShowRoutine()).ExecuteWhileDisabled();
        }
    }

    private IEnumerator HideRoutine() {
        yield return m_Rect.AnchorPosTo(-100, 0.3f, Axis.Y).Ease(Curve.CubeIn);
         
        this.gameObject.SetActive(false);
        yield return null;
    }

    private IEnumerator ShowRoutine() {
        this.gameObject.SetActive(true);
        yield return m_Rect.AnchorPosTo(-20, 0.3f, Axis.Y).Ease(Curve.CubeIn);
        yield return null;
    }

}
