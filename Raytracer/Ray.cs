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
        public float intersectDistance = float.NegativeInfinity;
        public string raytype;

        public Ray(Vector3 origin, Vector3 direction, string raytype = "p")
        {
            Origin = origin;
            Direction = Vector3.NormalizeFast(direction);
            this.raytype = raytype;
        }
    }
}
