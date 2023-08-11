using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Sim;
using Zavala.Roads;
using Zavala.Building;

namespace Zavala.World
{
    static public class TileEffectRendering
    {
        static public void RestoreDefaultMaterial(TileInstance tile) {
            if (tile.TopRenderer) {
                tile.TopRenderer.material = tile.TopDefaultMat;
            }
            if (tile.PillarRenderer) {
                tile.PillarRenderer.material = tile.PillarDefaultMat;
            }
        }

        static public void SetMaterial(TileInstance tile, Material newMat) {
            if (tile.TopRenderer) {
                if (tile.TopDefaultMat == null) {
                    // handle an unassigned default material
                    tile.TopDefaultMat = tile.TopRenderer.material;
                }

                // assign new material
                tile.TopRenderer.material = newMat;
            }
            if (tile.PillarRenderer) {
                if (tile.PillarDefaultMat == null) {
                    // handle an unassigned default material
                    tile.PillarDefaultMat = tile.PillarRenderer.material;
                }

                // assign new material
                tile.PillarRenderer.material = newMat;
            }
        }
    }
}