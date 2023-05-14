using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;

namespace Raytracing
{
    public class Light
    {
        public Vector3 Position, Color;
        Vector3 originalColor;
        public int iColor
        {
            get { return Application.RGBtoINT(Color); }
        }
        public float Intensity { get { return Color.Length; } set { Color = originalColor * value; } }

        public Light(Vector3 Position, Vector3 Color, float Intensity)
        {
            this.Position = Position;
            this.Color = Color;
            this.originalColor= this.Color;
            this.Intensity = Intensity;
        }
    }
}
