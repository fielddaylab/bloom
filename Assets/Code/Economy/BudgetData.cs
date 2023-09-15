using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using System.Linq;
using UnityEngine;
using UnityEngine.WSA;
using Zavala.Sim;

namespace Zavala.Economy
{
    public struct Budget
    {
        public long Net;

        public bool Updated; // whether the budget was updated since last check
    }

    [SharedStateInitOrder(10)]
    public sealed class BudgetData : SharedStateComponent, IRegistrationCallbacks
    {
        [SerializeField] private long m_initialBudget;

        [Header("Per-Region")]
        public Budget[] BudgetsPerRegion = new Budget[RegionInfo.MaxRegions];

        public void OnRegister() {
            for (int i = 0; i < BudgetsPerRegion.Count(); i++) {
                BudgetUtility.SetBudget(this, m_initialBudget, i);
            }
        }

        public void OnDeregister() {
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

        static public bool TrySpendBudget(BudgetData budgetData, long toSpend, uint regionIndex) {
            long net = budgetData.BudgetsPerRegion[regionIndex].Net;
            if (net - toSpend >= 0) {
                budgetData.BudgetsPerRegion[regionIndex].Net -= toSpend;
                budgetData.BudgetsPerRegion[regionIndex].Updated = true;
                return true;
            }
            return false;
        }

    }
}