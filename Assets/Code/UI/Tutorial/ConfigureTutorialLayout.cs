using BeauRoutine;
using UnityEngine;

namespace Zavala.UI.Tutorial {
    [SharedBetweenAnimators]
    public class ConfigureTutorialLayout : StateMachineBehaviour {
        public TutorialLayout Layout;
        public float PanelHeight = 230;
        public TextId Label;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            var configurer = animator.GetComponent<TutorialPanelConfigurer>();
            RectTransform rect = animator.GetComponent<RectTransform>();
            rect.SetSizeDelta(PanelHeight, Axis.Y);

            configurer.HexLayout.SetActive(Layout);
            configurer.UILayout.SetActive(!Layout);
            configurer.Label.SetText(Label);

            if (Layout) {
                configurer.Configure(Layout);
            }
        }
    }
}