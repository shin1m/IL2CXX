using DemoContentLoader;
using System;
using System.Collections.Generic;
using System.Numerics;
using Blazor.Extensions.Canvas.WebGL;

namespace DemoRenderer.UI
{
    /// <summary>
    /// Location of a glyph in the atlas.
    /// </summary>
    public struct GlyphSource
    {
        public Vector2 Minimum;
        public int PackedSpan;  //Lower 16 bits X, upper 16 bits Y. In texels.
        public float DistanceScale;
    }
    /// <summary>
    /// Runtime type containing GPU-related information necessary to render a specific font type.
    /// </summary>
    public class Font : Disposable
    {
        private readonly WebGL2Context context;
        public readonly FontContent Content;
        public readonly GlyphSource[] Sources;
        private WebGLTexture? atlas;

        //Technically you could establish the char-source relationship within the font content itself, and that would eliminate one dictionary lookup.
        //However, source ids don't really exist outside of the runtime type, and establishing a consistent order for them would require a little more complexity.
        //Just doing it here is a little simpler. You can change this up if glyph setup is somehow ever a performance concern.
        private readonly Dictionary<char, int> sourceIds = new();

        public Font(WebGL2Context context, FontContent font)
        {
            this.context = context;
            Content = font;
            int nextSourceId = 0;
            Sources = new GlyphSource[Content.Characters.Count];
            foreach (var (key, value) in Content.Characters)
            {
                sourceIds.Add(key, nextSourceId);
                Sources[nextSourceId] = new()
                {
                    Minimum = new(value.SourceMinimum.X, value.SourceMinimum.Y),
                    PackedSpan = value.SourceSpan.X | (value.SourceSpan.Y << 16),
                    DistanceScale = value.DistanceScale
                };
                ++nextSourceId;
            }
        }
        public async Task InitializeAsync()
        {
            atlas = await context.CreateTextureAsync();
            await context.BindTextureAsync(TextureType.TEXTURE_2D, atlas);
            var font = Content.Atlas;
            for (int mipLevel = 0; mipLevel < font.MipLevels; ++mipLevel)
            {
                var width = font.Width >> mipLevel;
                var height = font.Height >> mipLevel;
                //await context.TexImage2DAsync(Texture2DType.TEXTURE_2D, mipLevel, PixelFormat.R8_SNORM, width, height, PixelFormat.RED, PixelType.BYTE, new ArraySegment<sbyte>((sbyte[])(Array)font.Data, font.GetMipStartIndex(mipLevel), width * height).ToArray());
                var index = font.GetMipStartIndex(mipLevel);
                var pixels = new byte[width * height];
                for (var i = 0; i < pixels.Length; ++i) pixels[i] = (byte)((sbyte)font.Data[index + i] + 128);
                await context.TexImage2DAsync(Texture2DType.TEXTURE_2D, mipLevel, PixelFormat.R8, width, height, PixelFormat.RED, PixelType.UNSIGNED_BYTE, pixels);
            }
            await context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAX_LEVEL, Content.Atlas.MipLevels - 1);
            await context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MIN_FILTER, (int)TextureParameterValue.LINEAR_MIPMAP_LINEAR);
            await context.TexParameterAsync(TextureType.TEXTURE_2D, TextureParameter.TEXTURE_MAG_FILTER, (int)TextureParameterValue.LINEAR);
            await context.BindTextureAsync(TextureType.TEXTURE_2D, null);
        }
        protected override async ValueTask DoDisposeAsync() => await context.DeleteTextureAsync(atlas);
        public Task UseAsync() => context.BindTextureAsync(TextureType.TEXTURE_2D, atlas);
        public int GetSourceId(char character) => sourceIds.TryGetValue(character, out var sourceId) ? sourceId : -1;
    }
}
