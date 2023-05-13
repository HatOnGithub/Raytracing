using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raytracing
{
    public class Camera
    {
        Surface screen;

        public float distanceToPlane;
        public float CurrentFOV { get { return (float)Math.Atan2(screen.width / 2, distanceToPlane); } }
        public Vector3 Position { get; set; }
        public Vector3 Up { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Right { get { return Vector3.Cross(Direction, Up); } }
        public Vector3 PlaneCenter { get { return Position + distanceToPlane * Direction; } }
        public Vector3 p0 => PlaneCenter + (screen.height/2 * Up) - (screen.width/2 * Right);
        public Vector3 p1 =>  PlaneCenter + (screen.height/2 * Up) + (screen.width/2 * Right); 
        public Vector3 p2 => PlaneCenter - (screen.height/2 * Up) - (screen.width/2 * Right);
        public Vector3 PlaneX => p1 - p0;
        public Vector3 PlaneY => p2 - p0;


        public void SetFOV(float degrees) { float radians = (float)(Math.Clamp(degrees, 60, 120) * Math.PI/180); distanceToPlane = (float)(screen.width/2 / Math.Tan(radians)); }
        public void ChangeFOV(float degrees) { float newAngle = (float)(Math.Clamp(CurrentFOV + degrees, 60, 120) * Math.PI / 180); SetFOV(newAngle); }
        public Vector3 Normalise(Vector3 unNormalised) { Vector3 hat = unNormalised; hat.Normalize(); return hat; }

        public Camera(Surface screen, Vector3 Position, Vector3 Direction, Vector3 Up, float degrees)
        {
            this.screen = screen;
            this.Position = Position;
            this.Direction = Direction;
            this.Up = Up;
            SetFOV(degrees);
        }

        public Ray GetRayForPixelAt(int x, int y) { return new Ray(Position, Normalise((x * PlaneX + y * PlaneY) - Position)); }


    }
}
