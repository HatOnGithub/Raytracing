namespace Raytracing
{
    using OpenTK.Mathematics;
    using OpenTK.Windowing.Common;
    using OpenTK.Windowing.GraphicsLibraryFramework;
    using System.Drawing;

    class Application
    {
        // member variables
        public Surface screen;
        public Surface debug;
        public Raytracer? raytracer = null;

        // units per second
        public float movementspeed = 1;

        // degrees/s
        public float yawSpeed = 5;
        public float pitchSpeed = 5;
        public float rollSpeed = 5;
        public float fovChangeSpeed = 5;

        public bool showedTextureloading = false;

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
        public void Tick(FrameEventArgs e, KeyboardState keyboard)
        {
            HandleInput(e, keyboard);

            if (raytracer == null)
            {
                if (!showedTextureloading)
                {
                    showedTextureloading = true;
                    screen.Print("Loading Textures, give it a moment :)", 20, 20, 0xffffff);
                    return;
                }
                raytracer = new(screen, debug);
            }

            raytracer.Render();
        }

        public void HandleInput(FrameEventArgs e, KeyboardState keyboard)
        {
            Vector3 movement = new();
            float pitch = 0;
            float yaw = 0;
            float roll = 0;
            float fovChange = 0;

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

            raytracer?.camera.MoveCamera(
                movement * movementspeed * (float)e.Time,
                pitch * pitchSpeed * (float)e.Time,
                yaw * yawSpeed * (float)e.Time,
                roll * rollSpeed * (float)e.Time,
                fovChange * fovChangeSpeed * (float)e.Time);


            float raysPerSecond = 2 * (float)e.Time;
            if (keyboard[Keys.Right]) raytracer.raysShownf += raysPerSecond;
            if (keyboard[Keys.Left]) raytracer.raysShownf = MathF.Max(2, raytracer.raysShownf - raysPerSecond);

            float mtopxchange = 10 * (float)e.Time;
            if (keyboard[Keys.Up]) raytracer.M_to_Px += mtopxchange;
            if (keyboard[Keys.Down]) raytracer.M_to_Px = MathF.Max(0, raytracer.M_to_Px - mtopxchange);
        }

        public static int RGBtoINT(int r, int g, int b) { return (b << 16) + (g << 8) + r; }

        public static int RGBtoINT(Color color) { return (color.B << 16) + (color.G << 8) + color.R; }

        public static int RGBtoINT(Vector3 vector) { return ((int)(vector.Z * 255) << 16) + ((int)(vector.Y * 255) << 8) + (int)(vector.X * 255); }
    }
}