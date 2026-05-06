namespace TwentyFiveSlicer.Desktop.Models;

public sealed class UnityRuntimeSettings
{
    public string TintHex { get; set; } = "#FFFFFFFF";

    public double PixelsPerUnit { get; set; } = 100d;

    public bool UseSpritePivot { get; set; } = true;

    public double CustomPivotX { get; set; }

    public double CustomPivotY { get; set; }

    public string SortingLayerName { get; set; } = "Default";

    public int SortingOrder { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public bool RaycastTarget { get; set; } = true;

    public double RaycastPaddingLeft { get; set; }

    public double RaycastPaddingBottom { get; set; }

    public double RaycastPaddingRight { get; set; }

    public double RaycastPaddingTop { get; set; }

    public UnityRuntimeSettings Clone()
    {
        return Normalize(this);
    }

    public static UnityRuntimeSettings Normalize(UnityRuntimeSettings? settings)
    {
        settings ??= new UnityRuntimeSettings();

        return new UnityRuntimeSettings
        {
            TintHex = NormalizeTintHex(settings.TintHex),
            PixelsPerUnit = NormalizePixelsPerUnit(settings.PixelsPerUnit),
            UseSpritePivot = settings.UseSpritePivot,
            CustomPivotX = Round(settings.CustomPivotX),
            CustomPivotY = Round(settings.CustomPivotY),
            SortingLayerName = string.IsNullOrWhiteSpace(settings.SortingLayerName) ? "Default" : settings.SortingLayerName.Trim(),
            SortingOrder = settings.SortingOrder,
            MaterialName = settings.MaterialName?.Trim() ?? string.Empty,
            RaycastTarget = settings.RaycastTarget,
            RaycastPaddingLeft = Round(settings.RaycastPaddingLeft),
            RaycastPaddingBottom = Round(settings.RaycastPaddingBottom),
            RaycastPaddingRight = Round(settings.RaycastPaddingRight),
            RaycastPaddingTop = Round(settings.RaycastPaddingTop)
        };
    }

    public static string NormalizeTintHex(string? value)
    {
        return TryParseTintHex(value, out byte alpha, out byte red, out byte green, out byte blue)
            ? $"#{alpha:X2}{red:X2}{green:X2}{blue:X2}"
            : "#FFFFFFFF";
    }

    public static bool TryParseTintHex(string? value, out byte alpha, out byte red, out byte green, out byte blue)
    {
        alpha = 255;
        red = 255;
        green = 255;
        blue = 255;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();
        if (normalized.StartsWith('#'))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length == 6)
        {
            return byte.TryParse(normalized.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out red) &&
                   byte.TryParse(normalized.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out green) &&
                   byte.TryParse(normalized.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out blue);
        }

        if (normalized.Length == 8)
        {
            return byte.TryParse(normalized.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out alpha) &&
                   byte.TryParse(normalized.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out red) &&
                   byte.TryParse(normalized.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out green) &&
                   byte.TryParse(normalized.AsSpan(6, 2), System.Globalization.NumberStyles.HexNumber, null, out blue);
        }

        return false;
    }

    private static double NormalizePixelsPerUnit(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
        {
            return 100d;
        }

        return Math.Clamp(Round(value), 0.001d, 100000d);
    }

    private static double Round(double value)
    {
        return Math.Round(double.IsNaN(value) || double.IsInfinity(value) ? 0d : value, 3);
    }
}