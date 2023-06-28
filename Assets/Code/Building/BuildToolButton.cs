using FieldDay;
using UnityEngine;

namespace Zavala.Building {
    public class BuildToolButton : MonoBehaviour  {
        [SerializeField] public UserBuildTool ButtonTool;
        public void PressToggle(bool toggle) {
            BuildToolState bts = Game.SharedState.Get<BuildToolState>();
            if (toggle) {
                bts.ActiveTool = ButtonTool;
            } else {
                bts.ActiveTool = UserBuildTool.None;
            }
        }
    }
}

