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
                tile.TopRenderer.sharedMaterial = tile.TopDefaultConfig.Material;
            }
            if (tile.PillarRenderer) {
                tile.PillarRenderer.sharedMaterial = tile.PillarDefaultMat;
            }
        }

        static public void SetMaterial(TileInstance tile, Material newMat) {
            if (tile.TopRenderer) {
                // assign new material
                tile.TopRenderer.sharedMaterial = newMat;
            }
            if (tile.PillarRenderer) {
                if (tile.PillarDefaultMat == null) {
                    // handle an unassigned default material
                    tile.PillarDefaultMat = tile.PillarRenderer.sharedMaterial;
                }

                // assign new material
                tile.PillarRenderer.sharedMaterial = newMat;
            }
        }

        static public bool SetTopVisibility(TileInstance tile, bool vis) {
            if (tile != null) {
                Debug.Log("[TileEffectRendering] Hiding top of tile " + tile.ToString());
                tile.TopRenderer.enabled = vis;
                return true;
            }
            Debug.Log("[TileEffectRendering] Tile doesn't exist, apparently");
            return false;
        }
    }
}