using System;
using FieldDay.SharedState;

namespace Zavala.Advisor {

    public class PolicyState : SharedStateComponent {
        [NonSerialized] public RunoffPenaltyLevel RunoffPenalty;
        [NonSerialized] public SkimmingLevel SkimmingPolicy;
        [NonSerialized] public ExportTaxLevel ExportTaxPolicy;
        [NonSerialized] public SalesTaxLevel SalesTaxPolicy;

        public bool SetPolicy(PolicyType policyType, int policyIndex) {
            if (policyIndex < 0 || policyIndex > 3) return false;
            // TODO: there is probably a cleaner way to do this
            switch (policyType) {
                case PolicyType.RunoffPenalty:
                    RunoffPenalty = (RunoffPenaltyLevel)policyIndex;
                    return true;
                case PolicyType.Skimming:
                    SkimmingPolicy = (SkimmingLevel)policyIndex;
                    return true;
                case PolicyType.ExportTax:
                    ExportTaxPolicy = (ExportTaxLevel)policyIndex;
                    return true;
                case PolicyType.SalesTax:
                    SalesTaxPolicy = (SalesTaxLevel)policyIndex;
                    return true;
                default:
                    return false;
            }
        }

    }


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

    public enum ExportTaxLevel {
        Subsidy,
        None,
        Low,
        High
    }

    // TODO: Define how we want to put different sales taxes in place
    // Flags for different resources?
    // Low/Med/High on all resources?
    public enum SalesTaxLevel {
        None,
        Low,
        High
    }
}