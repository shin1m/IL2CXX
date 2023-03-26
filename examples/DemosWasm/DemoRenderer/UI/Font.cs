using DemoContentLoader;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using GL = WebGL2;

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
        private readonly GL context;
        public readonly FontContent Content;
        public readonly GlyphSource[] Sources;
        private readonly JSObject? atlas;

        //Technically you could establish the char-source relationship within the font content itself, and that would eliminate one dictionary lookup.
        //However, source ids don't really exist outside of the runtime type, and establishing a consistent order for them would require a little more complexity.
        //Just doing it here is a little simpler. You can change this up if glyph setup is somehow ever a performance concern.
        private readonly Dictionary<char, int> sourceIds = new();

        public Font(GL context, FontContent content)
        {
            this.context = context;
            Content = content;
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
            atlas = context.CreateTexture();
            context.BindTexture(GL.TEXTURE_2D, atlas);
            var font = Content.Atlas;
            for (int mipLevel = 0; mipLevel < font.MipLevels; ++mipLevel)
            {
                var width = font.Width >> mipLevel;
                var height = font.Height >> mipLevel;
                //context.TexImage2D(GL.TEXTURE_2D, mipLevel, GL.R8_SNORM, width, height, 0, GL.RED, GL.BYTE, font.Data.AsSpan(font.GetMipStartIndex(mipLevel), width * height));
                var index = font.GetMipStartIndex(mipLevel);
                var pixels = new byte[width * height];
                for (var i = 0; i < pixels.Length; ++i) pixels[i] = (byte)((sbyte)font.Data[index + i] + 128);
                context.TexImage2D(GL.TEXTURE_2D, mipLevel, GL.R8, width, height, 0, GL.RED, GL.UNSIGNED_BYTE, pixels.AsSpan());
            }
            context.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAX_LEVEL, Content.Atlas.MipLevels - 1);
            context.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.LINEAR_MIPMAP_LINEAR);
            context.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
            context.BindTexture(GL.TEXTURE_2D, null);
        }
        protected override void DoDispose()
        {
            context.DeleteTexture(atlas);
            atlas?.Dispose();
        }
        public void Use() => context.BindTexture(GL.TEXTURE_2D, atlas);
        public int GetSourceId(char character) => sourceIds.TryGetValue(character, out var sourceId) ? sourceId : -1;
    }
}
