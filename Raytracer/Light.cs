using OpenTK.Mathematics;

namespace Raytracing
{
    public class Light
    {
        protected Vector3 position;
        public virtual Vector3 Position(Vector3 position)
        {
            return this.position;
        }
        public Vector3 Color;
        Vector3 originalColor;
        public float Intensity { get { return Color.Length; } set { Color = originalColor * value; } }

        public Light(Vector3 Position, Vector3 Color, float Intensity)
        {
            this.position = Position;
            this.Color = Color;
            originalColor = this.Color;
            this.Intensity = Intensity;
        }
    }

    public class PlaneLight : Light
    {
        public Vector3 normal;
        public override Vector3 Position(Vector3 position)
        {
            Vector3 p0 = this.position;
            Vector3 l0 = position;
            Vector3 l = normal * -1;
            Vector3 n = normal;

            float numerator = Vector3.Dot(p0 - l0, n);
            float denominator = Vector3.Dot(l, n);
            float division = numerator / denominator;
            float intersectDistance;

            if (denominator == 0) intersectDistance = float.NegativeInfinity;
            else intersectDistance = division;

            return position + (normal * -1) * intersectDistance;
        }

        public PlaneLight(Vector3 Position, Vector3 Normal, Vector3 Color, float intensity) :
            base(Position, Color, intensity)
        {
            normal = Normal;
        }
    }
}
