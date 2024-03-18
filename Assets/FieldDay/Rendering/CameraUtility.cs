using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Rendering {
    /// <summary>
    /// Camera utility functions.
    /// </summary>
    static public class CameraUtility {
        static private readonly Camera[] s_CameraWorkArray = new Camera[32];

        /// <summary>
        /// Finds the most specific camera that renders the given layer.
        /// </summary>
        static public Camera FindMostSpecificCameraForLayer(int layer, bool includeInactive = true) {
            int cameraCount = Camera.GetAllCameras(s_CameraWorkArray);

            Camera found = null;
            int mostSpecificBitCount = int.MaxValue;

            layer = (1 << layer);

            for (int i = 0; i < cameraCount; ++i) {
                Camera cam = s_CameraWorkArray[i];
                if (!includeInactive && !cam.isActiveAndEnabled)
                    continue;

                int camCullingMask = cam.cullingMask;

                if ((camCullingMask & layer) == layer) {
                    int bitCount = Bits.Count(camCullingMask);
                    if (bitCount < mostSpecificBitCount) {
                        found = cam;
                        mostSpecificBitCount = bitCount;
                    }
                }
            }

            Array.Clear(s_CameraWorkArray, 0, cameraCount);
            return found;
        }
    }
}