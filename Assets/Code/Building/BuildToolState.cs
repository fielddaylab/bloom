using System;
using UnityEngine;
using FieldDay.SharedState;
using FieldDay;
using System.Collections.Generic;

namespace Zavala.Building {
    public struct RoadToolState {
        // [NonSerialized] public bool StartedRoad;
        [NonSerialized] public List<int> TracedTileIdxs;
        [NonSerialized] public int PrevTileIndex; // last known tile used for building roads

        // [NonSerialized] public List<GameObject> StagedBuilds; // visual indicator to player of what they will build, but not finalized on map

        // TODO: implement toll booths
        // [NonSerialized] public TollBooth m_lastKnownToll;

        public void ClearState() {
            PrevTileIndex = -1;
            if (TracedTileIdxs == null) {
                TracedTileIdxs = new List<int>();
                // StagedBuilds = new List<GameObject>();
            }
            else {
                TracedTileIdxs.Clear();
                // StagedBuilds.Clear();
            }
        }
    }

    public class BuildToolState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public UserBuildTool ActiveTool = UserBuildTool.None;
        [NonSerialized] public RoadToolState RoadToolState;
        [NonSerialized] public HexVector VecPrev;
        public GameObject Digester;

        public void OnDeregister() {
        }

        public void OnRegister() {
            ClearRoadTool();
        }

        /// <summary>
        /// Resets road tool state
        /// </summary>
        public void ClearRoadTool() {
            RoadToolState.ClearState();
        }
    }

    public enum UserBuildTool : byte {
        None,
        Destroy,
        Road,
        Storage,
        Digester
        // Skimmer
    }
}