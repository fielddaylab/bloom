using BeauRoutine;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World
{
    public class BorderRenderSystem : SharedStateSystemBehaviour<SimGridState, SimWorldState>
    {
        // public Material BorderMaterial;

        #region Work

        public override bool HasWork() {
            if (base.HasWork()) {
                // return ___, after border info generated
            }
            return false;
        }

        public override void ProcessWork(float deltaTime) {
            foreach (var index in m_StateA.HexSize) {
                if (m_StateA.HexSize.IsValidIndex(index)) {
                    if (!m_StateB.Tiles[index]) {
                        continue;
                    }
                    if ((m_StateA.Terrain.Info[index].Flags & TerrainFlags.IsBorder) != 0) {
                        // TODO: pick a render approach and implement here
                        // TileEffectRendering.SetMaterial(m_StateB.Tiles[index], BorderMaterial);
                    }
                }
            }
        }

        #endregion // Work

    }
}