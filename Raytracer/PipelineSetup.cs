using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
namespace Raytracing
{
    public class OpenTKApp : GameWindow
    {

        int screenID;            // unique integer identifier of the OpenGL texture
        int debugID;
        Application? app;      // instance of the application
        bool terminated = false; // application terminates gracefully when this is true

        // The following variables are only needed in Modern OpenGL
        public int VAOmain;
        public int VBOmain;
        public int programID;
        // All the data for the verticesMain interleaved in one array:
        // - XYZ in normalized device coordinates
        // - UV
        readonly float[] verticesMain =
        { //  X      Y     Z     U     V
            -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, // bottom-left  2-----3 triangles:
             0f  , -1.0f, 0.0f, 1.0f, 1.0f, // bottom-right | \   |     012
            -1.0f,  1.0f, 0.0f, 0.0f, 0.0f, // top-left     |   \ |     123
             0f  ,  1.0f, 0.0f, 1.0f, 0.0f, // top-right    0-----1
        };

        // The following variables are only needed in Modern OpenGL
        public int VAOdebug;
        public int VBOdebug;
        // All the data for the verticesDebug interleaved in one array:
        // - XYZ in normalized device coordinates
        // - UV
        readonly float[] verticesdebug =
        { //  X      Y     Z     U     V
             0f  , -1.0f, 0.0f, 0.0f, 1.0f, // bottom-left  2-----3 triangles:
             1.0f, -1.0f, 0.0f, 1.0f, 1.0f, // bottom-right | \   |     012
             0f  ,  1.0f, 0.0f, 0.0f, 0.0f, // top-left     |   \ |     123
             1.0f,  1.0f, 0.0f, 1.0f, 0.0f, // top-right    0-----1
        };


        public OpenTKApp()
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = new Vector2i(1800, 500)
            })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            // called during application initialization
            GL.ClearColor(0, 0, 0, 0);
            GL.Disable(EnableCap.DepthTest);
            Surface screen = new(ClientSize.X/2, ClientSize.Y);
            Surface debug = new(ClientSize.X/2, ClientSize.Y);
            app = new Application(screen, debug);
            screenID = app.screen.GenTexture();
            debugID = app.debug.GenTexture();

            SetupVAO(ref VAOmain, ref VBOmain, verticesMain);
            SetupVAO(ref VAOdebug, ref VAOdebug, verticesdebug);

            
            app.Init();
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            // called upon app close
            GL.DeleteTextures(1, ref screenID);
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            // called upon window resize. Note: does not change the size of the pixel buffer.
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            // called once per frame; app logic
            var keyboard = KeyboardState;
            if (keyboard[Keys.Escape]) terminated = true;
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            // called once per frame; render
            app?.Tick();

            if (terminated)
            {
                Close();
                return;
            }
            // convert Application.screen to OpenGL texture
            if (app != null)
            {
                GL.BindTexture(TextureTarget.Texture2D, screenID);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                               app.screen.width, app.screen.height, 0,
                               PixelFormat.Bgra,
                               PixelType.UnsignedByte, app.screen.pixels
                             );
                // draw screen filling quad

                GL.BindVertexArray(VAOmain);
                GL.UseProgram(programID);
                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

                // generate new texture and bind to debug screen
                GL.BindTexture(TextureTarget.Texture2D, debugID);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                               app.debug.width, app.debug.height, 0,
                               PixelFormat.Bgra,
                               PixelType.UnsignedByte, app.debug.pixels
                             );
                // draw screen filling quad

                // switch VAO (and VBO) and draw debug screen
                GL.BindVertexArray(VAOdebug);
                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            }
            // tell OpenTK we're done rendering
            SwapBuffers();
        }

        public void SetupVAO( ref int VAO, ref int VBO, float[] vertices)
        {
            // setting up a Modern OpenGL pipeline takes a lot of code
            // Vertex Array Object: will store the meaning of the data in the buffer
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // Vertex Buffer Object: a buffer of raw data
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Vertex Shader
            string shaderSource = File.ReadAllText("../../../shaders/screen_vs.glsl");
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, shaderSource);
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int status);
            if (status != (int)All.True)
            {
                string log = GL.GetShaderInfoLog(vertexShader);
                throw new Exception($"Error occurred whilst compiling vertex shader ({vertexShader}):\n{log}");
            }
            // Fragment Shader
            shaderSource = File.ReadAllText("../../../shaders/screen_fs.glsl");
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, shaderSource);
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
            if (status != (int)All.True)
            {
                string log = GL.GetShaderInfoLog(fragmentShader);
                throw new Exception($"Error occurred whilst compiling fragment shader ({fragmentShader}):\n{log}");
            }
            // Program: a set of shaders to be used together in a pipeline
            programID = GL.CreateProgram();
            GL.AttachShader(programID, vertexShader);
            GL.AttachShader(programID, fragmentShader);
            GL.LinkProgram(programID);
            GL.GetProgram(programID, GetProgramParameterName.LinkStatus, out status);
            if (status != (int)All.True)
            {
                string log = GL.GetProgramInfoLog(programID);
                throw new Exception($"Error occurred whilst linking program ({programID}):\n{log}");
            }
            // the program contains the compiled shaders, we can delete the source
            GL.DetachShader(programID, vertexShader);
            GL.DetachShader(programID, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            // send all the following draw calls through this pipeline
            GL.UseProgram(programID);
            // tell the VAOmain which part of the VAOmain data should go to each shader input
            int location = GL.GetAttribLocation(programID, "vPosition");
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            location = GL.GetAttribLocation(programID, "vUV");
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            // connect the texture to the shader uniform variable
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, screenID);
            GL.Uniform1(GL.GetUniformLocation(programID, "pixels"), 0);
        }

        public static void Main()
        {
            // entry point
            using OpenTKApp app = new();
            app.RenderFrequency = 144.0;
            app.Run();
        }
    }
}