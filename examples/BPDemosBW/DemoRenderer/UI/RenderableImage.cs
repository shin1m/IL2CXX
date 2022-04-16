using DemoContentLoader;
using System;
using System.Diagnostics;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer.UI
{
    /// <summary>
    /// Runtime type containing GPU-related information necessary to render a specific font type.
    /// </summary>
    public class RenderableImage : Disposable
    {
        private readonly WebGLContext context;
        public readonly Texture2DContent Content;
        private readonly bool srgb;
        public WebGLTexture? Texture;

        public RenderableImage(WebGLContext context, int width, int height, bool srgb = false)
        {
            this.context = context;
            Content = new Texture2DContent(width, height, 1, 4);
            this.srgb = srgb;
        }
        public RenderableImage(WebGLContext context, Texture2DContent imageContent, bool srgb = false)
        {
            if (imageContent.TexelSizeInBytes != 4)
            {
                throw new ArgumentException("The renderable image assumes an R8G8B8A8_UNorm or  texture.");
            }
            Debug.Assert(imageContent.MipLevels == 1, "We ignore any mip levels stored in the content; if the content pipeline output them, something's likely mismatched.");
            this.context = context;
            Content = imageContent;
            this.srgb = srgb;
        }
        public async Task InitializeAsync()
        {
            Texture = await context.CreateTextureAsync();
            await context.BindTextureAsync(TextureType.TEXTURE_2D, Texture);
            // Uploads the mip0 stored in the Content to the Texture2D and generates new mips.
            await context.TexImage2DAsync(Texture2DType.TEXTURE_2D, 0,
                //srgb ? PixelFormat.RGBA8_SNORM :
                PixelFormat.RGBA,
                Content.Width, Content.Height,
                PixelFormat.RGBA, PixelType.UNSIGNED_BYTE,
                new ArraySegment<byte>(Content.Data).Slice(Content.GetMipStartIndex(0)).ToArray()
            );
            await context.GenerateMipmapAsync(TextureType.TEXTURE_2D);
            await context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MIN_FILTER, (int)TextureParameterValue.LINEAR_MIPMAP_LINEAR);
            await context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAG_FILTER, (int)TextureParameterValue.LINEAR);
            await context.BindTextureAsync(TextureType.TEXTURE_2D, null);
        }
        protected override async ValueTask DoDisposeAsync() => await context.DeleteTextureAsync(Texture);
    }
}
