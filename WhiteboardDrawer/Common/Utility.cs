using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WhiteboardDrawer.Common
{
    public static class Utility
    {
        // Based on http://paulbourke.net/geometry/circlesphere/tvoght.c
        // Forgot how to do this, it's been since high school :P
        public static bool CircleCircleIntersection(Vector centreA, Vector centreB, double radiusA, double radiusB, out Vector outPointA, out Vector outPointB)
        {
            var diff = centreB - centreA;
            var dist = diff.Length;

            // No solutions.
            if (dist > (radiusA + radiusB))
            {
                outPointA = new Vector(0.0, 0.0);
                outPointB = new Vector(0.0, 0.0);
                return false;
            }

            // Infinite solutions.
            if (dist < Math.Abs(radiusA - radiusB))
            {
                outPointA = new Vector(0.0, 0.0);
                outPointB = new Vector(0.0, 0.0);
                return false;
            }

            var a = ((radiusA * radiusA) - (radiusB * radiusB) + (dist * dist)) / (2.0 * dist);
            var pointC = centreA + (diff * (a / dist));
            var h = Math.Sqrt((radiusA * radiusA) - (a * a));
            var r = new Vector(-diff.Y * (h / dist), diff.X * (h / dist));
            outPointA = pointC + r;
            outPointB = pointC - r;
            return true;
        }
        
    }
}
