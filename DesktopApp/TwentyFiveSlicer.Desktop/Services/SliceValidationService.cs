using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class SliceValidationService
{
    public static IReadOnlyList<SliceValidationMessage> Validate(
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight,
        TwentyFiveSliceData sliceData,
        UnityRuntimeSettings? runtimeSettings = null,
        string? targetKind = null)
    {
        var messages = new List<SliceValidationMessage>();

        if (sourceWidth <= 0d || sourceHeight <= 0d)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "Load an image to enable validation."));
            return messages;
        }

        AxisSegments vertical = BuildAxisSegments("X", sliceData.VerticalBorders, sourceWidth);
        AxisSegments horizontal = BuildAxisSegments("Y", sliceData.HorizontalBorders, sourceHeight);
        SliceSegmentDefinition[] xSegments = TwentyFiveSliceData.NormalizeSegments(sliceData.XSegments, vertical.Sizes.Length);
        SliceSegmentDefinition[] ySegments = TwentyFiveSliceData.NormalizeSegments(sliceData.YSegments, horizontal.Sizes.Length);
        int columns = vertical.Sizes.Length;
        int rows = horizontal.Sizes.Length;
        double fixedColumns = GetFixedSize(vertical.Sizes, xSegments);
        double fixedRows = GetFixedSize(horizontal.Sizes, ySegments);

        if (columns > TwentyFiveSliceData.MaxSegmentsPerAxis || rows > TwentyFiveSliceData.MaxSegmentsPerAxis)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"The grid is {columns} x {rows}, above the {TwentyFiveSliceData.MaxSegmentsPerAxis} x {TwentyFiveSliceData.MaxSegmentsPerAxis} safety cap."));
        }
        else if (columns != 5 || rows != 5)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, $"Variable grid is {columns} x {rows} cells."));
        }

        if (fixedColumns > targetWidth)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"The fixed columns need {fixedColumns:0}px, more than the {targetWidth:0}px target width."));
        }

        if (fixedRows > targetHeight)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"The fixed rows need {fixedRows:0}px, more than the {targetHeight:0}px target height."));
        }

        AddGuideHazards(messages, vertical);
        AddGuideHazards(messages, horizontal);
        AddTinySegmentHazards(messages, vertical);
        AddTinySegmentHazards(messages, horizontal);

        int hiddenColumns = xSegments.Count(segment => segment.Mode == SliceSegmentMode.Hidden);
        int hiddenRows = ySegments.Count(segment => segment.Mode == SliceSegmentMode.Hidden);
        if (hiddenColumns > 0 || hiddenRows > 0)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, $"Hidden segments: {hiddenColumns} columns, {hiddenRows} rows."));
        }

        if (runtimeSettings is not null || !string.IsNullOrWhiteSpace(targetKind))
        {
            AddRuntimeMetadataHazards(messages, sourceWidth, sourceHeight, targetWidth, targetHeight, UnityRuntimeSettings.Normalize(runtimeSettings), targetKind);
        }

        if (messages.Count == 0)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "No slice warnings for this target."));
        }

        return messages;
    }

    private static AxisSegments BuildAxisSegments(string axis, IReadOnlyList<double> borders, double totalSize)
    {
        double[] normalized = TwentyFiveSliceData.NormalizeAxis(borders);
        double[] stops = [0d, .. normalized, 100d];
        var sizes = new double[stops.Length - 1];
        for (int index = 0; index < sizes.Length; index++)
        {
            sizes[index] = (stops[index + 1] - stops[index]) * totalSize / 100d;
        }

        return new AxisSegments(axis, stops, sizes, totalSize);
    }

    private static void AddGuideHazards(List<SliceValidationMessage> messages, AxisSegments axis)
    {
        for (int index = 1; index < axis.StopsPercent.Length - 1; index++)
        {
            double percent = axis.StopsPercent[index];
            double pixel = axis.TotalPixels * percent / 100d;
            double distanceToEdge = Math.Min(pixel, axis.TotalPixels - pixel);
            if (distanceToEdge > 0d && distanceToEdge < 2d)
            {
                messages.Add(new SliceValidationMessage(
                    SliceValidationSeverity.Warning,
                    $"{axis.Axis} guide {index} is {distanceToEdge:0.##}px from a source edge."));
            }
        }
    }

    private static void AddTinySegmentHazards(List<SliceValidationMessage> messages, AxisSegments axis)
    {
        for (int index = 0; index < axis.Sizes.Length; index++)
        {
            double size = axis.Sizes[index];
            if (size > 0d && size < 2d)
            {
                messages.Add(new SliceValidationMessage(
                    SliceValidationSeverity.Warning,
                    $"{axis.Axis} segment {index} is only {size:0.##}px wide ({axis.StopsPercent[index]:0.##}% to {axis.StopsPercent[index + 1]:0.##}%)."));
            }
        }
    }

    private static double GetFixedSize(IReadOnlyList<double> sizes, IReadOnlyList<SliceSegmentDefinition> segments)
    {
        double total = 0d;
        for (int index = 0; index < sizes.Count; index++)
        {
            if (segments[index].Mode == SliceSegmentMode.Fixed)
            {
                total += sizes[index];
            }
        }

        return total;
    }

    private static void AddRuntimeMetadataHazards(
        List<SliceValidationMessage> messages,
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight,
        UnityRuntimeSettings settings,
        string? targetKind)
    {
        if (settings.PixelsPerUnit < 1d || settings.PixelsPerUnit > 1024d)
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"Pixels per unit {settings.PixelsPerUnit:0.###} is unusual for Unity handoff."));
        }

        if (!settings.UseSpritePivot &&
            (Math.Abs(settings.CustomPivotX) > sourceWidth || Math.Abs(settings.CustomPivotY) > sourceHeight))
        {
            messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"Custom pivot ({settings.CustomPivotX:0.###}, {settings.CustomPivotY:0.###}) sits outside the {sourceWidth:0.###} x {sourceHeight:0.###}px source bounds."));
        }

        string normalizedTargetKind = string.IsNullOrWhiteSpace(targetKind) ? DesktopHandoffEnvelope.DefaultTargetKind : targetKind.Trim();
        switch (normalizedTargetKind)
        {
            case DesktopHandoffEnvelope.DefaultTargetKind:
                messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "Sprite Asset profile preserves runtime metadata for handoff, but Unity will not apply component-specific settings until you choose a component target."));
                break;

            case UnityTargetProfileCatalog.TwentyFiveSliceImageKind:
                if (!string.Equals(settings.SortingLayerName, "Default", StringComparison.OrdinalIgnoreCase) || settings.SortingOrder != 0)
                {
                    messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "TwentyFiveSliceImage uses Canvas ordering, so sorting layer/order metadata is ignored on import."));
                }

                if (!settings.UseSpritePivot || Math.Abs(settings.CustomPivotX) > 0.001d || Math.Abs(settings.CustomPivotY) > 0.001d)
                {
                    messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "TwentyFiveSliceImage ignores custom pivot overrides; the UI RectTransform controls the effective pivot."));
                }

                if (HasAnyRaycastPadding(settings) && IsLargeRaycastPadding(settings, targetWidth, targetHeight))
                {
                    messages.Add(new SliceValidationMessage(SliceValidationSeverity.Warning, $"Raycast padding is large relative to the {targetWidth:0.###} x {targetHeight:0.###}px target."));
                }

                break;

            case UnityTargetProfileCatalog.TwentyFiveSliceSpriteRendererKind:
                if (settings.RaycastTarget || HasAnyRaycastPadding(settings))
                {
                    messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "TwentyFiveSliceSpriteRenderer ignores raycast target and raycast padding metadata."));
                }

                break;

            case UnityTargetProfileCatalog.SpriteRendererKind:
                if (settings.RaycastTarget || HasAnyRaycastPadding(settings))
                {
                    messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "SpriteRenderer ignores raycast target and raycast padding metadata."));
                }

                if (!settings.UseSpritePivot || Math.Abs(settings.CustomPivotX) > 0.001d || Math.Abs(settings.CustomPivotY) > 0.001d)
                {
                    messages.Add(new SliceValidationMessage(SliceValidationSeverity.Info, "SpriteRenderer ignores custom pivot overrides from desktop handoff metadata."));
                }

                break;
        }
    }

    private static bool HasAnyRaycastPadding(UnityRuntimeSettings settings)
    {
        return Math.Abs(settings.RaycastPaddingLeft) > 0.001d ||
               Math.Abs(settings.RaycastPaddingBottom) > 0.001d ||
               Math.Abs(settings.RaycastPaddingRight) > 0.001d ||
               Math.Abs(settings.RaycastPaddingTop) > 0.001d;
    }

    private static bool IsLargeRaycastPadding(UnityRuntimeSettings settings, double targetWidth, double targetHeight)
    {
        double maxHorizontal = Math.Max(Math.Abs(settings.RaycastPaddingLeft), Math.Abs(settings.RaycastPaddingRight));
        double maxVertical = Math.Max(Math.Abs(settings.RaycastPaddingBottom), Math.Abs(settings.RaycastPaddingTop));
        return maxHorizontal > Math.Max(8d, targetWidth / 2d) || maxVertical > Math.Max(8d, targetHeight / 2d);
    }

    private sealed record AxisSegments(string Axis, double[] StopsPercent, double[] Sizes, double TotalPixels);
}
