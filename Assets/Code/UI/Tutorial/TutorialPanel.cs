using FieldDay;
using FieldDay.Scripting;
using FieldDay.UI;
using UnityEngine;
using Zavala.Building;
using Zavala.Economy;

namespace Zavala.UI.Tutorial {
    public class TutorialPanel : SharedRoutinePanel {
        #region Inspector

        [Header("Tutorial")]
        [SerializeField] private TutorialPanelConfigurer m_Configurer;
        [SerializeField] private TutorialConfig[] m_Configurations;

        #endregion // Inspector

        static public TutorialContexts GetCurrentContexts() {
            TutorialContexts contexts = default;
            BlueprintState bp = Game.SharedState.Get<BlueprintState>();
            if (bp.IsActive) {
                contexts |= TutorialContexts.Blueprints;
                BuildToolState bt = Game.SharedState.Get<BuildToolState>();
                switch (bt.ActiveTool) {
                    case UserBuildTool.Road: {
                        contexts |= TutorialContexts.BuildRoad;
                        break;
                    }
                    case UserBuildTool.Storage: {
                        contexts |= TutorialContexts.BuildStorage;
                        break;
                    }
                    case UserBuildTool.Digester: {
                        contexts |= TutorialContexts.BuildDigester;
                        break;
                    }
                    case UserBuildTool.Destroy: {
                        contexts |= TutorialContexts.DestroyMode;
                        break;
                    }
                }
            }
            ScriptRuntimeState sc = ScriptUtility.Runtime;
            if (sc.Cutscene.IsRunning()) {
                contexts |= TutorialContexts.Dialogue;
            }
            return contexts;
        }
    }
}