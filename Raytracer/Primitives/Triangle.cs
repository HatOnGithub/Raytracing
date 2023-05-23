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
        public Triangle(Vector3[] vertices, string imagePath, MaterialType materialType, float reflectiveness = 0.5f) :
            base(vertices[0], imagePath, materialType, reflectiveness)
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

            Vector3 normal = Vector3.NormalizeFast(Vector3.Cross(B - A, C - A));

            //if (Vector3.Dot(ray.Direction, normal) > 0) normal *= -1;


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
            return Vector3.NormalizeFast(Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]));
        }

        public override Vector3 GetColorFromTextureAtIntersect(Vector3 IntersectPoint)
        {
            return Vector3.One;

            int x, y;

            x = (int)Math.Round((Texture.GetLength(0) - 1) * (Vector3.Dot(IntersectPoint - vertices[1], Direction) * (vertices[2] - vertices[1]).Length));
            y = (int)Math.Round((Texture.GetLength(1) - 1) * (Vector3.Dot(IntersectPoint - vertices[1], Up) * Vector3.Dot(vertices[0] - vertices[1], Up)));

            return Texture[x, y];
        }
    }
}
