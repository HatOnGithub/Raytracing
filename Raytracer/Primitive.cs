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
        public float reflectiveness = 0.5f;
        public float specularity = 10;
        public Vector3 ambientColor { get { return Scene.AmbientLight; } }

        public Primitive(Vector3 position, Vector3 color, MaterialType materialType, float reflectiveness = 0.5f)
        {
            Position = position;
            Color = color;
            this.materialType = materialType;
            this.reflectiveness = reflectiveness;
        }

        public abstract IntersectData Intersect(Ray ray);

        public abstract Vector3 Normal(Vector3 position);

    }

    public class Sphere : Primitive
    {
        public float radius;
        public Sphere(Vector3 position, Vector3 color, MaterialType materialType, float radius, float reflectiveness = 0.5f):
            base(position, color, materialType, reflectiveness)
        {
            this.radius = radius;
        }

        public override IntersectData Intersect(Ray ray)
        {
            // quadratic formula

            float intersectDistance = float.NegativeInfinity;
            Primitive? nullablePrimitive = null;
            Vector3 returnColor = ambientColor;

            float a = ray.Direction.LengthSquared;
            float b = Vector3.Dot(ray.Direction, ray.Origin - Position);
            float c = (ray.Origin - Position).LengthSquared - (radius * radius);
            float discriminant = (b * b) - (a * c);
            intersectDistance = MathF.Min(MathF.Max(0, (-b + MathF.Sqrt(discriminant)) / a), MathF.Max(0, (-b - MathF.Sqrt(discriminant)) / a));

            if (discriminant >= 0 && intersectDistance > 0)
            {
                returnColor = Color;
                nullablePrimitive = this;
            }

            return new(returnColor, intersectDistance, nullablePrimitive);
        }

        public override Vector3 Normal(Vector3 intersectPoint)
        {
            return Vector3.Normalize(intersectPoint - Position);
        }
    }

    public class Plane : Primitive
    {
        public Vector3 NormalVector;

        public Plane(Vector3 Position, Vector3 Normal, Vector3 Color, MaterialType materialType, float reflectiveness = 0.5f):
            base(Position, Color, materialType, reflectiveness)
        {
            NormalVector = Vector3.Normalize(Normal);
        }

        public override IntersectData Intersect(Ray ray)
        {
            Vector3 p0 = Position;
            Vector3 r0 = ray.Origin;
            Vector3 r = ray.Direction;
            Vector3 n = NormalVector;

            float numerator = Vector3.Dot(p0 - r0, n);
            float denominator = Vector3.Dot(r, n);
            float intersectDistance;

            if (numerator / denominator < 0 || denominator == 0 || Vector3.Dot(r, n) > 0) intersectDistance = float.NegativeInfinity;
            else intersectDistance = numerator / denominator;

            if (intersectDistance > 0) return new(Color, intersectDistance, this);
            return new(ambientColor, intersectDistance, null);
        }

        public override Vector3 Normal(Vector3 position) { return NormalVector; } // position irrelevant
    }
}
