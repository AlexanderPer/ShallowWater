using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
//using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Shadertoy
{
    class ShaderWindow : GameWindow
    {
        #region --- Fields ---

        float globalTime = 0.0f, timeSpeed = 1.0f;
        int vertex_shader_object, fragment_shader_object, shader_program;
        int vertex_buffer_object, color_buffer_object, element_buffer_object;

        //Shapes.Shape shape = new Examples.Shapes.Cube();
        Vector3[]  RectVertices = new Vector3[] {   new Vector3(-1.0f, -1.0f,  0.0f), new Vector3( 1.0f, -1.0f,  0.0f),
                                                    new Vector3( 1.0f,  1.0f,  0.0f), new Vector3(-1.0f,  1.0f,  0.0f) };
        int[] RectIndices = new int[] { 0, 1, 2, 2, 3, 0 };
        private static int ColorToRgba32(Color c) { return (int)((c.A << 24) | (c.B << 16) | (c.G << 8) | c.R); }
        int[] RectColors = new int[] { ColorToRgba32(Color.DarkRed), ColorToRgba32(Color.DarkRed), ColorToRgba32(Color.Gold), ColorToRgba32(Color.Gold) };

        #endregion

        #region --- Shaders ---

        private string vertexShaderSource = @"
void main()
{
    gl_FrontColor = gl_Color;
    gl_Position = ftransform();
}";

        // iResolution - viewport resolution (in pixels)
        // iGlobalTime - shader playback time (in seconds)
        private string fragmentShaderPrefix = @"
#version 120
uniform vec3 iResolution;
uniform float iGlobalTime;
";
        
        /*public static string FragmentShaderSource = @"
void main()
{
    gl_FragColor = gl_Color;
}";*/
        public static string FragmentShaderExample = @"
void main(void)
{
	vec2 uv = gl_FragCoord.xy / iResolution.xy;
	gl_FragColor = vec4(uv,0.5+0.5*sin(iGlobalTime),1.0);
}";
        public static string[] FragmentShaderSource;
        
        
        #endregion

        #region --- Constructors ---

        //public ShaderWindow() : base(800, 600, GraphicsMode.Default)
        public ShaderWindow() : base(512, 288, GraphicsMode.Default)
        { }

        #endregion

        #region OnLoad

        /// <summary>
        /// This is the place to load resources that change little
        /// during the lifetime of the GameWindow. In this case, we
        /// check for GLSL support, and load the shaders.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Check for necessary capabilities:
            Version version = new Version(GL.GetString(StringName.Version).Substring(0, 3));
            Version target = new Version(2, 0);
            if (version < target)
            {
                throw new NotSupportedException(String.Format(
                    "OpenGL {0} is required (you only have {1}).", target, version));
            }

            GL.ClearColor(Color.MidnightBlue);
            GL.Enable(EnableCap.DepthTest);

            CreateVBO();

            //using (StreamReader vs = new StreamReader("Data/Shaders/Simple_VS.glsl"))
            //using (StreamReader fs = new StreamReader("Data/Shaders/Simple_FS.glsl"))
            string fragmentShader = GetFragmentShaderStr(FragmentShaderSource);
            CreateShaders(vertexShaderSource, fragmentShaderPrefix + fragmentShader, out vertex_shader_object, out fragment_shader_object, out shader_program);
        }

        private string GetFragmentShaderStr(string[] shaderSource)
        {
            string sum = "";
            foreach (string line in shaderSource)
            {
                int i = line.IndexOf("//");
                string newLine = i > -1 ? line.Remove(i) : line;
                sum += newLine + "\n";
            }
            return sum;
        }

        #endregion

        #region CreateShaders

        void CreateShaders(string vs, string fs, out int vertexObject, out int fragmentObject, out int program)
        {
            int status_code;
            string info;

            vertexObject = GL.CreateShader(ShaderType.VertexShader);
            fragmentObject = GL.CreateShader(ShaderType.FragmentShader);

            // Compile vertex shader
            GL.ShaderSource(vertexObject, vs);
            GL.CompileShader(vertexObject);
            GL.GetShaderInfoLog(vertexObject, out info);
            GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // Compile vertex shader
            GL.ShaderSource(fragmentObject, fs);
            GL.CompileShader(fragmentObject);
            GL.GetShaderInfoLog(fragmentObject, out info);
            GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);
            
            if (status_code != 1)
                throw new ApplicationException(info);

            program = GL.CreateProgram();
            GL.AttachShader(program, fragmentObject);
            GL.AttachShader(program, vertexObject);

            GL.LinkProgram(program);
            GL.UseProgram(program);
        }

        #endregion

        #region private void CreateVBO()

        void CreateVBO()
        {
            int size;

            GL.GenBuffers(1, out vertex_buffer_object);
            GL.GenBuffers(1, out color_buffer_object);
            GL.GenBuffers(1, out element_buffer_object);

            // Upload the vertex buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertex_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(RectVertices.Length * 3 * sizeof(float)), RectVertices,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != RectVertices.Length * 3 * sizeof(Single))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (vertices). Tried to upload {0} bytes, uploaded {1}.",
                    RectVertices.Length * 3 * sizeof(Single), size));

            // Upload the color buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, color_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(RectColors.Length * sizeof(int)), RectColors,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != RectColors.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (colors). Tried to upload {0} bytes, uploaded {1}.",
                    RectColors.Length * sizeof(int), size));
            
            // Upload the index buffer (elements inside the vertex buffer, not color indices as per the IndexPointer function!)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, element_buffer_object);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(RectIndices.Length * sizeof(Int32)), RectIndices,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != RectIndices.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (offsets). Tried to upload {0} bytes, uploaded {1}.",
                    RectIndices.Length * sizeof(int), size));
        }

        #endregion

        #region OnUnload

        protected override void OnUnload(EventArgs e)
        {
            if (shader_program != 0)
                GL.DeleteProgram(shader_program);
            if (fragment_shader_object != 0)
                GL.DeleteShader(fragment_shader_object);
            if (vertex_shader_object != 0)
                GL.DeleteShader(vertex_shader_object);
            if (vertex_buffer_object != 0)
                GL.DeleteBuffers(1, ref vertex_buffer_object);
            if (element_buffer_object != 0)
                GL.DeleteBuffers(1, ref element_buffer_object);
        }

        #endregion

        #region OnResize

        /// <summary>
        /// Called when the user resizes the window.
        /// </summary>
        /// <param name="e">Contains the new width/height of the window.</param>
        /// <remarks>
        /// You want the OpenGL viewport to match the window. This is the place to do it!
        /// </remarks>
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            /*float aspect_ratio = Width / (float)Height;
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect_ratio, 1, 64);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref perpective);*/
            Matrix4 ortho = Matrix4.CreateOrthographic(2, 2, 1, 64);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref ortho);
        }

        #endregion

        #region OnUpdateFrame

        /// <summary>
        /// Prepares the next frame for rendering.
        /// </summary>
        /// <remarks>
        /// Place your control logic here. This is the place to respond to user input,
        /// update object positions etc.
        /// </remarks>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (Keyboard[OpenTK.Input.Key.Escape])
                this.Exit();

            if (Keyboard[OpenTK.Input.Key.F11])
                if (WindowState != WindowState.Fullscreen)
                    WindowState = WindowState.Fullscreen;
                else
                    WindowState = WindowState.Normal;
        }

        #endregion

        #region OnRenderFrame

        /// <summary>
        /// Place your rendering code here.
        /// </summary>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);

            globalTime += timeSpeed * (float)e.Time;
            #region Uniforms
            // viewport resolution (in pixels) (window resopution)
            GL.Uniform3(GL.GetUniformLocation(shader_program, "iResolution"), (float)Width, (float)Height, 0.0f);
            // shader playback time (in seconds)
            GL.Uniform1(GL.GetUniformLocation(shader_program, "iGlobalTime"), (float)globalTime);
            #endregion Uniforms

            Matrix4 lookat = Matrix4.LookAt(0, 0, 2, 0, 0, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref lookat);

            //angle += rotation_speed * (float)e.Time;
            //GL.Rotate(angle, 0.0f, 1.0f, 0.0f);

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertex_buffer_object);
            GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
            GL.BindBuffer(BufferTarget.ArrayBuffer, color_buffer_object);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, element_buffer_object);

            GL.DrawElements(BeginMode.Triangles, RectIndices.Length,
                DrawElementsType.UnsignedInt, IntPtr.Zero);

            //GL.DrawArrays(GL.Enums.BeginMode.POINTS, 0, shape.Vertices.Length);

            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            

            //int error = GL.GetError();
            //if (error != 0)
            //    Debug.Print(Glu.ErrorString(Glu.Enums.ErrorCode.INVALID_OPERATION));

            SwapBuffers();
        }

        #endregion

        #region public static void Main()

        /// <summary>
        /// Entry point of this example.
        /// </summary>
        public static void RunFragmentShader()
        {
            using (ShaderWindow example = new ShaderWindow())
            {
                // Get the title and category  of this example using reflection.
                //ExampleAttribute info = ((ExampleAttribute)example.GetType().GetCustomAttributes(false)[0]);
                //example.Title = String.Format("OpenTK | {0} {1}: {2}", info.Category, info.Difficulty, info.Title);
                example.Title = "Fragment Shader";
                example.Run(30.0, 0.0);
            }
        }

        /// <summary>
        /// Entry point of this example.
        /// </summary>
        public static void RunFragmentShaderWindow()
        {
            FragmentShaderSource = System.IO.File.ReadAllText("..\\..\\Shader\\SWater.glsl").Split(new Char[] { '\n' });            
            using (ShaderWindow example = new ShaderWindow())
            {
                // Get the title and category  of this example using reflection.
                //ExampleAttribute info = ((ExampleAttribute)example.GetType().GetCustomAttributes(false)[0]);
                //example.Title = String.Format("OpenTK | {0} {1}: {2}", info.Category, info.Difficulty, info.Title);
                example.Title = "Fragment Shader";
                example.Run(30.0, 0.0);
            }
        }

        #endregion
    }
}
