using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvisorButtonContainer : MonoBehaviour
{
    [SerializeField] private RectTransform m_Rect;
    private Routine m_TransitionRoutine;
    private bool m_FullyExpanded;


    public void HideAdvisorButtons() {
        m_TransitionRoutine.Replace(HideRoutine());
    }
    public void ShowAdvisorButtons() {
        m_TransitionRoutine.Replace(ShowRoutine());
    }

    private IEnumerator HideRoutine() {
        m_FullyExpanded = false;
        yield return m_Rect.AnchorPosTo(-100, 0.3f, Axis.Y).Ease(Curve.CubeIn);
         
        this.gameObject.SetActive(false);
        yield return null;
    }

    private IEnumerator ShowRoutine() {
        this.gameObject.SetActive(true);
        yield return m_Rect.AnchorPosTo(-20, 0.3f, Axis.Y).Ease(Curve.CubeIn);
        m_FullyExpanded = true;
        yield return null;
    }

}
