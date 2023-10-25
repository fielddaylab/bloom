using FieldDay;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.Movement
{
    [SysUpdate(GameLoopPhase.Update, 15, ZavalaGame.SimulationUpdateMask)] // After RequestFulfillment System (10)

    public class AirshipMovementSystem : ComponentSystemBehaviour<RequestFulfiller, AirshipInstance>
    {
        private static float AIRSHIP_SPEED = 2;

        public override void ProcessWork(float deltaTime) {
            SimTimeState timeState = ZavalaGame.SimTime;
            MarketPools pools = Game.SharedState.Get<MarketPools>();

            deltaTime = SimTimeUtility.AdjustedDeltaTime(deltaTime, timeState);

            foreach (var component in m_Components) {
                ProcessAirship(component.Primary, component.Secondary, pools, deltaTime);
            }
        }

        private void ProcessAirship(RequestFulfiller fulfiller, AirshipInstance airship, MarketPools pools, float deltaTime) {
            switch (airship.MoveState) {
                case AirshipInstance.State.Entering:
                    ProcessEntering(airship);
                    break;
                case AirshipInstance.State.EnRoute:
                    ProcessEnRoute(fulfiller, airship, deltaTime);
                    break;
                case AirshipInstance.State.Exiting:
                    ProcessExiting(fulfiller, airship, pools);
                    break;
                default:
                    break;
            }
        }

        private void ProcessEntering(AirshipInstance airship) {
            // TODO: fade in

            // transition state
            airship.MoveState = AirshipInstance.State.EnRoute;
        }

        private void ProcessEnRoute(RequestFulfiller fulfiller, AirshipInstance airship, float deltaTime) {
            // travel from src to destination + yOffset
            Vector3 newPos = Vector3.MoveTowards(fulfiller.transform.position, fulfiller.TargetWorldPos, AIRSHIP_SPEED * deltaTime);

            // when same position, drop parcel
            if (Mathf.Approximately(Vector3.Distance(newPos, fulfiller.TargetWorldPos), 0)) {
                fulfiller.AtTransitionPoint = true;
                airship.MoveState = AirshipInstance.State.Exiting;
            }
            else {
                fulfiller.transform.position = newPos;
            }
        }

        private void ProcessExiting(RequestFulfiller fulfiller, AirshipInstance airship, MarketPools pools) {
            // TODO: continue airship travel for a certain amount of glide time

            // TODO: fade out

            // TODO: free
            // free airship
            if (airship.IsExternal) {
                pools.ExternalAirships.Free(fulfiller);
            }
            else {
                pools.InternalAirships.Free(fulfiller);
            }
        }
    }
}
