using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using GL = WebGL;

using var host = new Host();
using var canvas = Host.GetCanvas();
using var gl = new GL(canvas);
var gear1 = new Gear(host, gl, new(0.8f, 0.1f, 0f, 1f), 1f, 4f, 1f, 20, 0.7f, "data/1F603_color.png");
var gear2 = new Gear(host, gl, new(0f, 0.8f, 0.2f, 1f), 0.5f, 2f, 2f, 10, 0.7f, "data/1F923_color.png");
var gear3 = new Gear(host, gl, new(0.2f, 0.2f, 1f, 1f), 1.3f, 2f, 0.5f, 10, 0.7f, "data/1F970_color.png");
gl.ClearColor(0f, 0f, 0f, 1f);
gl.Enable(GL.CULL_FACE);
gl.Enable(GL.DEPTH_TEST);
var rotate_x = MathF.PI * 20f / 180f;
var rotate_y = MathF.PI * 30f / 180f;
var rotate_z = 0f;
var then = 0.0;
var angle = 0f;
var done = new TaskCompletionSource();
void render(double now)
{
    var projection = Matrix4x4.CreatePerspective(gl.Width * 2f / gl.Height, 2f, 5f, 60f);
    var viewing =
        Matrix4x4.CreateRotationZ(rotate_z) *
        Matrix4x4.CreateRotationY(rotate_y) *
        Matrix4x4.CreateRotationX(rotate_x) *
        Matrix4x4.CreateTranslation(0f, 0f, -40f);
    gl.Clear(GL.COLOR_BUFFER_BIT | GL.DEPTH_BUFFER_BIT);
    gear1.Draw(gl, projection,
        Matrix4x4.CreateRotationZ(angle) *
        Matrix4x4.CreateTranslation(-3f, -2f, 0f) *
        viewing
    );
    gear2.Draw(gl, projection,
        Matrix4x4.CreateRotationZ(-2f * angle - MathF.PI * 9f / 180f) *
        Matrix4x4.CreateTranslation(3.1f, -2f, 0f) *
        viewing
    );
    gear3.Draw(gl, projection,
        Matrix4x4.CreateRotationZ(2f * angle - MathF.PI * 2f / 180f) *
        Matrix4x4.CreateRotationX(MathF.PI * 0.5f) *
        Matrix4x4.CreateTranslation(-3.1f, 2.2f, -1.8f) *
        viewing
    );
    angle += (float)((now - then) * 0.001) * MathF.PI * 60f / 180f;
    then = now;
    Host.RequestAnimationFrame(render);
}
Vector2? pressed = null;
var origin = new Vector2();
Host.OnMouseDown(canvas, (button, x, y) =>
{
    if (pressed != null) return;
    pressed = new(x, y);
    origin = new(rotate_x, rotate_y);
});
Host.OnMouseUp(canvas, (button, x, y) => pressed = null);
Host.OnMouseMove(canvas, (x, y) =>
{
    if (!(pressed is Vector2 p)) return;
    rotate_x = origin.X + MathF.PI * (y - p.Y) / 180f;
    rotate_y = origin.Y + MathF.PI * (x - p.X) / 180f;
});
Host.RequestAnimationFrame(render);
await done.Task;

interface IHost
{
    T Join<T>(T x) where T : IDisposable;
}

partial class Host : IDisposable, IHost
{
    [JSImport("globalThis.requestAnimationFrame")]
    internal static partial int RequestAnimationFrame([JSMarshalAs<JSType.Function<JSType.Number>>]Action<double> action);
    [JSImport("getCanvas", "main.js")]
    internal static partial JSObject GetCanvas();
    [JSImport("loadImage", "main.js")]
    internal static partial Task<JSObject> LoadImageAsync(string url);
    [JSImport("onMouseDown", "main.js")]
    internal static partial void OnMouseDown(JSObject element, [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]Action<int, int, int> action);
    [JSImport("onMouseUp", "main.js")]
    internal static partial void OnMouseUp(JSObject element, [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]Action<int, int, int> action);
    [JSImport("onMouseMove", "main.js")]
    internal static partial void OnMouseMove(JSObject element, [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]Action<int, int> action);

    private readonly List<IDisposable> disposables = new();

    public void Dispose()
    {
        foreach (var x in disposables) x.Dispose();
    }
    public T Join<T>(T x) where T : IDisposable
    {
        disposables.Add(x);
        return x;
    }
}

class Source
{
    private Vector3 normal = new(0f, 0f, 1f);
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector3> normals = new();

    public void Vertex3f(float x, float y, float z)
    {
        vertices.Add(new(x, y, z));
        normals.Add(normal);
    }
    public void Normal3f(float x, float y, float z) => normal = new(x, y, z);
    public int Count => vertices.Count;
    private JSObject Transfer(IHost host, GL gl, List<Vector3> vectors)
    {
        var buffer = host.Join(gl.CreateBuffer());
        gl.BindBuffer(GL.ARRAY_BUFFER, buffer);
        gl.BufferData(GL.ARRAY_BUFFER, CollectionsMarshal.AsSpan(vectors), GL.STATIC_DRAW);
        return buffer;
    }
    public JSObject Vertices(IHost host, GL gl) => Transfer(host, gl, vertices);
    public JSObject Normals(IHost host, GL gl) => Transfer(host, gl, normals);
}

class Gear
{
    class Face
    {
        class Vertices
        {
            readonly JSObject disc;
            readonly int disc_count;
            readonly JSObject teeth;
            readonly int teeth_count;

            public Vertices(IHost host, GL gl, Source disc, Source teeth)
            {
                this.disc = disc.Vertices(host, gl);
                disc_count = disc.Count;
                this.teeth = teeth.Vertices(host, gl);
                teeth_count = teeth.Count;
            }
            public void Draw(GL gl, int attribute)
            {
                gl.BindBuffer(GL.ARRAY_BUFFER, disc);
                gl.VertexAttribPointer(attribute, 3, GL.FLOAT, false, 0, 0);
                gl.DrawArrays(GL.TRIANGLE_STRIP, 0, disc_count);
                gl.BindBuffer(GL.ARRAY_BUFFER, teeth);
                gl.VertexAttribPointer(attribute, 3, GL.FLOAT, false, 0, 0);
                gl.DrawArrays(GL.TRIANGLES, 0, teeth_count);
            }
        }

        readonly JSObject texture;
        readonly Matrix4x4 texture_matrix;
        readonly Vertices front;
        readonly Vertices back;
        readonly JSObject program;
        readonly int attribute_vertex;
        readonly JSObject uniform_color;
        readonly JSObject uniform_vertex_matrix;
        readonly JSObject uniform_texture_matrix;
        readonly JSObject uniform_texture;
        readonly Vector3 light;

        public Face(IHost host, GL gl, JSObject texture, Matrix4x4 texture_matrix, Source front_disc, Source front_teeth, Source back_disc, Source back_teeth, JSObject program, Vector3 light)
        {
            this.texture = texture;
            this.texture_matrix = texture_matrix;
            front = new(host, gl, front_disc, front_teeth);
            back = new(host, gl, back_disc, back_teeth);
            this.program = program;
            attribute_vertex = gl.GetAttribLocation(program, "vertex");
            uniform_color = host.Join(gl.GetUniformLocation(program, "color"));
            uniform_vertex_matrix = host.Join(gl.GetUniformLocation(program, "vertexMatrix"));
            uniform_texture_matrix = host.Join(gl.GetUniformLocation(program, "textureMatrix"));
            uniform_texture = host.Join(gl.GetUniformLocation(program, "texture"));
            this.light = light;
        }
        public void Draw(GL gl, Vector4 color, Matrix4x4 normal_matrix, Matrix4x4 vertex_matrix)
        {
            gl.UseProgram(program);
            gl.EnableVertexAttribArray(attribute_vertex);
            gl.ActiveTexture(GL.TEXTURE0);
            gl.BindTexture(GL.TEXTURE_2D, texture);
            gl.Uniform1i(uniform_texture, 0);
            void face(Vector3 normal, Vertices vertices)
            {
                // compute color for flat shaded surface
                var ad = new Vector4(0.2f, 0.2f, 0.2f, 1f);   // ambient
                var ndotlp = Vector3.Dot(Vector3.Normalize(Vector3.TransformNormal(normal, normal_matrix)), light);
                if (ndotlp > 0f) ad += new Vector4(ndotlp, ndotlp, ndotlp, 0f); // ambient + diffuse
                gl.Uniform4f(uniform_color, ad.X * color.X, ad.Y * color.Y, ad.Z * color.Z, ad.W * color.W); // color * (ambient + diffuse)
                gl.UniformMatrix4fv(uniform_vertex_matrix, false, vertex_matrix);
                gl.UniformMatrix4fv(uniform_texture_matrix, false, texture_matrix);
                vertices.Draw(gl, attribute_vertex);
            }
            face(new(0f, 0f, 1f), front);
            face(new(0f, 0f, -1f), back);
            gl.DisableVertexAttribArray(attribute_vertex);
            gl.BindTexture(GL.TEXTURE_2D, null);
        }
    }
    class Side
    {
        readonly JSObject vertices;
        readonly JSObject normals;
        readonly int count;
        readonly JSObject program;
        readonly int attribute_vertex;
        readonly int attribute_normal;
        readonly JSObject uniform_color;
        readonly JSObject uniform_normal_matrix;
        readonly JSObject uniform_vertex_matrix;

        public Side(IHost host, GL gl, Source source, JSObject program)
        {
            vertices = source.Vertices(host, gl);
            normals = source.Normals(host, gl);
            count = source.Count;
            this.program = program;
            attribute_vertex = gl.GetAttribLocation(program, "vertex");
            attribute_normal = gl.GetAttribLocation(program, "normal");
            uniform_color = host.Join(gl.GetUniformLocation(program, "color"));
            uniform_normal_matrix = host.Join(gl.GetUniformLocation(program, "normalMatrix"));
            uniform_vertex_matrix = host.Join(gl.GetUniformLocation(program, "vertexMatrix"));
        }
        public void Draw(GL gl, Vector4 color, Span<float> normal_matrix, Matrix4x4 vertex_matrix)
        {
            gl.UseProgram(program);
            gl.EnableVertexAttribArray(attribute_vertex);
            gl.EnableVertexAttribArray(attribute_normal);
            gl.Uniform4f(uniform_color, color.X, color.Y, color.Z, color.W);
            gl.UniformMatrix3fv(uniform_normal_matrix, false, normal_matrix);
            gl.UniformMatrix4fv(uniform_vertex_matrix, false, vertex_matrix);
            gl.BindBuffer(GL.ARRAY_BUFFER, vertices);
            gl.VertexAttribPointer(attribute_vertex, 3, GL.FLOAT, false, 0, 0);
            gl.BindBuffer(GL.ARRAY_BUFFER, normals);
            gl.VertexAttribPointer(attribute_normal, 3, GL.FLOAT, false, 0, 0);
            gl.DrawArrays(GL.TRIANGLE_STRIP, 0, count);
            gl.DisableVertexAttribArray(attribute_normal);
            gl.DisableVertexAttribArray(attribute_vertex);
        }
    }

    readonly Vector4 color;
    readonly Face face;
    readonly Side outward;
    readonly Side cylinder;

    public Gear(IHost host, GL gl, Vector4 color, float inner, float outer, float width, int teeth, float depth, string image)
    {
        this.color = color;

        var texture = host.Join(gl.CreateTexture());
        ((Action)(async () =>
        {
            var data = await Host.LoadImageAsync(image);
            gl.ActiveTexture(GL.TEXTURE0);
            gl.BindTexture(GL.TEXTURE_2D, texture);
            gl.TexImage2D(GL.TEXTURE_2D, 0, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, data);
            gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
            gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
            gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.NEAREST);
            gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
            gl.BindTexture(GL.TEXTURE_2D, null);
        }))();

        var r0 = inner;
        var r1 = outer - depth / 2f;
        var r2 = outer + depth / 2f;
        var dz = 0.5f * width;
        var tooth = 2f * MathF.PI / teeth;
        var t4 = tooth / 4f;
        var front_disc = new Source();
        var front_teeth = new Source();
        var back_disc = new Source();
        var back_teeth = new Source();
        var outward = new Source();
        var cylinder = new Source();
        for (var i = 0; i < teeth; ++i)
        {
            var angle = i * tooth;
            var (cos0, sin0) = (MathF.Cos(angle), MathF.Sin(angle));
            var (x0, y0) = (r0 * cos0, r0 * sin0);
            var (x1, y1) = (r1 * cos0, r1 * sin0);
            var (x2, y2) = (r2 * MathF.Cos(angle + t4), r2 * MathF.Sin(angle + t4));
            var (x3, y3) = (r2 * MathF.Cos(angle + t4 * 2), r2 * MathF.Sin(angle + t4 * 2));
            var (x4, y4) = (r1 * MathF.Cos(angle + t4 * 3), r1 * MathF.Sin(angle + t4 * 3));
            // front face
            // GL_TRIANGLE_STRIP
            front_disc.Vertex3f(x0, y0, dz);
            front_disc.Vertex3f(x1, y1, dz);
            front_disc.Vertex3f(x0, y0, dz);
            front_disc.Vertex3f(x4, y4, dz);
            // front sides of teeth
            // GL_TRIANGLES
            front_teeth.Vertex3f(x1, y1, dz);   // 0
            front_teeth.Vertex3f(x2, y2, dz);   // 1
            front_teeth.Vertex3f(x3, y3, dz);   // 2
            front_teeth.Vertex3f(x1, y1, dz);   // 0
            front_teeth.Vertex3f(x3, y3, dz);   // 2
            front_teeth.Vertex3f(x4, y4, dz);   // 3
            // back face
            // GL_TRIANGLE_STRIP
            back_disc.Vertex3f(x1, y1, -dz);
            back_disc.Vertex3f(x0, y0, -dz);
            back_disc.Vertex3f(x4, y4, -dz);
            back_disc.Vertex3f(x0, y0, -dz);
            // back sides of teeth
            // GL_TRIANGLES
            back_teeth.Vertex3f(x4, y4, -dz);   // 0
            back_teeth.Vertex3f(x3, y3, -dz);   // 1
            back_teeth.Vertex3f(x2, y2, -dz);   // 2
            back_teeth.Vertex3f(x4, y4, -dz);   // 0
            back_teeth.Vertex3f(x2, y2, -dz);   // 2
            back_teeth.Vertex3f(x1, y1, -dz);   // 3
            // outward faces of teeth
            // GL_TRIANGLE_STRIP
            // repeated vertices are necessary to achieve flat shading in ES2
            if (i > 0)
            {
                outward.Vertex3f(x1, y1, dz);
                outward.Vertex3f(x1, y1, -dz);
            }
            outward.Normal3f(y2 - y1, x1 - x2, 0f);
            outward.Vertex3f(x1, y1, dz);
            outward.Vertex3f(x1, y1, -dz);
            outward.Vertex3f(x2, y2, dz);
            outward.Vertex3f(x2, y2, -dz);
            outward.Normal3f(cos0, sin0, 0f);
            outward.Vertex3f(x2, y2, dz);
            outward.Vertex3f(x2, y2, -dz);
            outward.Vertex3f(x3, y3, dz);
            outward.Vertex3f(x3, y3, -dz);
            outward.Normal3f(y4 - y3, x3 - x4, 0f);
            outward.Vertex3f(x3, y3, dz);
            outward.Vertex3f(x3, y3, -dz);
            outward.Vertex3f(x4, y4, dz);
            outward.Vertex3f(x4, y4, -dz);
            outward.Normal3f(cos0, sin0, 0f);
            outward.Vertex3f(x4, y4, dz);
            outward.Vertex3f(x4, y4, -dz);
            // inside radius cylinder
            // GL_TRIANGLE_STRIP
            cylinder.Normal3f(-cos0, -sin0, 0f);
            cylinder.Vertex3f(x0, y0, -dz);
            cylinder.Vertex3f(x0, y0, dz);
        }
        front_disc.Vertex3f(r0, 0f, dz);
        front_disc.Vertex3f(r1, 0f, dz);
        back_disc.Vertex3f(r1, 0f, -dz);
        back_disc.Vertex3f(r0, 0f, -dz);
        outward.Vertex3f(r1, 0f, dz);
        outward.Vertex3f(r1, 0f, -dz);
        cylinder.Normal3f(-1f, 0f, 0f);
        cylinder.Vertex3f(r0, 0f, -dz);
        cylinder.Vertex3f(r0, 0f, dz);

        JSObject build(string vshader, string fshader)
        {
            var program = host.Join(gl.CreateProgram());
            void compile(int type, string source, Action action)
            {
                using var shader = gl.CreateShader(type);
                try
                {
                    gl.ShaderSource(shader, source);
                    gl.CompileShader(shader);
                    var error = gl.GetShaderInfoLog(shader);
                    if (error != string.Empty) throw new Exception(error);
                    gl.AttachShader(program, shader);
                    try
                    {
                        action();
                    }
                    finally
                    {
                        gl.DetachShader(program, shader);
                    }
                }
                finally
                {
                    gl.DeleteShader(shader);
                }
            }
            compile(GL.VERTEX_SHADER, vshader, () => compile(GL.FRAGMENT_SHADER, fshader, () =>
            {
                gl.LinkProgram(program);
                var error = gl.GetProgramInfoLog(program);
                if (error != string.Empty) throw new Exception(error);
            }));
            return program;
        }

        // face shaders
        // flat shading + uniform color + uniform mvp
        face = new(host, gl, texture,
            Matrix4x4.CreateScale(0.5f / r2, -0.5f / r2, 1f) * Matrix4x4.CreateTranslation(0.5f, 0.5f, 0f),
            front_disc, front_teeth, back_disc, back_teeth, build(@"
attribute vec3 vertex;
uniform mat4 vertexMatrix;
uniform mat4 textureMatrix;
varying vec2 texCoord;

void main()
{
	vec4 v = vec4(vertex, 1.0);
	gl_Position = vertexMatrix * v;
	texCoord = (textureMatrix * v).xy;
}
", @"
#ifdef GL_ES
precision mediump float;
precision mediump int;
#endif

varying vec2 texCoord;
uniform vec4 color;
uniform sampler2D texture;

void main()
{
	gl_FragColor = color * 0.5 + texture2D(texture, texCoord) * 0.5;
}
"), Vector3.Normalize(new(5f, 5f, 10f)));
        // outward teeth shaders
        // flat shading - each normal across polygon is constant
        // per-vertex normal + uniform color + uniform mvp
        this.outward = new(host, gl, outward, build(@"
attribute vec3 vertex;
attribute vec3 normal;
uniform vec4 color;
uniform mat3 normalMatrix;
uniform mat4 vertexMatrix;
varying vec4 varying_color;

void main()
{
	vec3 light = normalize(vec3(5.0, 5.0, 10.0));
	vec4 c = vec4(0.2, 0.2, 0.2, 1.0);
	float d = dot(normalize(normalMatrix * normal), light);
	if (d > 0.0) c += vec4(d, d, d, 0.0);
	varying_color = color * c;
	gl_Position = vertexMatrix * vec4(vertex, 1.0);
}
", @"
#ifdef GL_ES
precision mediump float;
precision mediump int;
#endif

varying vec4 varying_color;

void main()
{
	gl_FragColor = varying_color;
}
"));
        // cylinder shaders
        // smooth shading + per-vertex normal + uniform color + uniform mvp
        this.cylinder = new(host, gl, cylinder, build(@"
attribute vec3 vertex;
attribute vec3 normal;
uniform mat3 normalMatrix;
uniform mat4 vertexMatrix;
varying vec3 varying_normal;

void main()
{
	varying_normal = normalize(normalMatrix * normal);
	gl_Position = vertexMatrix * vec4(vertex, 1.0);
}
", @"
#ifdef GL_ES
precision mediump float;
precision mediump int;
#endif

uniform vec4 color;
varying vec3 varying_normal;

void main()
{
	vec3 light = normalize(vec3(5.0, 5.0, 10.0));
	vec4 c = vec4(0.2, 0.2, 0.2, 1.0);
	float d  = dot(varying_normal, light);
	if (d > 0.0) c += vec4(d, d, d, 0.0);
	gl_FragColor = color * c;
}
"));
        gl.BindBuffer(GL.ARRAY_BUFFER, null);
    }
    public void Draw(GL gl, Matrix4x4 projection, Matrix4x4 viewing)
    {
        var vertex_matrix = viewing * projection;
        //var normal_matrix = (~Matrix3(viewing)).transposition();
        if (Matrix4x4.Invert(viewing, out var normal_matrix))
        {
            normal_matrix.Translation = default;
            normal_matrix = Matrix4x4.Transpose(normal_matrix);
        }
        var normal_matrix_values = new[] {
            normal_matrix.M11, normal_matrix.M12, normal_matrix.M13,
            normal_matrix.M21, normal_matrix.M22, normal_matrix.M23,
            normal_matrix.M31, normal_matrix.M32, normal_matrix.M33
        };
        // front, back, front teeth and back teeth
        face.Draw(gl, color, normal_matrix, vertex_matrix);
        // outward teeth
        outward.Draw(gl, color, normal_matrix_values, vertex_matrix);
        // cylinder
        cylinder.Draw(gl, color, normal_matrix_values, vertex_matrix);
        gl.UseProgram(null);
        gl.BindBuffer(GL.ARRAY_BUFFER, null);
    }
}
