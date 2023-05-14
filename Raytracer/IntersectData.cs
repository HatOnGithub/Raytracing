using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Raytracing
{
    public class IntersectData
    {
        public int iColor { get { return Application.RGBtoINT(Color); } }
        public Vector3 Color;
        public float distance;
        public Primitive? intersectedPrimitive;
        public bool isLight;

        public IntersectData(Vector3 color, float distance, Primitive? intersectedPrimitive,  bool isLight = false)
        {
            Color = color;
            this.distance = distance;
            this.intersectedPrimitive = intersectedPrimitive;
            this.isLight = isLight;
        }
    }
}
