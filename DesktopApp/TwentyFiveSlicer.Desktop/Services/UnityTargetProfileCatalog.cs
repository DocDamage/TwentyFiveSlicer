using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed record UnityTargetProfileDescriptor(string TargetKind, string DisplayName, string Description);

public static class UnityTargetProfileCatalog
{
    public const string TwentyFiveSliceImageKind = "twentyFiveSliceImage";
    public const string TwentyFiveSliceSpriteRendererKind = "twentyFiveSliceSpriteRenderer";
    public const string SpriteRendererKind = "spriteRenderer";

    private static readonly UnityTargetProfileDescriptor[] Profiles =
    [
        new(
            DesktopHandoffEnvelope.DefaultTargetKind,
            "Sprite Asset",
            "Bridge profile only. Slice, sprite, and runtime metadata are preserved for Unity handoff, but no component-specific runtime settings will apply until you choose a Unity component target."),
        new(
            TwentyFiveSliceImageKind,
            "TwentyFiveSliceImage",
            "UI image profile. Tint, material, and raycast settings matter most. Sorting layer/order and custom pivot overrides are preserved for handoff metadata but are not applied by the Unity UI component."),
        new(
            TwentyFiveSliceSpriteRendererKind,
            "TwentyFiveSliceSpriteRenderer",
            "World-space 25-slice renderer profile. Tint, pixels per unit, pivot, sorting, and material are meaningful. Raycast metadata is ignored."),
        new(
            SpriteRendererKind,
            "SpriteRenderer",
            "Plain sprite renderer bridge profile. Tint, sorting, and material are meaningful. Custom pivot and raycast metadata are preserved for handoff but not applied by Unity."),
    ];

    public static IReadOnlyList<UnityTargetProfileDescriptor> GetAll() => Profiles;

    public static UnityTargetProfileDescriptor? Find(string? targetKind)
    {
        if (string.IsNullOrWhiteSpace(targetKind))
        {
            return Profiles[0];
        }

        return Profiles.FirstOrDefault(profile => string.Equals(profile.TargetKind, targetKind.Trim(), StringComparison.Ordinal));
    }

    public static string GetDisplayName(string? targetKind)
    {
        if (Find(targetKind) is UnityTargetProfileDescriptor profile)
        {
            return profile.DisplayName;
        }

        return string.IsNullOrWhiteSpace(targetKind) ? "Sprite Asset" : targetKind.Trim();
    }

    public static string GetDescription(string? targetKind)
    {
        if (Find(targetKind) is UnityTargetProfileDescriptor profile)
        {
            return profile.Description;
        }

        return string.IsNullOrWhiteSpace(targetKind)
            ? Profiles[0].Description
            : $"Unknown Unity target kind '{targetKind.Trim()}'. The desktop app will preserve it for handoff, but only generic profile warnings are available.";
    }

    public static UnityRuntimeSettings ApplyRecommendedDefaults(string? targetKind, UnityRuntimeSettings? settings, SpriteAssetContext? spriteContext)
    {
        UnityRuntimeSettings normalized = UnityRuntimeSettings.Normalize(settings);
        double preferredPixelsPerUnit = spriteContext is not null && spriteContext.PixelsPerUnit > 0d
            ? spriteContext.PixelsPerUnit
            : normalized.PixelsPerUnit;

        var defaults = new UnityRuntimeSettings
        {
            TintHex = normalized.TintHex,
            PixelsPerUnit = preferredPixelsPerUnit,
            UseSpritePivot = true,
            CustomPivotX = 0d,
            CustomPivotY = 0d,
            SortingLayerName = "Default",
            SortingOrder = 0,
            MaterialName = string.Empty,
            RaycastTarget = string.Equals(targetKind?.Trim(), TwentyFiveSliceImageKind, StringComparison.Ordinal),
            RaycastPaddingLeft = 0d,
            RaycastPaddingBottom = 0d,
            RaycastPaddingRight = 0d,
            RaycastPaddingTop = 0d,
        };

        return UnityRuntimeSettings.Normalize(defaults);
    }
}