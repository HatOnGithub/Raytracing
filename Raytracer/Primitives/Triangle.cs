using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Raytracing
{
    public class Triangle : Primitive
    {
        public Vector3[] vertices = new Vector3[3];

        public Triangle(Vector3[] vertices, Vector3 Color, MaterialType materialType, float reflectiveness = 0.5f) :
            base(vertices[0], Color, materialType, reflectiveness)
        {
            this.vertices = vertices;
            Direction = Vector3.Normalize(vertices[2] - vertices[1]);
            Up = Vector3.Normalize(Vector3.Cross(Normal(new()), Direction));
        }
        public Triangle(Vector3[] vertices, string path, MaterialType materialType, float reflectiveness = 0.5f) :
            base(vertices[0], path, materialType, reflectiveness)
        {
            this.vertices = vertices;
            Direction = Vector3.Normalize(vertices[2] - vertices[1]);
            Up = Vector3.Normalize(Vector3.Cross(Normal(new()), Direction));
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

            float intersectDistance = Vector3.Dot(vertices[0] - ray.Origin, normal) / Vector3.Dot(ray.Direction, normal);
            Vector3 planeIntersection = ray.Origin + intersectDistance * ray.Direction;

            if (Vector3.Dot(Vector3.Cross(B - A, planeIntersection - A), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(A - C, planeIntersection - C), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(C - B, planeIntersection - B), normal) >= 0)
                return new(GetColorFromTextureAtIntersect(ray.Origin + ray.Direction * intersectDistance), intersectDistance, this);

            return new(ambientColor, float.NegativeInfinity, null);
        }

        public override Vector3 Normal(Vector3 position)
        {
            return Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        }

        public override Vector3 GetColorFromTextureAtIntersect(Vector3 IntersectPoint)
        {
            int x, y;

            x = (Texture.GetLength(0) - 1) * (int)Math.Round(Vector3.Dot(IntersectPoint - vertices[1], Direction) * (vertices[2] - vertices[1]).Length);
            y = (Texture.GetLength(1) - 1) * (int)Math.Round(Vector3.Dot(IntersectPoint - vertices[1], Up) * Vector3.Dot(vertices[0] - vertices[1], Up));

            return Texture[x, y];
        }
    }
}
