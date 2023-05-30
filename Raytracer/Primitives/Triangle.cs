using OpenTK.Mathematics;

namespace Raytracing
{
    public class Triangle : Primitive
    {
        public Vector3[] vertices = new Vector3[3];
        public float Area;

        public Triangle(Vector3[] vertices, Vector3 Color, MaterialType materialType, float reflectiveness = 0.5f) :
            base(vertices[0], Color, materialType, reflectiveness)
        {
            this.vertices = vertices;
            Vector3 AB = vertices[1] - vertices[0];
            Vector3 AC = vertices[2] - vertices[0];
            Area = Area = Vector3.Dot(Normal(new()), Vector3.Cross(AB, AC));
        }
        public Triangle(Vector3[] vertices, string imagePath, MaterialType materialType, float reflectiveness = 0.5f) :
            base(vertices[0], imagePath, materialType, reflectiveness)
        {
            this.vertices = vertices;
            Vector3 AB = vertices[1] - vertices[0];
            Vector3 AC = vertices[2] - vertices[0];
            Area = Vector3.Dot(Normal(new()), Vector3.Cross(AB, AC));
        }

        public override IntersectData Intersect(Ray ray)
        {
            Vector3 A = vertices[0];
            Vector3 B = vertices[1];
            Vector3 C = vertices[2];

            Vector3 normal = Vector3.NormalizeFast(Vector3.Cross(B - A, C - A));

            if (Vector3.Dot(ray.Direction, normal) > 0)
            {
                normal *= -1;
                B = C;
                C = vertices[1];
            }

            float intersectDistance = Vector3.Dot(vertices[0] - ray.Origin, normal) / Vector3.Dot(ray.Direction, normal);

            Vector3 planeIntersection = ray.Origin + intersectDistance * ray.Direction;

            if (Vector3.Dot(Vector3.Cross(B - A, planeIntersection - A), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(A - C, planeIntersection - C), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(C - B, planeIntersection - B), normal) >= 0 &&
                intersectDistance > 0)
                return new(GetColorFromTextureAtIntersect(ray.Origin + ray.Direction * intersectDistance), intersectDistance, this);

            return new(ambientColor, float.NegativeInfinity, null);
        }

        public override Vector3 Normal(Vector3 position)
        {
            return Vector3.NormalizeFast(Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]));
        }

        public override Vector3 GetColorFromTextureAtIntersect(Vector3 P)
        {
            Vector3 A = vertices[0];
            Vector3 B = vertices[1];
            Vector3 C = vertices[2];
            Vector3 n = Normal(new());

            float alpha = (Vector3.Dot(Vector3.Cross(C - B, P - B), n)) / Area;
            float beta = (Vector3.Dot(Vector3.Cross(A - C, P - C), n)) / Area;
            float gamma = (Vector3.Dot(Vector3.Cross(B - A, P - A), n)) / Area;

            Vector2 Auv = new(Texture.GetLength(0), Texture.GetLength(1));
            Vector2 Buv = new(Texture.GetLength(0) / 2, 0);
            Vector2 Cuv = new(0, Texture.GetLength(1));

            Vector2 Puv = (alpha * Auv) + (beta * Buv) + (gamma * Cuv);

            if (Puv.X >= Texture.GetLength(0) || Puv.Y >= Texture.GetLength(1)) return averageColor;

            return Texture[(int)Puv.X, (int)Puv.Y];
        }
    }
}
