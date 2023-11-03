using BeauRoutine;
using FieldDay;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;
using static System.TimeZoneInfo;

namespace Zavala.Movement
{
    [SysUpdate(GameLoopPhase.Update, 15, ZavalaGame.SimulationUpdateMask)] // After RequestFulfillment System (10)

    public class AirshipMovementSystem : ComponentSystemBehaviour<RequestFulfiller, AirshipInstance>
    {
        public override void ProcessWork(float deltaTime) {
            SimTimeState timeState = ZavalaGame.SimTime;
            MarketPools pools = Game.SharedState.Get<MarketPools>();

            deltaTime = SimTimeUtility.AdjustedDeltaTime(deltaTime, timeState);

            foreach (var component in m_Components) {
                ProcessAirship(component.Primary, component.Secondary, pools, deltaTime, timeState);
            }
        }

        private void ProcessAirship(RequestFulfiller fulfiller, AirshipInstance airship, MarketPools pools, float deltaTime, SimTimeState timeState) {
            if (!airship.MovementRoutine.Exists()) {
                switch (airship.MoveState) {
                    case AirshipInstance.State.Entering:
                        ProcessEntering(fulfiller, airship, timeState);
                        break;
                    case AirshipInstance.State.EnRoute:
                        ProcessEnRoute(fulfiller, airship, timeState);
                        break;
                    case AirshipInstance.State.Exiting:
                        ProcessExiting(fulfiller, airship, timeState);
                        break;
                    case AirshipInstance.State.Finished:
                        FreeAirship(fulfiller, airship, pools);
                        break;
                    default:
                        break;
                }
            }
            else {
                airship.MovementRoutine.TryManuallyUpdate(deltaTime);
            }
        }

        private void ProcessEntering(RequestFulfiller fulfiller, AirshipInstance airship, SimTimeState timeState) {
            airship.MovementRoutine.Replace(EnterRoutine(fulfiller, airship, timeState)).SetPhase(RoutinePhase.Manual);
        }

        private void ProcessEnRoute(RequestFulfiller fulfiller, AirshipInstance airship, SimTimeState timeState) {
            airship.MovementRoutine.Replace(EnRouteRoutine(fulfiller, airship, timeState)).SetPhase(RoutinePhase.Manual);
        }

        private void ProcessExiting(RequestFulfiller fulfiller, AirshipInstance airship, SimTimeState timeState) {
            airship.MovementRoutine.Replace(ExitRoutine(fulfiller, airship, timeState)).SetPhase(RoutinePhase.Manual);
            fulfiller.AtTransitionPoint = false;
        }

        private void FreeAirship(RequestFulfiller fulfiller, AirshipInstance airship, MarketPools pools) {
            // free airship
            if (airship.IsExternal) {
                pools.ExternalAirships.Free(fulfiller);
            }
            else {
                pools.InternalAirships.Free(fulfiller);
            }
        }

        #region Routines 

        private IEnumerator EnterRoutine(RequestFulfiller fulfiller, AirshipInstance airship, SimTimeState timeState) {
            // fade in
            // yield return airship.Mesh.material.FadeTo(0, 0);

            Vector3 descendPos = fulfiller.SourceWorldPos - new Vector3(0, 0.5f, 0) + airship.transform.forward * 0.3f;

            // descend
            yield return Routine.Combine(
                // airship.Mesh.material.FadeTo(1, 1.5f).Ease(Curve.SineIn),
                airship.transform.MoveTo(descendPos, 1.5f).Ease(Curve.SineIn)
                );

            // transition state
            airship.MoveState = AirshipInstance.State.EnRoute;

            yield return null;
        }

        private IEnumerator EnRouteRoutine(RequestFulfiller fulfiller, AirshipInstance airship, SimTimeState timeState) {
            Vector3 newPos;
            do {
                float deltaTime = Time.deltaTime;

                newPos = Vector3.MoveTowards(fulfiller.transform.position, fulfiller.TargetWorldPos, MarketParams.AirshipSpeed * deltaTime);
                fulfiller.transform.position = newPos;
                yield return null;
            }
            while (!Mathf.Approximately(Vector3.Distance(newPos, fulfiller.TargetWorldPos), 0));

            fulfiller.AtTransitionPoint = true;
            airship.MoveState = AirshipInstance.State.Exiting;
        }

        private IEnumerator ExitRoutine(RequestFulfiller fulfiller, AirshipInstance airship, SimTimeState timeState) {
            Vector3 risePos = airship.transform.position + new Vector3(0, 0.5f, 0) + airship.transform.forward * 0.3f;

            // rise and fade out
            yield return Routine.Combine(
                // airship.Mesh.material.FadeTo(0, 1.5f),
                airship.transform.MoveTo(risePos, 1.5f)
                );

            airship.MoveState = AirshipInstance.State.Finished;
        }

        #endregion // Routines
    }
}
