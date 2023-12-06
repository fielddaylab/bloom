using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using UnityEngine;

namespace Zavala {
    static public class HullGeneration {

        // TODO: quickhull? graham scan?

        // very basic rect hull
        static public unsafe bool ComputeFastRect(Vector2* points, int pointCount, out Rect output) {
            if (pointCount < 3) {
                output = default;
                return false;
            }

            Vector2 minX, minY, maxX, maxY;
            minX = minY = maxX = maxY = points[0];

            for (int i = 1; i < pointCount; i++) {
                Vector2 p = points[i];

                if (p.x < minX.x) {
                    minX = p;
                } else if (p.x > maxX.x) {
                    maxX = p;
                }

                if (p.y < minY.y) {
                    minY = p;
                } else if (p.y > maxY.y) {
                    maxY = p;
                }
            }

            output = Rect.MinMaxRect(minX.x, minY.y, maxX.x, maxY.y);
            return true;
        }
    }
}