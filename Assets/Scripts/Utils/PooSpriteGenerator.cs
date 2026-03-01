using UnityEngine;

/// <summary>
/// Generates a simple procedural poo sprite (brown blob with eyes).
/// Used by PooMarker when no sprite asset is assigned.
/// </summary>
public static class PooSpriteGenerator
{
    static Sprite _cachedSprite;
    const int Size = 32;

    public static Sprite GetPooSprite()
    {
        if (_cachedSprite != null) return _cachedSprite;

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Clear to transparent
        var clear = new Color(0, 0, 0, 0);
        var pixels = new Color[Size * Size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        tex.SetPixels(pixels);

        // Draw brown poo blob (stacked circles getting smaller toward top)
        var brown      = new Color(0.45f, 0.28f, 0.12f);
        var darkBrown  = new Color(0.35f, 0.2f, 0.08f);
        var highlight  = new Color(0.55f, 0.38f, 0.18f);

        // Bottom blob
        FillCircle(tex, 16, 8, 10, brown);
        // Middle blob
        FillCircle(tex, 16, 16, 8, brown);
        // Top blob (smaller)
        FillCircle(tex, 16, 22, 5, brown);
        // Tip
        FillCircle(tex, 16, 26, 3, darkBrown);

        // Highlight on top
        FillCircle(tex, 14, 24, 2, highlight);

        // Eyes (white with black pupils)
        FillCircle(tex, 13, 17, 2, Color.white);
        FillCircle(tex, 19, 17, 2, Color.white);
        tex.SetPixel(13, 17, Color.black);
        tex.SetPixel(19, 17, Color.black);

        tex.Apply();
        _cachedSprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        return _cachedSprite;
    }

    static void FillCircle(Texture2D tex, int cx, int cy, int r, Color color)
    {
        int r2 = r * r;
        for (int x = cx - r; x <= cx + r; x++)
        for (int y = cy - r; y <= cy + r; y++)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) continue;
            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2)
                tex.SetPixel(x, y, color);
        }
    }
}
