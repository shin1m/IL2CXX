using System.Runtime.InteropServices.JavaScript;
using DemoContentLoader;
using GL = ThinJS.WebGL2;

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

        public CompressToSwap(GL context, float gamma = 2.2f) : base(context)
        {
            Gamma = gamma;
            constants = new(context);
        }
        protected override void DoDispose()
        {
            constants.Dispose();
            base.DoDispose();
        }
        public void Initialize(ContentArchive content)
        {
            var program = Build(
                content.Load<GLSLContent>(@"PostProcessing\CompressToSwap.glvs").Source,
                content.Load<GLSLContent>(@"PostProcessing\CompressToSwap.glfs").Source
            );
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_Constants"), 0);
            constants.Initialize();
        }
        public void Render(JSObject? source)
        {
            constants.Update(0, 1f / Gamma);
            Use();
            context.BindTexture(GL.TEXTURE_2D, source);
            context.DrawArrays(GL.TRIANGLES, 0, 3);
        }
    }
}
