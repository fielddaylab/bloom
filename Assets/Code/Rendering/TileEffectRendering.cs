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
        static public bool SetTopVisibility(TileInstance tile, bool vis) {
            if (tile != null) {
                //Debug.Log("[TileEffectRendering] Hiding top of tile " + tile.ToString());
                tile.TopRenderer.enabled = vis;
                return true;
            }
            //Debug.Log("[TileEffectRendering] Tile doesn't exist, apparently");
            return false;
        }
    }
}