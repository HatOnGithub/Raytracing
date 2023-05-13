namespace Raytracing
{
    using OpenTK.Mathematics;
    using System.Drawing;

    class Application
    {
        // member variables
        public Surface screen;
        public Surface debug;
        // constructor
        public Application(Surface screen, Surface debug)
        {
            this.screen = screen;
            this.debug = debug;
        }
        // initialize
        public void Init()
        {

        }
        // tick: renders one frame
        public void Tick()
        {
            screen.Clear(RGBtoINT(255,100,0));
            debug.Clear(RGBtoINT(0,50,255));
        }

        public int RGBtoINT(int r, int g, int b) { return (b << 16) + (g << 8) + r; }

        public int RGBtoINT(Color color) { return (color.B << 16) + (color.G << 8) + color.R; }
    }
}