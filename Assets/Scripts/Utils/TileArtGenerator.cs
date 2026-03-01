using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates simple procedural tile sprites per ring at runtime.
/// Ring 0 (center): Park/garden — green with dot pattern
/// Ring 1 (inner): Urban/city — brown/grey with grid pattern
/// Ring 2 (outer): Industrial — dark grey/rust with diagonal pattern
/// </summary>
public static class TileArtGenerator
{
    static readonly Dictionary<int, Sprite> _cache = new();
    const int Size = 64;

    public static Sprite GetTileSprite(int ring)
    {
        ring = Mathf.Clamp(ring, 0, 2);
        if (_cache.TryGetValue(ring, out var cached)) return cached;

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        switch (ring)
        {
            case 0: FillPark(tex); break;
            case 1: FillUrban(tex); break;
            default: FillIndustrial(tex); break;
        }

        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        _cache[ring] = sprite;
        return sprite;
    }

    static void FillPark(Texture2D tex)
    {
        var baseColor = new Color(0.25f, 0.55f, 0.2f);
        var dotColor  = new Color(0.35f, 0.65f, 0.25f);
        Fill(tex, baseColor);
        // Scattered dot pattern (grass tufts)
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            if ((x + y * 3) % 7 == 0)
                tex.SetPixel(x, y, dotColor);
        }
        // Border
        DrawBorder(tex, new Color(0.15f, 0.4f, 0.12f), 2);
    }

    static void FillUrban(Texture2D tex)
    {
        var baseColor = new Color(0.45f, 0.4f, 0.35f);
        var gridColor = new Color(0.35f, 0.3f, 0.28f);
        Fill(tex, baseColor);
        // Grid pattern (roads/blocks)
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            if (x % 8 == 0 || y % 8 == 0)
                tex.SetPixel(x, y, gridColor);
        }
        DrawBorder(tex, new Color(0.3f, 0.25f, 0.2f), 2);
    }

    static void FillIndustrial(Texture2D tex)
    {
        var baseColor = new Color(0.3f, 0.28f, 0.25f);
        var stripeColor = new Color(0.4f, 0.3f, 0.2f);
        Fill(tex, baseColor);
        // Diagonal stripe pattern (hazard/industrial)
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            if ((x + y) % 10 < 3)
                tex.SetPixel(x, y, stripeColor);
        }
        DrawBorder(tex, new Color(0.2f, 0.18f, 0.15f), 2);
    }

    static void Fill(Texture2D tex, Color color)
    {
        var pixels = new Color[Size * Size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
    }

    static void DrawBorder(Texture2D tex, Color color, int width)
    {
        for (int i = 0; i < width; i++)
        {
            for (int x = 0; x < Size; x++)
            {
                tex.SetPixel(x, i, color);
                tex.SetPixel(x, Size - 1 - i, color);
            }
            for (int y = 0; y < Size; y++)
            {
                tex.SetPixel(i, y, color);
                tex.SetPixel(Size - 1 - i, y, color);
            }
        }
    }
}
