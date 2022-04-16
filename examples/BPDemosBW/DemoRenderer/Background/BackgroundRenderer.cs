using BepuUtilities;
using DemoContentLoader;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer.Background
{
    public class BackgroundRenderer : Shader
    {
        private readonly ConstantsBuffer<Matrix> constants;

        public BackgroundRenderer(WebGL2Context context) : base(context) => constants = new(context);
        public async Task InitializeAsync(ContentArchive content)
        {
            await InitializeAsync(
                content.Load<GLSLContent>(@"Background\RenderBackground.glvs").Source,
                content.Load<GLSLContent>(@"Background\RenderBackground.glfs").Source
            );
            await context.UniformBlockBindingAsync(program, await context.GetUniformBlockIndexAsync(program, "type_Constants"), 0);
            await constants.InitializeAsync();
        }
        public async Task RenderAsync(Camera camera)
        {
            await constants.UpdateAsync(0, Matrix.Invert(camera.View * Matrix.CreatePerspectiveFieldOfView(camera.FieldOfView, camera.AspectRatio, camera.FarClip, camera.NearClip)));
            await UseAsync();
            await context.DrawArraysAsync(Primitive.TRIANGLES, 0, 3);
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await constants.DisposeAsync();
            await base.DoDisposeAsync();
        }
    }
}
