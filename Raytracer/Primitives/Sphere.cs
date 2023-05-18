using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Raytracing
{
    public class Sphere : Primitive
    {
        public float radius;

        public Sphere(Vector3 position, Vector3 Color, MaterialType materialType, float radius, float reflectiveness = 0.5f, float refractiveIndex = 1.6f) :
            base(position, Color, materialType, reflectiveness)
        {
            this.radius = radius;
            this.refractiveIndex = refractiveIndex;
            Up = new(0, 1, 0);
            Direction = new(1, 0, 0);
        }

        public Sphere(Vector3 position, string path, MaterialType materialType, float radius, float reflectiveness = 0.5f, float refractiveIndex = 1.6f) :
            base(position, path, materialType, reflectiveness)
        {
            this.radius = radius;
            this.refractiveIndex = refractiveIndex;
            Up = new(0, 1, 0);
            Direction = new(1, 0, 0);
        }

        public override IntersectData Intersect(Ray ray)
        {
            // quadratic formula

            float intersectDistance = float.NegativeInfinity;
            Primitive? nullablePrimitive = null;
            Vector3 returnColor = ambientColor;


            float a = ray.Direction.LengthSquared;
            float b = Vector3.Dot(ray.Direction, ray.Origin - Position);
            float c = (ray.Origin - Position).LengthSquared - radius * radius;
            float discriminant = b * b - a * c;

            intersectDistance = MathF.Min(MathF.Max(0, (-b + MathF.Sqrt(discriminant)) / a), MathF.Max(0, (-b - MathF.Sqrt(discriminant)) / a));

            if (discriminant >= 0 && intersectDistance > 0)
            {
                returnColor = GetColorFromTextureAtIntersect(ray.Origin + ray.Direction * intersectDistance);
                nullablePrimitive = this;
            }

            return new(returnColor, intersectDistance, nullablePrimitive);
        }

        public override Vector3 Normal(Vector3 intersectPoint)
        {
            return Vector3.NormalizeFast(intersectPoint - Position);
        }

        public override Vector3 GetColorFromTextureAtIntersect(Vector3 IntersectPoint)
        {
            int x, y;

            Vector3 normal = Normal(IntersectPoint);
            float xnormal = (normal.X + 1) / 2;
            if (normal.Z < 0) xnormal = 4 - xnormal;
            x = (int)((Texture.GetLength(0) - 1) * (xnormal / 4));
            y = (int)((Texture.GetLength(1) - 1) * -((normal.Y - 1) / 2));

            return Texture[x, y];
        }
    }
}
