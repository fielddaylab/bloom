using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Cards;
using Zavala.Economy;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Advisor {
    public struct PolicyBlock {
        public Dictionary<PolicyType, PolicyLevel> Map;
        public Dictionary<PolicyType, bool> EverSet; // Whether this policy type has ever been set in its region
    }

    // TODO: track unlocked policies (progression)

    public class PolicyState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public PolicyBlock[] Policies = new PolicyBlock[RegionInfo.MaxRegions];

        // public static ResourceBlock[] ExportTaxVals = new ResourceBlock[4];
        public static ResourceBlock[] ImportTaxVals = new ResourceBlock[4];
        public static ResourceBlock[] SalesTaxVals = new ResourceBlock[4];
        public static ResourceBlock[] RunoffPenaltyVals = new ResourceBlock[4];
        // public static int[] SkimmerPolicyVals = new int[4];

        public CastableEvent<PolicyType> PolicySlotClicked = new CastableEvent<PolicyType>();
        public CastableEvent<CardData> PolicyCardSelected = new CastableEvent<CardData>();
        public ActionEvent PolicyCloseButtonClicked = new ActionEvent();
        public ActionEvent OnPolicyUpdated = new ActionEvent();

        // TODO: There is probably a cleaner way to do this. Does it belong in a system?
        public bool SetPolicyByIndex(PolicyType policyType, int policyIndex, int region, bool forced) {
            if (policyIndex < 0 || policyIndex > 3) { return false; }

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("policyType", policyType.ToString());
                varTable.Set("policyIndex", policyIndex);
                varTable.Set("policyRegion", region);
                varTable.Set("policyForced", forced);
                ScriptUtility.Trigger(GameTriggers.PolicySet, varTable);
            }

            Policies[region].Map[policyType] = (PolicyLevel)policyIndex;
            Policies[region].EverSet[policyType] = true; // this policy has now been set
            bool policySetSuccessful = false;
            switch (policyType) {
                case PolicyType.RunoffPolicy:
                    // read runoff policy
                    Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].RunoffPenalty = RunoffPenaltyVals[policyIndex];
                    policySetSuccessful = true;
                    break;
                case PolicyType.SkimmingPolicy:
                    // read skimming policy
                    // Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].SkimmerCost = SkimmerPolicyVals[policyIndex];
                    PhosphorusSkimmerUtility.SpawnSkimmersInRegion(region, policyIndex);
                    policySetSuccessful = true;
                    break;
                    /*
                case PolicyType.ExportTaxPolicy:
                    Policies[region].ExportTax = (ExportTaxLevel)policyIndex;
                    // set this region's export tax to the initialized export tax value for the given index
                    Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].ExportTax = ExportTaxVals[policyIndex];
                    return true;
                    */
                case PolicyType.ImportTaxPolicy:
                    // set this region's import tax to the initialized import tax value for the given index
                    Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].ImportTax = ImportTaxVals[policyIndex];
                    policySetSuccessful = true;
                    break;
                case PolicyType.SalesTaxPolicy:
                    // set this region's purchase tax to the initialized sales tax value for the given index
                    Game.SharedState.Get<MarketConfig>().UserAdjustmentsPerRegion[region].PurchaseTax = SalesTaxVals[policyIndex];
                    policySetSuccessful = true;
                    break;
                default:
                    break;
            }

            OnPolicyUpdated?.Invoke();

            return policySetSuccessful;
        }

        public void InitializePolicyValues() {
            /*
            ExportTaxVals[0].SetAll(-2);
            ExportTaxVals[1].SetAll(0);
            ExportTaxVals[2].SetAll(2);
            ExportTaxVals[3].SetAll(4);
            */

            ImportTaxVals[0].SetAll(0);  // NONE
            ImportTaxVals[1].SetAll(0); // MILK IMPORT SUBSIDY
            ImportTaxVals[1].Milk = -6;
            ImportTaxVals[2].SetAll(0); // GRAIN IMPORT SUBSIDY
            ImportTaxVals[2].Grain = -6;
            ImportTaxVals[3].SetAll(0);  // FERT IMPORT SUBSIDY
            ImportTaxVals[3].DFertilizer = -6;
            ImportTaxVals[3].Manure = -6;


            SalesTaxVals[0].SetAll(0); // NONE 
            SalesTaxVals[1].SetAll(1); // LOW TAX
            SalesTaxVals[2].SetAll(3); // HIGH TAX
            SalesTaxVals[3].SetAll(-2);// SUBSIDY
            SalesTaxVals[3].MFertilizer = 0; // don't apply subsidies to Phos4Us

            RunoffPenaltyVals[0].Manure = 0;
            RunoffPenaltyVals[1].Manure = 8;
            RunoffPenaltyVals[2].Manure = 16;
            RunoffPenaltyVals[3].Manure = 99;

            //SkimmerPolicyVals[0] = 0; 
            //SkimmerPolicyVals[1] = 1; // these are PER SKIMMER
            //SkimmerPolicyVals[2] = 1; // budget spent in PhosphorusSkimmerSystem
            //SkimmerPolicyVals[3] = 1; // dredgers cost DOUBLE (done in PhosphorusSkimmerSystem)
        }

        private void InitializePolicyMap() {
            for (int i = 0; i < Policies.Length; i++) {
                Policies[i].Map = new Dictionary<PolicyType, PolicyLevel>() {
                    { PolicyType.RunoffPolicy, PolicyLevel.None },
                    { PolicyType.SkimmingPolicy, PolicyLevel.None },
                    { PolicyType.ImportTaxPolicy, PolicyLevel.None },
                    { PolicyType.SalesTaxPolicy, PolicyLevel.None }
                };
                Policies[i].EverSet = new Dictionary<PolicyType, bool>() {
                    { PolicyType.RunoffPolicy, false },
                    { PolicyType.SkimmingPolicy, false },
                    { PolicyType.ImportTaxPolicy, false },
                    { PolicyType.SalesTaxPolicy, false }
                };
            }
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
            InitializePolicyMap();

            PolicyCardSelected.Register(HandlePolicyCardSelected);
        }

        public void OnDeregister() {
        }

        #region Handlers

        private void HandlePolicyCardSelected(CardData data) {
            SetPolicyByIndex(data.PolicyType, (int)data.PolicyLevel, (int)Game.SharedState.Get<SimGridState>().CurrRegionIndex, false);
        }

        #endregion // Handlers
    }

    public static class PolicyUtility {

        public static void ForcePolicyToNone(PolicyType type, Transform instigator, int regionIndex) {
            WorldCameraUtility.PanCameraToTransform(instigator);
            Game.SharedState.Get<PolicyState>().SetPolicyByIndex(type, 0, regionIndex, true);
        }
        public static void ForcePolicyToNone(PolicyType type, string instigatorName, int regionIndex) {
            WorldCameraUtility.PanCameraToActor(instigatorName);
            Game.SharedState.Get<PolicyState>().SetPolicyByIndex(type, 0, regionIndex, true);
        }

        [LeafMember("PolicyLevelInRegion")]
        public static int CurrentIndexOfPolicyInRegion(int regionOneIndexed, PolicyType type) {
            int regionZeroIndexed = regionOneIndexed--;
            PolicyState policy = Game.SharedState.Get<PolicyState>();
            PolicyLevel lvl = policy.Policies[regionZeroIndexed].Map[type];
            return (int)lvl;
        }
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
        Tax
    }


    public enum SalesTaxLevel {
        None,
        Low,
        High,
        Subsidy
    }



    #endregion
}