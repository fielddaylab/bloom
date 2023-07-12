using BeauUtil;
using FieldDay;
using UnityEngine;
using Zavala.Building;

namespace Zavala.Advisor {
    public class AdvisorButton : MonoBehaviour {
        [SerializeField] public AdvisorType ButtonAdvisorType;
        public void PressToggle(bool toggle) {
            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            if (toggle) {
                advisorState.ActiveAdvisor = ButtonAdvisorType;
            } else {
                advisorState.ActiveAdvisor = AdvisorType.None;
            }
        }
    }


}