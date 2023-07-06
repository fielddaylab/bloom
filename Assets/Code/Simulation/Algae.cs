using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Zavala.Sim {

    /// <summary>
    /// Algae state for each tile
    /// </summary>
    public struct AlgaeTileState {
        public float PercentAlgae; // growth level of algae for this tile
        public bool isWater;
    }

    static public class AlgaeSim {
        #region Tunable Parameters

        // begin growing algae when this P threshold is exceeded
        [ConfigIntVar("Minimum Phosphorus for Algae Growth", 1, 64, 1)] static public int MinPForAlgaeGrowth = 10;
        // gain this percentage of growth per sim tick that P is above threshold
        [ConfigFloatVar("Algae Growth Increment", 0, 1, 0.1f)] static public float AlgaeGrowthIncrement = 0.1f;

        #endregion

    }

}