using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
//using System.Linq;
using UnityEngine;
using Zavala.Data;
using Zavala.Sim;

namespace Zavala.Economy
{
    public struct Budget
    {
        public long Net;

        public bool Updated; // whether the budget was updated since last check
    }

    [SharedStateInitOrder(10)]
    public sealed class BudgetData : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject
    {
        [SerializeField] private long[] m_initialBudgetsPerRegion;

        [Header("Per-Region")]
        public Budget[] BudgetsPerRegion = new Budget[RegionInfo.MaxRegions];

        public void OnRegister() {
            for (int i = 0; i < BudgetsPerRegion.Length; i++) {
                BudgetUtility.SetBudget(this, m_initialBudgetsPerRegion[i], i);
            }

            ZavalaGame.SaveBuffer.RegisterHandler("Budget", this);
        }

        public void OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Budget");
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            for(int i = 0; i < consts.MaxRegions; i++) {
                writer.Write(BudgetsPerRegion[i].Net);
            }
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            ArrayUtils.EnsureCapacity(ref BudgetsPerRegion, consts.MaxRegions);
            for(int i = 0; i < consts.MaxRegions; i++) {
                reader.Read(ref BudgetsPerRegion[i].Net);
            }
        }
    }

    /// <summary>
    /// Budget utility methods.
    /// </summary>
    static public class BudgetUtility
    {
        static public void SetBudget(BudgetData budgetData, long budget, int regionIndex) {
            budgetData.BudgetsPerRegion[regionIndex].Net = budget;

            budgetData.BudgetsPerRegion[regionIndex].Updated = true;
        }

        static public void AddToBudget(BudgetData budgetData, long toAdd, int regionIndex) {
            budgetData.BudgetsPerRegion[regionIndex].Net += toAdd;

            budgetData.BudgetsPerRegion[regionIndex].Updated = true;
        }

        // "try add" in case of negative addition - translates to spending
        static public bool TryAddToBudget(BudgetData budgetData, long toAdd, int regionIndex) {
            return TrySpendBudget(budgetData, -toAdd, (uint)regionIndex);
        }

        static public bool CanSpendBudget(BudgetData budgetData, long toSpend, uint regionIndex)
        {
            long net = budgetData.BudgetsPerRegion[regionIndex].Net;
            return net - toSpend >= 0;
        }

        static public bool TrySpendBudget(BudgetData budgetData, long toSpend, uint regionIndex) {
            long net = budgetData.BudgetsPerRegion[regionIndex].Net;
            if (net - toSpend >= 0) {
                budgetData.BudgetsPerRegion[regionIndex].Net -= toSpend;
                budgetData.BudgetsPerRegion[regionIndex].Updated = true;
                return true;
            }
            return false;
        }

        [DebugMenuFactory]
        static private DMInfo BudgetDebugMenu()
        {
            DMInfo info = new DMInfo("Budget");
            info.AddButton("+ $50", () => {
                BudgetData budget = Game.SharedState.Get<BudgetData>();
                SimGridState grid = Game.SharedState.Get<SimGridState>();
                BudgetUtility.AddToBudget(budget, 50, (int)grid.CurrRegionIndex);
            }, () => Game.SharedState.TryGet(out BudgetData budget));

            info.AddButton("+ $1000", () => {
                BudgetData budget = Game.SharedState.Get<BudgetData>();
                SimGridState grid = Game.SharedState.Get<SimGridState>();
                BudgetUtility.AddToBudget(budget, 1000, (int)grid.CurrRegionIndex);
            }, () => Game.SharedState.TryGet(out BudgetData budget));

            return info;
        }
    }
}