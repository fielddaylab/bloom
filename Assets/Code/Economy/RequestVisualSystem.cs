using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 4)]
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
                if (VisualIndex(request.Requester, request.Requested) == -1) {
                    // Spawn a new request visual
                    // TODO: make an appear routine
                    HexVector pos = m_StateC.HexSize.FastIndexToPos(request.Requester.Position.TileIndex);
                    Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
                    RequestVisual newVisual = m_StateB.RequestPool.Alloc(worldPos);
                    newVisual.Resources = request.Requested;
                    RequestVisualUtility.SetResourceGraphic(newVisual, newVisual.Resources);

                    if (m_StateB.VisualMap.ContainsKey(request.Requester)) {
                        // Add a new visual to the requester's queue
                        m_StateB.VisualMap[request.Requester].PushBack(newVisual);
                    }
                    else {
                        // initialize a new queue for this requester, then add the visual
                        RingBuffer<RequestVisual> newRing = new RingBuffer<RequestVisual>(4, RingBufferMode.Expand);
                        newRing.PushBack(newVisual);
                        m_StateB.VisualMap.Add(request.Requester, newRing);
                    }
                }
            }
        }

        private void RemoveFulfilledVisuals() {
            // Remove alerts which have been fulfilled
            foreach (MarketActiveRequestInfo request in m_StateB.FulfilledQueue) {
                // request has been fulfilled -- remove visual
                int index = VisualIndex(request.Requester, request.Requested);
                if (index != -1) {
                    RingBuffer<RequestVisual> visuals = m_StateB.VisualMap[request.Requester];
                    // match found
                    // TODO: make a disappear routine
                    // free from pools
                    m_StateB.RequestPool.Free(visuals[index]);

                    // remove from list of visuals
                    visuals.FastRemoveAt(index);
                    m_StateB.VisualMap[request.Requester] = visuals;
                }
            }
        }

        private int VisualIndex(ResourceRequester requester, ResourceBlock requested) {
            if (!m_StateB.VisualMap.ContainsKey(requester)) {
                return -1;
            }
            RingBuffer<RequestVisual> visuals = m_StateB.VisualMap[requester];
            // find which request visual matches the fulfilled request
            for (int i = 0; i < visuals.Count; i++) {
                RequestVisual visual = visuals[i];
                if ((visual.Resources - requested).IsZero) {
                    return i;
                }
            }

            return -1;
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
    }
}

