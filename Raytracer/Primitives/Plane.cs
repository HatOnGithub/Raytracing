using OpenTK.Mathematics;

namespace Raytracing
{
    public class Plane : Primitive
    {
        public Vector3 NormalVector;
        public Vector2 TextureDimensions;

        public Plane(Vector3 Position, Vector3 Normal, Vector3 Color, Vector2 TextureDimensions, MaterialType materialType, float reflectiveness = 0.5f) :
            base(Position, Color, materialType, reflectiveness)
        {
            NormalVector = Vector3.Normalize(Normal);
            this.TextureDimensions = TextureDimensions;


            Direction = Vector3.Normalize(Vector3.Cross(new(0, 1, 0), NormalVector));
            if (float.IsNaN(Direction.X) || Direction == Vector3.Zero)
                Direction = Vector3.Normalize(Vector3.Cross(new(1, 0, 0), NormalVector));
            Up = Vector3.Normalize(Vector3.Cross(NormalVector, Direction));
            if (float.IsNaN(Up.X) || Up == Vector3.Zero)
                Up = Vector3.Normalize(Vector3.Cross(-NormalVector, Direction));
        }
        public Plane(Vector3 Position, Vector3 Normal, string imagePath, Vector2 TextureDimensions, MaterialType materialType, float reflectiveness = 0.5f) :
            base(Position, imagePath, materialType, reflectiveness)
        {
            NormalVector = Vector3.Normalize(Normal);
            this.TextureDimensions = TextureDimensions;


            Direction = Vector3.Normalize(Vector3.Cross(new(0, 1, 0), NormalVector));
            if (float.IsNaN(Direction.X) || Direction == Vector3.Zero)
                Direction = Vector3.Normalize(Vector3.Cross(new(1, 0, 0), NormalVector));
            Up = Vector3.Normalize(Vector3.Cross(NormalVector, Direction));
            if (float.IsNaN(Up.X) || Up == Vector3.Zero)
                Up = Vector3.Normalize(Vector3.Cross(-NormalVector, Direction));
        }

        public override IntersectData Intersect(Ray ray)
        {
            // formulae from Wikipedia (line-Plane intersections)

            Vector3 p0 = Position;
            Vector3 l0 = ray.Origin;
            Vector3 l = ray.Direction;
            Vector3 n = NormalVector;

            if (Vector3.Dot(ray.Direction, n) > 0) n *= -1;

            float numerator = Vector3.Dot(p0 - l0, n);
            float denominator = Vector3.Dot(l, n);
            float division = numerator / denominator;
            float intersectDistance = float.NegativeInfinity;

            if (!(division < 0 || denominator == 0 || numerator == 0 || Vector3.Dot(l, n) > 0)) intersectDistance = division;

            if (intersectDistance > 0) return new(GetColorFromTextureAtIntersect(ray.Origin + ray.Direction * intersectDistance), intersectDistance, this);
            return new(ambientColor, intersectDistance, null);
        }

        public override Vector3 Normal(Vector3 position) { return NormalVector; } // position irrelevant

        public override Vector3 GetColorFromTextureAtIntersect(Vector3 IntersectPoint)
        {
            // tiles the plane regularly according to the given size a texture should be on the plane in meters
            double 
                x = (Vector3.Dot(IntersectPoint - Position, Direction) % TextureDimensions.X) / TextureDimensions.X, 
                y = (Vector3.Dot(IntersectPoint - Position, Up) % TextureDimensions.Y) / TextureDimensions.Y;

            if (x < 0) x += 1;
            if (y < 0) y += 1;

            return Texture[(int)(x * (Texture.GetLength(0) - 1)), (int)((1 - y) * (Texture.GetLength(1) - 1))];
        }
    }
}
