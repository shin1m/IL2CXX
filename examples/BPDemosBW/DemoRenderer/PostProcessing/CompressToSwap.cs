using DemoContentLoader;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer.PostProcessing
{
    /// <summary>
    /// Applies a gamma curve, anti-banding dithering, and outputs the result into the lower precision target.
    /// </summary>
    public class CompressToSwap : Shader
    {
        /// <summary>
        /// Gets or sets the display gamma. This isn't SRGB, but it'll do.
        /// </summary>
        public float Gamma;
        private readonly ConstantsBuffer<float> constants; //alas, lack of root constants

        public CompressToSwap(WebGL2Context context, float gamma = 2.2f) : base(context)
        {
            Gamma = gamma;
            constants = new(context);
        }
        public async Task InitializeAsync(ContentArchive content)
        {
            await InitializeAsync(
                content.Load<GLSLContent>(@"PostProcessing\CompressToSwap.glvs").Source,
                content.Load<GLSLContent>(@"PostProcessing\CompressToSwap.glfs").Source
            );
            await context.UniformBlockBindingAsync(program, await context.GetUniformBlockIndexAsync(program, "type_Constants"), 0);
            await constants.InitializeAsync();
        }
        protected override async ValueTask DoDisposeAsync()
        {
            await constants.DisposeAsync();
            await base.DoDisposeAsync();
        }
        public async Task RenderAsync(WebGLTexture source)
        {
            await constants.UpdateAsync(0, 1f / Gamma);
            await UseAsync();
            await context.BindTextureAsync(TextureType.TEXTURE_2D, source);
            await context.DrawArraysAsync(Primitive.TRIANGLES, 0, 3);
        }
    }
}
