using System;
using System.Threading.Tasks;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer
{
    public class Shader : Disposable
    {
        protected readonly WebGL2Context context;
        protected WebGLProgram? program;
        private WebGLVertexArrayObject? vao;

        private async Task CompileAsync(ShaderType type, string source, Func<Task> action)
        {
            var shader = await context.CreateShaderAsync(type);
            try
            {
                await context.ShaderSourceAsync(shader, source);
                await context.CompileShaderAsync(shader);
                var error = await context.GetShaderInfoLogAsync(shader);
                if (error != string.Empty) throw new Exception(error);
                await context.AttachShaderAsync(program, shader);
                try
                {
                    await action();
                }
                finally
                {
                    await context.DetachShaderAsync(program, shader);
                }
            }
            finally
            {
                await context.DeleteShaderAsync(shader);
            }
        }

        public Shader(WebGL2Context context) => this.context = context;
        public async Task InitializeAsync(string vertex, string fragment)
        {
            program = await context.CreateProgramAsync();
            vao = await context.CreateVertexArrayAsync();
            await CompileAsync(ShaderType.VERTEX_SHADER, vertex, () => CompileAsync(ShaderType.FRAGMENT_SHADER, fragment, async () =>
            {
                await context.LinkProgramAsync(program);
                var error = await context.GetProgramInfoLogAsync(program);
                if (error != string.Empty) throw new Exception(error);
            }));
            await context.BindVertexArrayAsync(vao);
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await context.DeleteProgramAsync(program);
            await context.DeleteVertexArrayAsync(vao);
        }
        public async Task UseAsync()
        {
            await context.UseProgramAsync(program);
            await context.BindVertexArrayAsync(vao);
        }
    }
}
