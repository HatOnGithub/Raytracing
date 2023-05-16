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
        DiffuseMirror,
        Mirror,
        Glass
    }



    public abstract class Primitive
    {
        
        public Vector3 Position;
        public Vector3 DiffuseColor;
        public Vector3 SpecularColor = Vector3.One;
        public MaterialType materialType;
        public float reflectiveness = 0.5f;
        public float specularity = 10;
        public Vector3 ambientColor { get { return Scene.AmbientLight; } }

        public Primitive(Vector3 position, Vector3 color, MaterialType materialType, float reflectiveness = 0.5f)
        {
            Position = position;
            DiffuseColor = color;
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
                returnColor = DiffuseColor;
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
            Vector3 l0 = ray.Origin;
            Vector3 l = ray.Direction;
            Vector3 n = NormalVector;

            float numerator = Vector3.Dot(p0 - l0, n);
            float denominator = Vector3.Dot(l, n);
            float division = numerator / denominator; 
            float intersectDistance;

            if (division < 0 || denominator == 0 || numerator == 0 || Vector3.Dot(l, n) > 0) intersectDistance = float.NegativeInfinity;
            else intersectDistance = division;

            if (intersectDistance > 0) return new(DiffuseColor, intersectDistance, this);
            return new(ambientColor, intersectDistance, null);
        }

        public override Vector3 Normal(Vector3 position) { return NormalVector; } // position irrelevant
    }

    public class Triangle : Primitive
    {
        public Vector3[] vertices = new Vector3[3];

        public Triangle(Vector3[] vertices, Vector3 Color, MaterialType materialType, float reflectiveness = 0.5f):
            base(vertices[0], Color, materialType, reflectiveness)
        {
            this.vertices = vertices;
        }

        public override IntersectData Intersect(Ray ray)
        {
            Vector3 A = vertices[0];
            Vector3 B = vertices[1];
            Vector3 C = vertices[2];

            if (Vector3.Dot(ray.Direction, Vector3.Cross(B - A, C - A)) > 0)
            {
                B = vertices[2];
                C = vertices[1];
            }

            Vector3 normal = Vector3.Cross(B - A, C - A);

            float intersectDistance = (Vector3.Dot(vertices[0] - ray.Origin, normal) / Vector3.Dot(ray.Direction, normal));
            Vector3 planeIntersection = ray.Origin + intersectDistance * ray.Direction;

            if (Vector3.Dot(Vector3.Cross(B - A, planeIntersection - A), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(A - C, planeIntersection - C), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(C - B, planeIntersection - B), normal) >= 0) 
                return new(DiffuseColor, intersectDistance, this);

            return new(ambientColor, float.NegativeInfinity, null);
        }

        public override Vector3 Normal(Vector3 position)
        {
            return Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        }
    }
}
