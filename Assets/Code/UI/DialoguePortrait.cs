using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePortrait : MonoBehaviour {

    [SerializeField] private Graphic Outline;
    [SerializeField] private Graphic Shadow;
    [SerializeField] private float DegreeTilt;

    public void ShowDetails(Color outlineColor, bool show = true) {
        Outline.gameObject.SetActive(show);
        Outline.color = outlineColor;
        Shadow.gameObject.SetActive(show);
        transform.SetRotation(show ? DegreeTilt : 0, Axis.Z);
    }
}
