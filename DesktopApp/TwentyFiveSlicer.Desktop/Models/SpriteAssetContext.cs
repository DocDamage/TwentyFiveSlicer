namespace TwentyFiveSlicer.Desktop.Models;

public sealed class SpriteAssetContext
{
    public int SchemaVersion { get; set; } = 1;

    public string SpriteName { get; set; } = string.Empty;

    public string TexturePath { get; set; } = string.Empty;

    public string SidecarPath { get; set; } = string.Empty;

    public int TextureWidth { get; set; }

    public int TextureHeight { get; set; }

    public string CoordinateOrigin { get; set; } = "bottom-left";

    public SpritePixelRect SpriteRect { get; set; } = new();

    public SpritePixelPoint PivotPixels { get; set; } = new();

    public double PixelsPerUnit { get; set; } = 100d;

    public bool UsesAtlasRegion =>
        SpriteRect.X != 0 ||
        SpriteRect.Y != 0 ||
        SpriteRect.Width != TextureWidth ||
        SpriteRect.Height != TextureHeight;

    public SpriteAssetContext Clone()
    {
        return new SpriteAssetContext
        {
            SchemaVersion = SchemaVersion,
            SpriteName = SpriteName,
            TexturePath = TexturePath,
            SidecarPath = SidecarPath,
            TextureWidth = TextureWidth,
            TextureHeight = TextureHeight,
            CoordinateOrigin = CoordinateOrigin,
            SpriteRect = new SpritePixelRect
            {
                X = SpriteRect.X,
                Y = SpriteRect.Y,
                Width = SpriteRect.Width,
                Height = SpriteRect.Height
            },
            PivotPixels = new SpritePixelPoint
            {
                X = PivotPixels.X,
                Y = PivotPixels.Y
            },
            PixelsPerUnit = PixelsPerUnit
        };
    }

    public static SpriteAssetContext Normalize(SpriteAssetContext? context, int textureWidth, int textureHeight, string? texturePath = null, string? sidecarPath = null)
    {
        context ??= new SpriteAssetContext();

        int normalizedTextureWidth = Math.Max(1, textureWidth);
        int normalizedTextureHeight = Math.Max(1, textureHeight);
        string origin = NormalizeOrigin(context.CoordinateOrigin);

        int rectWidth = Math.Clamp(context.SpriteRect.Width <= 0 ? normalizedTextureWidth : context.SpriteRect.Width, 1, normalizedTextureWidth);
        int rectHeight = Math.Clamp(context.SpriteRect.Height <= 0 ? normalizedTextureHeight : context.SpriteRect.Height, 1, normalizedTextureHeight);

        int rawX = Math.Clamp(context.SpriteRect.X, 0, normalizedTextureWidth - rectWidth);
        int rawY = Math.Clamp(context.SpriteRect.Y, 0, normalizedTextureHeight - rectHeight);
        int topLeftY = origin == "bottom-left"
            ? Math.Clamp(normalizedTextureHeight - rawY - rectHeight, 0, normalizedTextureHeight - rectHeight)
            : rawY;

        return new SpriteAssetContext
        {
            SchemaVersion = context.SchemaVersion <= 0 ? 1 : context.SchemaVersion,
            SpriteName = context.SpriteName?.Trim() ?? string.Empty,
            TexturePath = texturePath ?? context.TexturePath?.Trim() ?? string.Empty,
            SidecarPath = sidecarPath ?? context.SidecarPath?.Trim() ?? string.Empty,
            TextureWidth = normalizedTextureWidth,
            TextureHeight = normalizedTextureHeight,
            CoordinateOrigin = "top-left",
            SpriteRect = new SpritePixelRect
            {
                X = rawX,
                Y = topLeftY,
                Width = rectWidth,
                Height = rectHeight
            },
            PivotPixels = new SpritePixelPoint
            {
                X = Math.Round(Math.Clamp(double.IsFinite(context.PivotPixels.X) ? context.PivotPixels.X : 0d, 0d, rectWidth), 3),
                Y = Math.Round(Math.Clamp(double.IsFinite(context.PivotPixels.Y) ? context.PivotPixels.Y : 0d, 0d, rectHeight), 3)
            },
            PixelsPerUnit = NormalizePixelsPerUnit(context.PixelsPerUnit)
        };
    }

    private static double NormalizePixelsPerUnit(double value)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            return 100d;
        }

        return Math.Clamp(Math.Round(value, 3), 0.001d, 100000d);
    }

    private static string NormalizeOrigin(string? value)
    {
        return string.Equals(value?.Trim(), "top-left", StringComparison.OrdinalIgnoreCase)
            ? "top-left"
            : "bottom-left";
    }
}

public sealed class SpritePixelRect
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}

public sealed class SpritePixelPoint
{
    public double X { get; set; }

    public double Y { get; set; }
}