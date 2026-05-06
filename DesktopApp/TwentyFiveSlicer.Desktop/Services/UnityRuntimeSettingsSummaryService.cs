using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class UnityRuntimeSettingsSummaryService
{
    public static string Build(UnityRuntimeSettings? settings, BitmapSource? sourceImage, SpriteAssetContext? spriteContext, double targetWidth, double targetHeight, string? targetKind = null)
    {
        UnityRuntimeSettings normalized = UnityRuntimeSettings.Normalize(settings);
        string normalizedTargetKind = string.IsNullOrWhiteSpace(targetKind) ? UnityTargetProfileCatalog.SpriteRendererKind : targetKind.Trim();
        string profileName = UnityTargetProfileCatalog.GetDisplayName(normalizedTargetKind);

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

        return normalizedTargetKind switch
        {
            UnityTargetProfileCatalog.TwentyFiveSliceImageKind =>
                $"{profileName}: {targetWidthPixels:0} x {targetHeightPixels:0} UI pixels. Material {materialLabel}, raycast {(normalized.RaycastTarget ? "on" : "off")} pad L{normalized.RaycastPaddingLeft:0.###} B{normalized.RaycastPaddingBottom:0.###} R{normalized.RaycastPaddingRight:0.###} T{normalized.RaycastPaddingTop:0.###}.{Environment.NewLine}" +
                $"Bridge metadata: pivot {pivotLabel}, {pixelsPerUnit:0.###} PPU. Sorting layer/order and custom pivot overrides are preserved for handoff but are not applied by TwentyFiveSliceImage.",

            UnityTargetProfileCatalog.TwentyFiveSliceSpriteRendererKind =>
                $"{profileName}: {widthUnits:0.###} x {heightUnits:0.###} units at {pixelsPerUnit:0.###} PPU. Pivot {pivotLabel}. Bounds min ({minX:0.###}, {minY:0.###}) max ({maxX:0.###}, {maxY:0.###}).{Environment.NewLine}" +
                $"World metadata: sorting {normalized.SortingLayerName}/{normalized.SortingOrder}, material {materialLabel}. Raycast metadata is ignored on renderer targets.",

            UnityTargetProfileCatalog.SpriteRendererKind =>
                $"{profileName}: {widthUnits:0.###} x {heightUnits:0.###} units at {pixelsPerUnit:0.###} PPU. Pivot {pivotLabel}. Bounds min ({minX:0.###}, {minY:0.###}) max ({maxX:0.###}, {maxY:0.###}).{Environment.NewLine}" +
                $"Renderer metadata: sorting {normalized.SortingLayerName}/{normalized.SortingOrder}, material {materialLabel}. Custom pivot and raycast metadata are preserved for handoff but not applied on a plain SpriteRenderer.",

            _ =>
                $"{profileName}: slice and sprite metadata handoff only.{Environment.NewLine}" +
                "Runtime metadata is preserved for Unity import, but no component-specific settings will apply until you choose a Unity component target profile."
        };
    }
}