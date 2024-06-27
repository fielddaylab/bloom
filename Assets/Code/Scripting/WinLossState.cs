


using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Data;
using Zavala.Sim;
using Zavala.UI;
using Zavala.World;

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
        [Header("Failure")]
        public int BudgetBelow;
        public bool CheckFarmsUnconnected;
        public int TotalPhosphorusAbove;
        public float TotalAlgaeAbove;
        public int CityFallingDurationAbove;
        [Header("Success")]
        public int RegionAgeAbove;
        public string NodeReached;
        public TargetRatio DFertilizerRatio;
        public TargetRatio MFertilizerRatio;
        public bool CheckCitiesConnected;
    }

    [Serializable]
    public struct TargetRatio {
        [Range(0f, 1f)]
        public float Target;
        public bool Above;
    }

    [Serializable]
    public struct ConditionsPerRegion {
        public RegionId Region;
        public EndGameConditions[] IndependentEndConditions;
    }

    public class WinLossState : SharedStateComponent, IRegistrationCallbacks {
        public SimTimer EndCheckTimer;

        //public GameLoop Loop;
        public SceneReference SceneOnFail;
        public bool IgnoreFailure = false;
        public ConditionsPerRegion[] EndConditionsPerRegion;
        // [NonSerialized] public bool[] FarmsConnectedInRegion;
        [NonSerialized] public int[] CityFallingTimersPerRegion;
        [NonSerialized] public bool CheckTimer;
        [NonSerialized] public bool HasMetEnding;

        public void OnDeregister() {
        }

        public void OnRegister() {
            // FarmsConnectedInRegion = new bool[RegionInfo.MaxRegions];
            CityFallingTimersPerRegion = new int[RegionInfo.MaxRegions];

        }
    }

    public static class WinLossUtility {

        [LeafMember("EndGame")]
        public static void EndGame() {
            // make sure to deregister anything that registers itself on start without deregistering on end
            // Game.Scenes.UnloadScene(Game.Scenes.)
            WinLossState wls = Game.SharedState.Get<WinLossState>();
            SimAllocator.Reset();
            Game.Scenes.LoadMainScene(wls.SceneOnFail);
        }

        public static void IgnoreFailure() {
            Game.SharedState.Get<WinLossState>().IgnoreFailure = true;
        }

        [LeafMember("FailureIsEnabled")]
        public static bool FailureIsEnabled() {
            return !Game.SharedState.Get<WinLossState>().IgnoreFailure;
        }
        public static void TriggerEnding(EndType eType, int regionIndex) {
            if (eType == EndType.Succeeded) {
                Log.Warn("[WinLossSystem] TRIGGERED GAME WIN {0} in Region {1}", eType.ToString(), regionIndex);
                using (TempVarTable varTable = TempVarTable.Alloc()) {
                    varTable.Set("endType", eType.ToString());
                    varTable.Set("alertRegion", regionIndex + 1);
                    ScriptUtility.Trigger(GameTriggers.GameCompleted, varTable);
                }
                ZavalaGame.Events.Dispatch(GameEvents.GameWon);
                return;
            }
            Log.Warn("[WinLossSystem] TRIGGERED GAME FAIL {0} in Region {1}", eType, regionIndex);

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("endType", eType.ToString());
                varTable.Set("alertRegion", regionIndex + 1);
                WorldCameraUtility.PanCameraToRegionCity(regionIndex + 1);
                var handle = ScriptUtility.Trigger(GameTriggers.GameFailed, varTable);
                SaveUtility.Reload();
            }
            ZavalaGame.Events.Dispatch(GameEvents.GameFailed, EvtArgs.Box(new LossData(eType.ToString(), (ushort) regionIndex)));
        }

        public static void TriggerWin() {
            TriggerEnding(EndType.Succeeded, 1);
        }

        [DebugMenuFactory]
        static private DMInfo WinLossMenu() {
            DMInfo info = new DMInfo("Win/Loss");
            info.AddButton("Disable Failure", () => {
                IgnoreFailure();
                CutscenePanel.End();
            });
            info.AddButton("Trigger Win", () => {
                TriggerWin();
            });
            info.AddButton("End Game", () => {
                EndGame();
            });
            return info;
        }

    }
}