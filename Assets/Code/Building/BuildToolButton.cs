using FieldDay;
using UnityEngine;

namespace Zavala.Building {
    public class BuildToolButton : MonoBehaviour  {
        [SerializeField] public UserBuildTool ButtonTool;
        public void PressToggle(bool toggle) {
            BuildToolState bts = Game.SharedState.Get<BuildToolState>();
            if (toggle) {
                BuildToolUtility.SetTool(bts, ButtonTool);
            } else {
                BuildToolUtility.SetTool(bts, UserBuildTool.None);
            }
        }
    }
}

