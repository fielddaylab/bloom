using BeauUtil;
using FieldDay;
using UnityEditor;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World {
    public class TileInstance : MonoBehaviour {
        public Renderer TopRenderer;
        public Renderer PillarRenderer;
        public Renderer[] Decorations;

        //static private GUIStyle centerStyle;

        //private void OnDrawGizmos() {
        //    if (!Application.isPlaying || !Frame.IsActive(this)) {
        //        return;
        //    }

        //    SimGridState grid = ZavalaGame.Grid;
        //    SimWorldState world = ZavalaGame.World;

        //    if (SimWorldUtility.TryGetTilePosFromWorld(grid, world, transform.position, out HexVector vec)) {
        //        int index = grid.HexSize.FastPosToIndex(vec);
        //        if (centerStyle == null) {
        //            centerStyle = new GUIStyle(EditorStyles.boldLabel);
        //            centerStyle.alignment = TextAnchor.MiddleCenter;
        //            centerStyle.normal.textColor = Color.red;
        //        }
        //        Gizmos.color = Color.yellow;
        //        Handles.color = Color.yellow;
        //        Handles.Label(transform.position, string.Format("{0}\n[{1},{2}]\nindex {3}", vec.ToString(), vec.X.ToStringLookup(), vec.Y.ToStringLookup(), index.ToStringLookup()), centerStyle);
        //    }
            
        //}
    }
}