using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBFace = HarfBuzzSharp.Face;
using HBFont = HarfBuzzSharp.Font;

namespace LittleQuranTales.Services;

public class ArabicTextRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SKTypeface _skTypeface;
    private readonly HBFace _hbFace;
    private readonly Dictionary<string, Texture2D> _cache = new();
    private readonly Dictionary<string, Vector2> _measureCache = new();

    private const float HiScale = 2f;
    private const int CacheMaxSize = 256;

    private static bool ContainsArabic(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var c in text)
            if (c >= 0x0600 && c <= 0x06FF || c >= 0x0750 && c <= 0x077F || c >= 0x08A0 && c <= 0x08FF || c >= 0xFB50 && c <= 0xFDFF || c >= 0xFE70 && c <= 0xFEFF)
                return true;
        return false;
    }

    public ArabicTextRenderer(GraphicsDevice graphicsDevice, string fontPath)
    {
        _graphicsDevice = graphicsDevice;
        _skTypeface = SKTypeface.FromFile(fontPath);
        if (_skTypeface == null)
            throw new InvalidOperationException($"Failed to load SkiaSharp font: {fontPath}");

        var hbBlob = HarfBuzzSharp.Blob.FromFile(fontPath);
        _hbFace = new HBFace(hbBlob, 0);
    }

    private struct ShapeResult
    {
        public ushort[] GlyphIds;
        public float[] XAdvances;
        public SKPoint[] Positions;
        public float TotalWidth;
        public float TotalHeight;
        public float BaselineY;
    }

    private static readonly ShapeResult _emptyShape = new() { TotalWidth = 1, TotalHeight = 1 };

    private ShapeResult ShapeText(string text, float fontSize)
    {
        int count;
        var result = new ShapeResult();
        if (string.IsNullOrEmpty(text))
        {
            result.TotalHeight = fontSize * 1.2f;
            result.TotalWidth = 1;
            return result;
        }

        using var hbFont = new HBFont(_hbFace);
        hbFont.SetFunctionsOpenType();
        hbFont.SetScale((int)fontSize, (int)fontSize);

        var utf8 = System.Text.Encoding.UTF8.GetBytes(text);
        using var buffer = new HBBuffer();
        var utf8Handle = GCHandle.Alloc(utf8, GCHandleType.Pinned);
        try { buffer.AddUtf8(utf8Handle.AddrOfPinnedObject(), utf8.Length); }
        finally { utf8Handle.Free(); }

        bool isRTL = ContainsArabic(text);
        buffer.Direction = isRTL ? HarfBuzzSharp.Direction.RightToLeft : HarfBuzzSharp.Direction.LeftToRight;
        if (isRTL)
        {
            buffer.Script = HarfBuzzSharp.Script.Arabic;
            buffer.Language = new HarfBuzzSharp.Language("ar");
        }
        else
        {
            buffer.Script = HarfBuzzSharp.Script.Latin;
        }
        hbFont.Shape(buffer);

        var infos = buffer.GlyphInfos;
        var pos = buffer.GlyphPositions;
        count = infos.Length;
        if (count == 0)
        {
            result.TotalHeight = fontSize * 1.2f;
            result.TotalWidth = 1;
            return result;
        }

        var ids = new ushort[count];
        var pts = new SKPoint[count];

        // HarfBuzz RTL returns glyphs in visual left-to-right order
        // with positive XAdvance. Standard LTR positioning loop works.
        float cx = 0;
        float minX = 0, maxRight = 0;
        for (int i = 0; i < count; i++)
        {
            ids[i] = (ushort)infos[i].Codepoint;
            float x = cx + pos[i].XOffset;
            float r = x + pos[i].XAdvance;
            pts[i] = new SKPoint(x, pos[i].YOffset);

            if (i == 0) { minX = x; maxRight = r; }
            else { if (x < minX) minX = x; if (r > maxRight) maxRight = r; }

            cx += pos[i].XAdvance;
        }

        float shiftX = -minX;
        for (int i = 0; i < count; i++)
            pts[i].X += shiftX;

        float tw = maxRight - minX;
        if (tw < 1) tw = 1;

        using var skFont = new SKFont(_skTypeface, fontSize);
        var m = skFont.Metrics;
        float asc = Math.Abs(m.Ascent);
        float desc = Math.Abs(m.Descent);
        float th = asc + desc;
        if (th < fontSize) th = fontSize * 1.5f;

        result.GlyphIds = ids;
        result.XAdvances = null;
        result.Positions = pts;
        result.TotalWidth = tw;
        result.TotalHeight = th;
        result.BaselineY = asc;
        return result;
    }

    public Vector2 MeasureString(string text, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;
        var key = $"{text}|{fontSize:F1}";
        if (_measureCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            var sr = ShapeText(text, fontSize);
            var r = new Vector2(sr.TotalWidth, sr.TotalHeight);
            _measureCache[key] = r;
            return r;
        }
        catch
        {
            var r = new Vector2(1, fontSize * 1.2f);
            _measureCache[key] = r;
            return r;
        }
    }

    private Texture2D Render(string text, float fontSize)
    {
        if (string.IsNullOrEmpty(text))
            return MakeFallback(1, (int)Math.Max(1, fontSize));

        try
        {
            var key = $"{text}|{fontSize:F1}";
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            if (_cache.Count >= CacheMaxSize)
            {
                foreach (var kv in _cache)
                { kv.Value.Dispose(); break; }
                _cache.Clear();
            }

            var sr = ShapeText(text, fontSize * HiScale);
            var ids = sr.GlyphIds;
            int n = ids?.Length ?? 0;

            if (n == 0)
                return MakeFallback(1, (int)Math.Max(1, fontSize));

            var bw = Math.Max(1, (int)Math.Ceiling(sr.TotalWidth));
            var bh = Math.Max(1, (int)Math.Ceiling(sr.TotalHeight));

            using var bitmap = new SKBitmap(bw, bh, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);

            using var skFont = new SKFont(_skTypeface, fontSize * HiScale);
            using var paint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
            };

            using var builder = new SKTextBlobBuilder();
            var runBuf = builder.AllocatePositionedRun(skFont, n, null);
            var gSpan = runBuf.GetGlyphSpan();
            var pSpan = runBuf.GetPositionSpan();

            float textY = sr.BaselineY;

            for (int i = 0; i < n; i++)
            {
                gSpan[i] = ids[i];
                pSpan[i] = new SKPoint(sr.Positions[i].X, sr.Positions[i].Y + textY);
            }

            using var blob = builder.Build();
            canvas.DrawText(blob, 0, 0, paint);
            canvas.Flush();

            var src = new byte[bw * bh * 4];
            Marshal.Copy(bitmap.GetPixels(), src, 0, src.Length);

            var colors = new Color[bw * bh];
            for (int i = 0; i < colors.Length; i++)
            {
                var o = i * 4;
                colors[i] = new Color(src[o + 2], src[o + 1], src[o], src[o + 3]);
            }

            var tex = new Texture2D(_graphicsDevice, bw, bh);
            tex.SetData(colors);
            _cache[key] = tex;
            return tex;
        }
        catch
        {
            return MakeFallback(1, (int)Math.Max(1, fontSize));
        }
    }

    private Texture2D MakeFallback(int w, int h)
    {
        var tex = new Texture2D(_graphicsDevice, w, h);
        tex.SetData(new Color[w * h]);
        return tex;
    }

    public void DrawString(SpriteBatch batch, string text, float fontSize, Vector2 position, Color color)
    {
        var tex = Render(text, fontSize);
        batch.Draw(tex, position, null, color, 0, Vector2.Zero, 1f / HiScale, SpriteEffects.None, 0);
    }

    public void DrawString(SpriteBatch batch, string text, float fontSize, Vector2 position, Color color,
        float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
    {
        var tex = Render(text, fontSize);
        batch.Draw(tex, position, null, color, rotation, origin * HiScale, scale / HiScale, effects, layerDepth);
    }

    public void ClearCache()
    {
        foreach (var kv in _cache)
            kv.Value.Dispose();
        _cache.Clear();
        _measureCache.Clear();
    }

    public void Dispose()
    {
        ClearCache();
        _skTypeface?.Dispose();
        _hbFace?.Dispose();
    }
}
