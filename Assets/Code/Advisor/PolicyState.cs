using System;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine.Events;
using Zavala.Cards;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.Advisor {
    public struct PolicyBlock {
        public RunoffPenaltyLevel Runoff;
        public SkimmingLevel Skimming;
        // public ExportTaxLevel ExportTax;
        public ImportTaxLevel ImportTax;
        public SalesTaxLevel SalesTax;
    }

    // TODO: track unlocked policies (progression)

    public class PolicyState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public PolicyBlock[] Policies = new PolicyBlock[RegionInfo.MaxRegions];

        // public static ResourceBlock[] ExportTaxVals = new ResourceBlock[4];
        public static ResourceBlock[] ImportTaxVals = new ResourceBlock[4];
        public static ResourceBlock[] SalesTaxVals = new ResourceBlock[4];

        public UnityEvent<PolicyType> PolicySlotClicked = new UnityEvent<PolicyType>();
        public UnityEvent<CardData> PolicyCardSelected = new UnityEvent<CardData>();
        public UnityEvent PolicyCloseButtonClicked = new UnityEvent();

        // TODO: There is probably a cleaner way to do this. Does it belong in a system?
        public bool SetPolicyByIndex(PolicyType policyType, int policyIndex, int region) {
            if (policyIndex < 0 || policyIndex > 3) return false;
            switch (policyType) {
                case PolicyType.RunoffPolicy:
                    Policies[region].Runoff = (RunoffPenaltyLevel)policyIndex;
                    // TODO: When runoff penalty implemented, read runoff policy
                    return true;
                case PolicyType.SkimmingPolicy:
                    Policies[region].Skimming = (SkimmingLevel)policyIndex;
                    // TODO: When skimming implemented, read skimming policy
                    return true;
                    /*
                case PolicyType.ExportTaxPolicy:
                    Policies[region].ExportTax = (ExportTaxLevel)policyIndex;
                    // set this region's export tax to the initialized export tax value for the given index
                    Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].ExportTax = ExportTaxVals[policyIndex];
                    return true;
                    */
                case PolicyType.ImportTaxPolicy:
                    Policies[region].ImportTax = (ImportTaxLevel)policyIndex;
                    // set this region's import tax to the initialized import tax value for the given index
                    Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].ImportTax = ImportTaxVals[policyIndex];
                    return true;
                case PolicyType.SalesTaxPolicy:
                    Policies[region].SalesTax = (SalesTaxLevel)policyIndex;
                    // set this region's purchase tax to the initialized sales tax value for the given index
                    Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].PurchaseTax = SalesTaxVals[policyIndex];
                    return true; 
                default:
                    return false;
            }
        }

        public void InitializePolicyValues() {
            /*
            ExportTaxVals[0].SetAll(-2);
            ExportTaxVals[1].SetAll(0);
            ExportTaxVals[2].SetAll(2);
            ExportTaxVals[3].SetAll(4);
            */

            ImportTaxVals[0].SetAll(-2);
            ImportTaxVals[1].SetAll(0);
            ImportTaxVals[2].SetAll(2);
            ImportTaxVals[3].SetAll(4);

            SalesTaxVals[0].SetAll(-2);
            SalesTaxVals[1].SetAll(0);
            SalesTaxVals[2].SetAll(2);
            SalesTaxVals[3].SetAll(4);
        }

        /// <summary>
        /// Return a numerical value of runoff penalty based on the policy level.
        /// </summary>
        /// <param name="level">Level of runoff penalty to be converted</param>
        /// <returns></returns>
        public int RunoffPenaltyToMoney(RunoffPenaltyLevel level) {
            return 5 * (int)level;
        }

        public void OnRegister() {
            InitializePolicyValues();

            PolicyCardSelected.AddListener(HandlePolicyCardSelected);
        }

        public void OnDeregister() {
        }

        #region Handlers

        private void HandlePolicyCardSelected(CardData data) {
            SetPolicyByIndex(data.PolicyType, (int)data.PolicyLevel, (int)Game.SharedState.Get<SimGridState>().CurrRegionIndex);
        }

        #endregion // Handlers
    }

    public enum PolicyType : byte {
        RunoffPolicy,
        SkimmingPolicy,
        // ExportTaxPolicy,
        ImportTaxPolicy,
        SalesTaxPolicy
    }

    #region Policy Level Names

    public enum RunoffPenaltyLevel {
        None,
        Low,
        High,
        Shutdown
    }

    public enum SkimmingLevel {
        None,
        Low,
        High,
        Dredge
    }

    /*
    public enum ExportTaxLevel {
        None,
        Low,
        High,
        Subsidy
    }
    */

    public enum ImportTaxLevel
    {
        None,
        Low,
        High,
        Subsidy
    }


    public enum SalesTaxLevel {
        None,
        Low,
        High,
        Subsidy
    }



    #endregion
}