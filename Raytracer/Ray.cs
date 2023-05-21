using OpenTK.Mathematics;

namespace Raytracing
{
    public class Ray
    {
        public Vector3 Origin, Direction;
        public float intersectDistance = 0;
        public char raytype;
        public int bouncesLeft;
        public bool sendToDebug;
        public int lifetime = 5;


        public Ray(Vector3 origin, Vector3 direction, int bouncesLeft, char raytype = 'p', bool sendToDebug = false)
        {
            Origin = origin;
            Direction = Vector3.NormalizeFast(direction);
            this.raytype = raytype;
            this.bouncesLeft = bouncesLeft;
            this.sendToDebug = sendToDebug;
        }
    }
}
