using BepuUtilities;
using DemoContentLoader;
using GL = ThinJS.WebGL2;

namespace DemoRenderer.Background
{
    public class BackgroundRenderer : Shader
    {
        private readonly ConstantsBuffer<Matrix> constants;

        public BackgroundRenderer(GL context) : base(context) => constants = new(context);
        protected override void DoDispose()
        {
            constants.Dispose();
            base.DoDispose();
        }
        public void Initialize(ContentArchive content)
        {
            var program = Build(
                content.Load<GLSLContent>(@"Background\RenderBackground.glvs").Source,
                content.Load<GLSLContent>(@"Background\RenderBackground.glfs").Source
            );
            context.UniformBlockBinding(program, context.GetUniformBlockIndex(program, "type_Constants"), 0);
            constants.Initialize();
        }
        public void Render(Camera camera)
        {
            constants.Update(0, Matrix.Invert(camera.View * Matrix.CreatePerspectiveFieldOfView(camera.FieldOfView, camera.AspectRatio, camera.FarClip, camera.NearClip)));
            Use();
            context.DrawArrays(GL.TRIANGLES, 0, 3);
        }
    }
}
