


using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using Zavala.Sim;
using Zavala.UI;

namespace Zavala.Scripting {

    public enum EndType {
        OutOfMoney,
        CityFailed,
        TooManyBlooms,
        Succeeded
    }



    [Serializable]
    // Failure conditions 
    public struct EndGameConditions {
        public EndType Type;
        public int BudgetBelow;
        public bool CheckFarmsUnconnected;
        public int TotalPhosphorusAbove;
        public float TotalAlgaeAbove;
        public int CityFallingDurationAbove;
        public int RegionAgeAbove;
    }

    [Serializable]
    public struct ConditionsPerRegion {
        public RegionId Region;
        public List<EndGameConditions> IndependentEndConditions;
    }

    public class WinLossState : SharedStateComponent, IRegistrationCallbacks {
        public SceneReference SceneOnFail;
        public bool IgnoreFailure = false;
        public ConditionsPerRegion[] EndConditionsPerRegion;
        [NonSerialized] public bool[] FarmsConnectedInRegion;
        [NonSerialized] public int[] CityFallingTimersPerRegion;
        [NonSerialized] public bool CheckTimer;




        public void OnDeregister() {
        }

        public void OnRegister() {
            FarmsConnectedInRegion = new bool[RegionInfo.MaxRegions];
            CityFallingTimersPerRegion = new int[RegionInfo.MaxRegions];

        }
    }

    public static class WinLossUtility {

        [LeafMember("EndGame")]
        public static void EndGame() {
            // make sure to deregister anything that registers itself on start without deregistering on end
            SimAllocator.Reset();
            Game.Scenes.LoadMainScene(Game.SharedState.Get<WinLossState>().SceneOnFail);
        }

        public static void IgnoreFailure() {
            Game.SharedState.Get<WinLossState>().IgnoreFailure = true;
        }

        [LeafMember("FailureIsEnabled")]
        public static bool FailureIsEnabled() {
            return !Game.SharedState.Get<WinLossState>().IgnoreFailure;
        }

        [DebugMenuFactory]
        static private DMInfo WinLossMenu() {
            DMInfo info = new DMInfo("Win/Loss");
            info.AddButton("Disable Failure", () => {
                IgnoreFailure();
                CutscenePanel.End();
            });
            return info;
        }

    }
}