using System;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using GL = ThinJS.WebGL;

using var canvas = Host.GetCanvas();
using var gl = new GL(canvas);

using var program = gl.CreateProgram();
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
compile(GL.VERTEX_SHADER, @"
attribute vec4 aVertexPosition;
attribute vec4 aVertexColor;

uniform mat4 uModelViewMatrix;
uniform mat4 uProjectionMatrix;

varying lowp vec4 vColor;

void main(void) {
  gl_Position = uProjectionMatrix * uModelViewMatrix * aVertexPosition;
  vColor = aVertexColor;
}
", () => compile(GL.FRAGMENT_SHADER, @"
varying lowp vec4 vColor;

void main(void) {
  gl_FragColor = vColor;
}
", () =>
{
    gl.LinkProgram(program);
    var error = gl.GetProgramInfoLog(program);
    if (error != string.Empty) throw new Exception(error);
}));

var aPosition = gl.GetAttribLocation(program, "aVertexPosition");
var aColor = gl.GetAttribLocation(program, "aVertexColor");
using var uProjection = gl.GetUniformLocation(program, "uProjectionMatrix");
using var uModelView = gl.GetUniformLocation(program, "uModelViewMatrix");

using var positions = gl.CreateBuffer();
gl.BindBuffer(GL.ARRAY_BUFFER, positions);
gl.BufferData(GL.ARRAY_BUFFER, new[] {1f, 1f, -1f, 1f, 1f, -1f, -1f, -1f}.AsSpan(), GL.STATIC_DRAW);
using var colors = gl.CreateBuffer();
gl.BindBuffer(GL.ARRAY_BUFFER, colors);
gl.BufferData(GL.ARRAY_BUFFER, new[] {
    1f, 1f, 1f, 1f, // white
    1f, 0f, 0f, 1f, // red
    0f, 1f, 0f, 1f, // green
    0f, 0f, 1f, 1f, // blue
}.AsSpan(), GL.STATIC_DRAW);

gl.ClearColor(0f, 0f, 0f, 1f);
gl.Enable(GL.CULL_FACE);
gl.Enable(GL.DEPTH_TEST);
var done = new TaskCompletionSource();
var then = 0.0;
var squareRotation = 0.0;
void render(double now)
{
    now *= 0.001;
    var deltaTime = now - then;
    then = now;
    gl.Clear(GL.COLOR_BUFFER_BIT | GL.DEPTH_BUFFER_BIT);
    var projection = Matrix4x4.CreatePerspectiveFieldOfView(45f * MathF.PI / 180f, gl.Width / gl.Height, 0.1f, 100f);
    var viewing = Matrix4x4.CreateTranslation(0f, 0f, -6f);
    gl.BindBuffer(GL.ARRAY_BUFFER, positions);
    gl.VertexAttribPointer(aPosition, 2, GL.FLOAT, false, 0, 0);
    gl.EnableVertexAttribArray(aPosition);
    gl.BindBuffer(GL.ARRAY_BUFFER, colors);
    gl.VertexAttribPointer(aColor, 4, GL.FLOAT, false, 0, 0);
    gl.EnableVertexAttribArray(aColor);
    gl.UseProgram(program);
    gl.UniformMatrix4fv(uProjection, false, projection);
    gl.UniformMatrix4fv(uModelView, false, Matrix4x4.CreateRotationZ((float)squareRotation) * viewing);
    gl.DrawArrays(GL.TRIANGLE_STRIP, 0, 4);
    gl.DisableVertexAttribArray(aPosition);
    gl.DisableVertexAttribArray(aColor);
    squareRotation += deltaTime;
    Host.RequestAnimationFrame(render);
}
Host.RequestAnimationFrame(render);
await done.Task;

partial class Host
{
    [JSImport("globalThis.requestAnimationFrame")]
    internal static partial int RequestAnimationFrame([JSMarshalAs<JSType.Function<JSType.Number>>]Action<double> action);
    [JSImport("getCanvas", "main.js")]
    internal static partial JSObject GetCanvas();
}
