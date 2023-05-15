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
        float aspectRatio;
        public float CurrentFOV { get { return (float)Math.Atan2(screen.width / 2, distanceToPlane); } }
        public Vector3 Position { get; set; }
        public Vector3 Up { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Right;
        public Vector3 PlaneCenter;
        public Vector3 p0;
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 PlaneX;
        public Vector3 PlaneY;


        public void SetFOV(float degrees) 
        { 
            float radians = (float)(Math.Clamp(degrees, 60, 120) * (Math.PI/180)); 
            distanceToPlane = aspectRatio / MathF.Tan(radians / 2);
            UpdateVectors();
        }
        public void ChangeFOV(float degrees) { float newAngle = (float)(Math.Clamp(CurrentFOV + degrees, 60, 120) * Math.PI / 180); SetFOV(newAngle); }
        public Vector3 Normalise(Vector3 unNormalised) { Vector3 hat = unNormalised; hat.Normalize(); return hat; }

        public Camera(Surface screen, Vector3 Position, Vector3 Direction, Vector3 Up, float degrees)
        {
            this.screen = screen;
            this.Position = Position;
            this.Direction = Vector3.Normalize(Direction);
            aspectRatio = (float)screen.width / (float)screen.height;
            this.Up = Vector3.Normalize(Up);
            SetFOV(degrees);
        }

        public void UpdateVectors()
        {
            Right = Vector3.Cross(Direction, Up);
            PlaneCenter = Position + distanceToPlane * Direction; ;
            p0 = PlaneCenter + Up - (aspectRatio * Right);
            p1 = PlaneCenter + Up + (aspectRatio * Right);
            p2 = PlaneCenter - Up - (aspectRatio * Right);
            PlaneX = p1 - p0;
            PlaneY = p2 - p0;
        }

        public Ray GetRayForPixelAt(float x, float y) 
        { 
            return new Ray(Position, Normalise(p0 + ((x / screen.width) * PlaneX) + ((y / screen.height) * PlaneY) - Position)); 
        }


    }
}
