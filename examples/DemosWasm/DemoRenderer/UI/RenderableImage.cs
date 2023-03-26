using DemoContentLoader;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using GL = WebGL2;

namespace DemoRenderer.UI
{
    /// <summary>
    /// Runtime type containing GPU-related information necessary to render a specific font type.
    /// </summary>
    public class RenderableImage : Disposable
    {
        private readonly GL context;
        public readonly Texture2DContent Content;
        private readonly bool srgb;
        public readonly JSObject? Texture;

        public RenderableImage(GL context, Texture2DContent imageContent, bool srgb = false)
        {
            if (imageContent.TexelSizeInBytes != 4)
            {
                throw new ArgumentException("The renderable image assumes an R8G8B8A8_UNorm or  texture.");
            }
            Debug.Assert(imageContent.MipLevels == 1, "We ignore any mip levels stored in the content; if the content pipeline output them, something's likely mismatched.");
            this.context = context;
            Content = imageContent;
            this.srgb = srgb;
            Texture = context.CreateTexture();
            context.BindTexture(GL.TEXTURE_2D, Texture);
            // Uploads the mip0 stored in the Content to the Texture2D and generates new mips.
            context.TexImage2D(GL.TEXTURE_2D, 0,
                //srgb ? GL.RGBA8_SNORM :
                GL.RGBA,
                Content.Width, Content.Height, 0,
                GL.RGBA, GL.UNSIGNED_BYTE,
                Content.Data.AsSpan(Content.GetMipStartIndex(0))
            );
            context.GenerateMipmap(GL.TEXTURE_2D);
            context.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.LINEAR_MIPMAP_LINEAR);
            context.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
            context.BindTexture(GL.TEXTURE_2D, null);
        }
        protected override void DoDispose()
        {
            context.DeleteTexture(Texture);
            Texture?.Dispose();
        }
    }
}
