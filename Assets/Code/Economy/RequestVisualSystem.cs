using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 4, ZavalaGame.SimulationUpdateMask)]
    public class RequestVisualSystem : SharedStateSystemBehaviour<MarketData, RequestVisualState, SimGridState>
    {
        // NOT persistent state - work lists for various updates
        private readonly RingBuffer<MarketRequestInfo> m_RequestWorkList = new RingBuffer<MarketRequestInfo>(8, RingBufferMode.Expand);

        public override void ProcessWork(float deltaTime) {
            if (m_StateA.RequestQueue == null || m_StateA.RequestQueue.Count == 0) {
                return;
            }

            if (m_StateB.NewUrgents.Count > 0) {
                GenerateNewVisuals();
                m_StateB.NewUrgents.Clear();
            }
            RemoveFulfilledVisuals();
            m_StateB.FulfilledQueue.Clear();
        }

        #region Helpers

        private void GenerateNewVisuals() {
            // pop up only for alerts that are urgent
            foreach (var request in m_StateB.NewUrgents) {
                if (request.Requester.Flagstaff.FlagVisuals.gameObject.activeSelf) return;

                request.Requester.Flagstaff?.FlagVisuals.gameObject.SetActive(true);

                // This process is simplified by the fact that a requester will only ever request one category of resource
                // If this assumption changes, we'll need to implement a similar system to what was in place when we had UI popup requests
                if (request.Requester.Flagstaff)
                {
                    request.Requester.Flagstaff.FlagVisuals.Resources = request.Requested;
                    RequestVisualUtility.SetResourceGraphic(request.Requester.Flagstaff.FlagVisuals, request.Requester.Flagstaff.FlagVisuals.Resources);
                }

                int regionIndex = request.Requester.GetComponent<OccupiesTile>().RegionIndex;
                using (TempVarTable varTable = TempVarTable.Alloc()) {
                    varTable.Set("resource", ResourceUtility.FirstResource(request.Requested).ToString());
                    varTable.Set("alertRegion", regionIndex+1); // 0-indexed to 1-indexed
                    ScriptUtility.Trigger(GameTriggers.UrgentRequest, varTable);
                }

                if (!m_StateB.UrgentMap.ContainsKey(request.Requester)) {
                    m_StateB.UrgentMap.Add(request.Requester, 0);
                }

                m_StateB.UrgentMap[request.Requester]++;
            }
        }

        private void RemoveFulfilledVisuals() {
            // Remove alerts which have been fulfilled
            foreach (MarketActiveRequestInfo request in m_StateB.FulfilledQueue) {
                // request has been fulfilled -- remove visual
                if (!m_StateB.UrgentMap.ContainsKey(request.Requester)) {
                    return;
                }

                m_StateB.UrgentMap[request.Requester] = 0; // Reset urgent requests

                if (m_StateB.UrgentMap[request.Requester] <= 0)
                {
                    request.Requester.Flagstaff?.FlagVisuals.gameObject.SetActive(false);
                }
            }
        }

        #endregion // Helpers
    }

    public static class RequestVisualUtility { 
        public static void SetResourceGraphic(RequestVisual visual, ResourceBlock block) {
            if (block.Manure != 0) {
                visual.ResourceImage.sprite = visual.ManureSprite;
            }
            else if (block.MFertilizer != 0) {
                visual.ResourceImage.sprite = visual.MFertilizerSprite;
            }
            else if (block.DFertilizer != 0) {
                visual.ResourceImage.sprite = visual.DFertilizerSprite;
            }
            else if (block.Grain != 0) {
                visual.ResourceImage.sprite = visual.GrainSprite;
            }
            else if (block.Milk != 0) {
                visual.ResourceImage.sprite = visual.MilkSprite;
            }
        }

        public static void SetResourceGraphic(RequestSpriteVisual visual, ResourceBlock block)
        {
            if (block.Manure != 0)
            {
                visual.ResourceImage.sprite = visual.ManureSprite;
            }
            else if (block.MFertilizer != 0)
            {
                visual.ResourceImage.sprite = visual.MFertilizerSprite;
            }
            else if (block.DFertilizer != 0)
            {
                visual.ResourceImage.sprite = visual.DFertilizerSprite;
            }
            else if (block.Grain != 0)
            {
                visual.ResourceImage.sprite = visual.GrainSprite;
            }
            else if (block.Milk != 0)
            {
                visual.ResourceImage.sprite = visual.MilkSprite;
            }
        }
    }
}

