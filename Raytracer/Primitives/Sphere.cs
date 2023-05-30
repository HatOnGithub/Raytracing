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

        public Sphere(Vector3 position, string imagePath, MaterialType materialType, float radius, float reflectiveness = 0.5f, float refractiveIndex = 1.6f) :
            base(position, imagePath, materialType, reflectiveness)
        {
            this.radius = radius;
            this.refractiveIndex = refractiveIndex;
            Up = new(0, 1, 0);
            Direction = new(1, 0, 0);
        }

        // algorithm derived from slides and Wikipedia
        public override IntersectData Intersect(Ray ray)
        {
            // quadratic formula

            float intersectDistance = float.NegativeInfinity;
            Primitive? nullablePrimitive = null;
            Vector3 returnColor = ambientColor;


            float
                a = ray.Direction.LengthSquared,
                b = 2 * Vector3.Dot(ray.Direction, ray.Origin - Position),
                c = (ray.Origin - Position).LengthSquared - (radius * radius),
                discriminant = (b * b) - (4 * a * c),
                plus = (-b + MathF.Sqrt(discriminant)) / (2 * a),
                minus = (-b - MathF.Sqrt(discriminant)) / (2 * a);

            if (discriminant < 0) return new(returnColor, intersectDistance, nullablePrimitive);

            if ((intersectDistance = minus < plus && minus > 0 ? minus : plus) > 0) return new(GetColorFromTextureAtIntersect(ray.Origin + ray.Direction * intersectDistance), intersectDistance, this);

            return new(ambientColor, float.NegativeInfinity, null);
        }

        public override Vector3 Normal(Vector3 intersectPoint) { return Vector3.NormalizeFast(intersectPoint - Position); }

        public override Vector3 GetColorFromTextureAtIntersect(Vector3 IntersectPoint)
        {
            Vector3 normal = Normal(IntersectPoint);
            return Texture[(int)Math.Round((Texture.GetLength(0) - 1) * (MathF.Atan2(normal.X, normal.Z) / (2 * MathF.PI) + 0.5f)), (int)Math.Round((Texture.GetLength(1) - 1) * -((normal.Y - 1) / 2))];
        }
    }
}
