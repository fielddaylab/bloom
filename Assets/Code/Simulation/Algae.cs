using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Data;
using Leaf.Runtime;
using UnityEngine;

namespace Zavala.Sim {

    /// <summary>
    /// Algae state for each tile
    /// </summary>
    public struct AlgaeTileState {
        public float PercentAlgae; // growth level of algae for this tile
        public bool isWater;
        public bool IsPeaked;
    }

    static public class AlgaeSim {
        #region Tunable Parameters

        // begin growing algae when this P threshold is exceeded
        [ConfigVar("Minimum Phosphorus for Algae Growth", 1, 64, 1)] static public int MinPForAlgaeGrowthDefault = 15;
        // gain this percentage of growth per sim tick that P is above threshold
        [ConfigVar("Algae Growth Increment", 0, 0.2f, 0.01f)] static public float AlgaeGrowthIncrement = 0.1f;
        #endregion

    }

}