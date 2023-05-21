using OpenTK.Mathematics;

namespace Raytracing
{
    public struct IntersectData
    {
        public Vector3 Color;
        public float distance;
        public Primitive? intersectedPrimitive;
        public bool isLight;

        public IntersectData(Vector3 color, float distance, Primitive? intersectedPrimitive, bool isLight = false)
        {
            Color = color;
            this.distance = distance;
            this.intersectedPrimitive = intersectedPrimitive;
            this.isLight = isLight;
        }
    }
}
