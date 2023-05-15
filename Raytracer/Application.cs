namespace Raytracing
{
    using OpenTK.Mathematics;
    using System.Drawing;
    using OpenTK;
    using OpenTK.Windowing.GraphicsLibraryFramework;
    using OpenTK.Windowing.Common;

    class Application
    {
        // member variables
        public Surface screen;
        public Surface debug;
        public Raytracer raytracer;
        public KeyboardState keyboardState;

        // units per second
        public float movementspeed = 1;

        // degrees/s
        public float yawSpeed = 5;
        public float pitchSpeed = 5;
        public float rollSpeed = 5;
        public float fovChangeSpeed = 5;

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
        public void Tick(FrameEventArgs e, KeyboardState keyboard)
        {
            keyboardState = keyboard;

            HandleInput(e, keyboard);

            raytracer.Render();
        }

        public void HandleInput(FrameEventArgs e, KeyboardState keyboard)
        {
            Vector3 movement = new();
            float pitch = 0;
            float yaw = 0;
            float roll = 0;
            float fovChange = 1;

            if (keyboard[Keys.W]) movement.X += 1;
            if (keyboard[Keys.A]) movement.Z -= 1;
            if (keyboard[Keys.S]) movement.X -= 1;
            if (keyboard[Keys.D]) movement.Z += 1;
            if (keyboard[Keys.Space]) movement.Y += 1;
            if (keyboard[Keys.LeftShift]) movement.Y -= 1;

            if (keyboard[Keys.Q]) yaw -= 1;
            if (keyboard[Keys.E]) yaw += 1;

            if (keyboard[Keys.R]) pitch -= 1;
            if (keyboard[Keys.F]) pitch += 1;

            if (keyboard[Keys.Z]) roll += 1;
            if (keyboard[Keys.X]) roll -= 1;

            if (keyboard[Keys.T]) fovChange += 1;
            if (keyboard[Keys.G]) fovChange -= 1;

            raytracer.MoveCamera(
                movement * movementspeed * (float)e.Time, 
                pitch * pitchSpeed * (float)e.Time, 
                yaw * yawSpeed * (float)e.Time, 
                roll * rollSpeed * (float)e.Time, 
                fovChange * fovChangeSpeed * (float)e.Time);
        }

        public static int RGBtoINT(int r, int g, int b) { return (b << 16) + (g << 8) + r; }

        public static int RGBtoINT(Color color) { return (color.B << 16) + (color.G << 8) + color.R; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(vector.Z * 255) << 16) + ((int)(vector.Y * 255) << 8) + (int)(vector.X * 255); }
    }
}