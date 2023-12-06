using System;
using UnityEngine;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Input {
    /// <summary>
    /// Input system that relies on user input.
    /// TODO: A system that relies on an external input stream (e.g. for playback)
    /// </summary>
    [SysUpdate(GameLoopPhase.PreUpdate, 1)]
    public class UserInputCameraSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera> {
        public override void ProcessWork(float deltaTime) {
            m_StateA.ViewportMouseRay = m_StateB.Camera.ViewportPointToRay(m_StateA.ViewportMousePos, Camera.MonoOrStereoscopicEye.Mono);
            
            // TODO: evaluate performance?
            SimDataUtility.TryUpdateCurrentRegion(ZavalaGame.SimGrid, ZavalaGame.SimWorld, m_StateB.LookTarget);
        }
    }
}