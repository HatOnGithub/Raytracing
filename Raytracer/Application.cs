namespace Raytracing
{
    using OpenTK.Mathematics;
    using System.Drawing;
    using OpenTK;

    class Application
    {
        // member variables
        public Surface screen;
        public Surface debug;
        public Raytracer raytracer;
        // constructor
        public Application(Surface screen, Surface debug)
        {
            this.screen = screen;
            this.debug = debug;
            raytracer = new(screen, debug);
        }
        // initialize
        public void Init()
        {

        }
        // tick: renders one frame
        public void Tick()
        {
            screen.Clear(RGBtoINT(0,0,0));
            debug.Clear(RGBtoINT(0,0,0));

            raytracer.Render();
        }

        public static int RGBtoINT(int r, int g, int b) { return (b << 16) + (g << 8) + r; }

        public static int RGBtoINT(Color color) { return (color.B << 16) + (color.G << 8) + color.R; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(vector.Z * 255) << 16) + ((int)(vector.Y * 255) << 8) + (int)(vector.X * 255); }
    }
}