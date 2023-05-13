using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raytracing
{
    public enum MaterialType
    {
        Diffuse,
        Gloss,
        Mirror
    }


    public abstract class Primitive
    {
        
        public Vector3 Position;
        public Vector3 Color;
        public MaterialType materialType;

        public Primitive(Vector3 position, Vector3 color, MaterialType materialType)
        {
            Position = position;
            Color = color;
            this.materialType = materialType;
        }

        public abstract IntersectData Intersect(Ray ray);
    }
}
