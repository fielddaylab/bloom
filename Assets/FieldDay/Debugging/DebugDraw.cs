using BeauUtil;
using UnityEngine;

namespace FieldDay.Debugging {
    /// <summary>
    /// Debug rendering helper.
    /// </summary>
    public sealed class DebugDraw : MonoBehaviour {
        
        static public void AddBounds(Bounds bounds, Color color, float lineWidth = 1, float duration = 0, bool depthTest = true) {
            unsafe {
                Vector3* corners = stackalloc Vector3[8];
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                corners[0] = min;
                corners[1] = new Vector3(min.x, min.y, max.z);
                corners[2] = new Vector3(min.x, max.y, min.z);
                corners[3] = new Vector3(min.x, max.y, max.z);
                corners[4] = new Vector3(max.x, min.y, min.z);
                corners[5] = new Vector3(max.x, min.y, max.z);
                corners[6] = new Vector3(max.x, max.y, min.z);
                corners[7] = max;

                SubmitBox(corners, color, lineWidth, duration, depthTest);
            }
        }

        static public void AddBounds(Vector3 pointMin, Vector3 pointMax, Color color, float lineWidth = 1, float duration = 0, bool depthTest = true) {
            unsafe {
                Vector3* corners = stackalloc Vector3[8];
                Vector3 min = pointMin;
                Vector3 max = pointMax;
                corners[0] = min;
                corners[1] = new Vector3(min.x, min.y, max.z);
                corners[2] = new Vector3(min.x, max.y, min.z);
                corners[3] = new Vector3(min.x, max.y, max.z);
                corners[4] = new Vector3(max.x, min.y, min.z);
                corners[5] = new Vector3(max.x, min.y, max.z);
                corners[6] = new Vector3(max.x, max.y, min.z);
                corners[7] = max;

                SubmitBox(corners, color, lineWidth, duration, depthTest);
            }
        }

        static public void AddOrientedBounds(Matrix4x4 center, Bounds bounds, Color color, float lineWidth = 1, float duration = 0, bool depthTest = true) {
            unsafe {
                Vector3* corners = stackalloc Vector3[8];
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                corners[0] = min;
                corners[1] = new Vector3(min.x, min.y, max.z);
                corners[2] = new Vector3(min.x, max.y, min.z);
                corners[3] = new Vector3(min.x, max.y, max.z);
                corners[4] = new Vector3(max.x, min.y, min.z);
                corners[5] = new Vector3(max.x, min.y, max.z);
                corners[6] = new Vector3(max.x, max.y, min.z);
                corners[7] = max;

                for (int i = 0; i < 8; i++) {
                    corners[i] = center.MultiplyPoint3x4(corners[i]);
                }

                SubmitBox(corners, color, lineWidth, duration, depthTest);
            }
        }

        static private unsafe void SubmitBox(Vector3* corners, Color color, float lineWidth, float duration, bool depthTest) {
            Debug.DrawLine(corners[0], corners[1], color, duration, depthTest);
            Debug.DrawLine(corners[0], corners[2], color, duration, depthTest);
            Debug.DrawLine(corners[0], corners[4], color, duration, depthTest);

            Debug.DrawLine(corners[1], corners[3], color, duration, depthTest);
            Debug.DrawLine(corners[1], corners[5], color, duration, depthTest);

            Debug.DrawLine(corners[2], corners[3], color, duration, depthTest);
            Debug.DrawLine(corners[2], corners[6], color, duration, depthTest);

            Debug.DrawLine(corners[3], corners[7], color, duration, depthTest);

            Debug.DrawLine(corners[4], corners[5], color, duration, depthTest);
            Debug.DrawLine(corners[4], corners[6], color, duration, depthTest);

            Debug.DrawLine(corners[5], corners[7], color, duration, depthTest);

            Debug.DrawLine(corners[6], corners[7], color, duration, depthTest);
        }
    }
}