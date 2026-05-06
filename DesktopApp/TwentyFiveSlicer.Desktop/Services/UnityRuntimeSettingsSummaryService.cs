using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class UnityRuntimeSettingsSummaryService
{
    public static string Build(UnityRuntimeSettings? settings, BitmapSource? sourceImage, SpriteAssetContext? spriteContext, double targetWidth, double targetHeight)
    {
        UnityRuntimeSettings normalized = UnityRuntimeSettings.Normalize(settings);

        double sourceWidth = sourceImage?.PixelWidth ?? spriteContext?.SpriteRect.Width ?? Math.Max(1d, targetWidth);
        double sourceHeight = sourceImage?.PixelHeight ?? spriteContext?.SpriteRect.Height ?? Math.Max(1d, targetHeight);
        double targetWidthPixels = Math.Max(1d, targetWidth);
        double targetHeightPixels = Math.Max(1d, targetHeight);
        double pixelsPerUnit = normalized.PixelsPerUnit;

        double pivotX = normalized.UseSpritePivot ? spriteContext?.PivotPixels.X ?? (sourceWidth / 2d) : normalized.CustomPivotX;
        double pivotY = normalized.UseSpritePivot ? spriteContext?.PivotPixels.Y ?? (sourceHeight / 2d) : normalized.CustomPivotY;
        double widthUnits = targetWidthPixels / pixelsPerUnit;
        double heightUnits = targetHeightPixels / pixelsPerUnit;
        double minX = -pivotX / pixelsPerUnit;
        double minY = -pivotY / pixelsPerUnit;
        double maxX = minX + widthUnits;
        double maxY = minY + heightUnits;
        string pivotLabel = normalized.UseSpritePivot
            ? spriteContext is null
                ? $"sprite center ({pivotX:0.###} px, {pivotY:0.###} px)"
                : $"imported sprite ({pivotX:0.###} px, {pivotY:0.###} px)"
            : $"custom ({pivotX:0.###} px, {pivotY:0.###} px)";
        string materialLabel = string.IsNullOrWhiteSpace(normalized.MaterialName) ? "Sprites/Default" : normalized.MaterialName;

        return $"SpriteRenderer: {widthUnits:0.###} x {heightUnits:0.###} units at {pixelsPerUnit:0.###} PPU. Pivot {pivotLabel}. Bounds min ({minX:0.###}, {minY:0.###}) max ({maxX:0.###}, {maxY:0.###}).{Environment.NewLine}" +
               $"Unity metadata: sorting {normalized.SortingLayerName}/{normalized.SortingOrder}, material {materialLabel}, raycast {(normalized.RaycastTarget ? "on" : "off")} pad L{normalized.RaycastPaddingLeft:0.###} B{normalized.RaycastPaddingBottom:0.###} R{normalized.RaycastPaddingRight:0.###} T{normalized.RaycastPaddingTop:0.###}.";
    }
}