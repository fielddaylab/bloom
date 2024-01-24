using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Economy;

namespace Zavala.UI.Info {
    public class InfoPopupTab : MonoBehaviour {

        [SerializeField] public ResourceMask Resource;
        [SerializeField] private TMP_Text Title;
        [SerializeField] private Image Icon;
        [SerializeField] private Graphic m_Graphic;

        [SerializeField] public Button TabButton;

        public Sprite GetSprite() {
            return Icon.sprite;
        }
        public void TintTab(Color tint) {
            m_Graphic.color = tint;
            //m_Graphic.CrossFadeColor(tint, 0.2f, false, false);
        }
    }


}