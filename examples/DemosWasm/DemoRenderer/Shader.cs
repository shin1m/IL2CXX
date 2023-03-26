using System;
using System.Runtime.InteropServices.JavaScript;
using GL = WebGL2;

namespace DemoRenderer
{
    public class Shader : Disposable
    {
        protected readonly GL context;
        private readonly JSObject? program;
        private readonly JSObject? vao;

        public Shader(GL context)
        {
            this.context = context;
            program = context.CreateProgram();
            vao = context.CreateVertexArray();
        }
        protected override void DoDispose()
        {
            context.DeleteProgram(program);
            program?.Dispose();
            context.DeleteVertexArray(vao);
            vao?.Dispose();
        }
        public JSObject Build(string vertex, string fragment)
        {
            if (program == null) throw new Exception();
            void compile(int type, string source, Action action)
            {
                var shader = context.CreateShader(type) ?? throw new Exception();
                try
                {
                    context.ShaderSource(shader, source);
                    context.CompileShader(shader);
                    var error = context.GetShaderInfoLog(shader);
                    if (error != string.Empty) throw new Exception(error);
                    context.AttachShader(program, shader);
                    try
                    {
                        action();
                    }
                    finally
                    {
                        context.DetachShader(program, shader);
                    }
                }
                finally
                {
                    context.DeleteShader(shader);
                }
            }
            compile(GL.VERTEX_SHADER, vertex, () => compile(GL.FRAGMENT_SHADER, fragment, () =>
            {
                context.LinkProgram(program);
                var error = context.GetProgramInfoLog(program);
                if (error != string.Empty) throw new Exception(error);
            }));
            context.BindVertexArray(vao);
            return program;
        }
        public void Use()
        {
            context.UseProgram(program);
            context.BindVertexArray(vao);
        }
    }
}
