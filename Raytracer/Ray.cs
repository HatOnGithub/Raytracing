using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Raytracing
{
    public class Ray
    {
        public Vector3 Origin, Direction;
        public int bounces;
        public float IntersectDistance;

        public Ray(Vector3 origin, Vector3 direction, int bounces)
        {
            Origin = origin;
            Direction = direction;
            this.bounces = bounces;
        }
    }
}
