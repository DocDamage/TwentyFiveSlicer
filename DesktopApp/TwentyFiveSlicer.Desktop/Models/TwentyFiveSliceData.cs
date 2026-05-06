using System.Text.Json;
using System.Text.Json.Serialization;

namespace TwentyFiveSlicer.Desktop.Models;

public sealed class TwentyFiveSliceData : IJsonOnDeserialized
{
    public const int CurrentSchemaVersion = 2;
    public const int MaxSegmentsPerAxis = 100;

    private static readonly double[] DefaultGuides = { 20d, 40d, 60d, 80d };

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("xGuidesPercent")]
    public double[] XGuidesPercent { get; set; }

    [JsonPropertyName("yGuidesPercent")]
    public double[] YGuidesPercent { get; set; }

    [JsonPropertyName("xSegments")]
    public SliceSegmentDefinition[] XSegments { get; set; }

    [JsonPropertyName("ySegments")]
    public SliceSegmentDefinition[] YSegments { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    [JsonIgnore]
    public double[] VerticalBorders
    {
        get => XGuidesPercent;
        set
        {
            XGuidesPercent = NormalizeAxis(value);
            XSegments = NormalizeSegments(XSegments, XGuidesPercent.Length + 1);
        }
    }

    [JsonIgnore]
    public double[] HorizontalBorders
    {
        get => YGuidesPercent;
        set
        {
            YGuidesPercent = NormalizeAxis(value);
            YSegments = NormalizeSegments(YSegments, YGuidesPercent.Length + 1);
        }
    }

    public TwentyFiveSliceData()
        : this(null, null)
    {
    }

    public TwentyFiveSliceData(
        IEnumerable<double>? xGuidesPercent,
        IEnumerable<double>? yGuidesPercent,
        IEnumerable<SliceSegmentDefinition>? xSegments = null,
        IEnumerable<SliceSegmentDefinition>? ySegments = null)
    {
        SchemaVersion = CurrentSchemaVersion;
        XGuidesPercent = NormalizeAxis(xGuidesPercent);
        YGuidesPercent = NormalizeAxis(yGuidesPercent);
        XSegments = NormalizeSegments(xSegments, XGuidesPercent.Length + 1);
        YSegments = NormalizeSegments(ySegments, YGuidesPercent.Length + 1);
    }

    public TwentyFiveSliceData Clone()
    {
        return new TwentyFiveSliceData(XGuidesPercent, YGuidesPercent, XSegments, YSegments);
    }

    public static TwentyFiveSliceData CreateDefault()
    {
        return new TwentyFiveSliceData(DefaultGuides, DefaultGuides);
    }

    public void OnDeserialized()
    {
        if (ExtensionData is not null)
        {
            if (ExtensionData.TryGetValue("verticalBorders", out JsonElement verticalElement) &&
                TryReadDoubleArray(verticalElement, out double[] vertical))
            {
                XGuidesPercent = vertical;
            }

            if (ExtensionData.TryGetValue("horizontalBorders", out JsonElement horizontalElement) &&
                TryReadDoubleArray(horizontalElement, out double[] horizontal))
            {
                YGuidesPercent = horizontal;
            }

            ExtensionData.Clear();
        }

        SchemaVersion = CurrentSchemaVersion;
        XGuidesPercent = NormalizeAxis(XGuidesPercent);
        YGuidesPercent = NormalizeAxis(YGuidesPercent);
        XSegments = NormalizeSegments(XSegments, XGuidesPercent.Length + 1);
        YSegments = NormalizeSegments(YSegments, YGuidesPercent.Length + 1);
    }

    public static double[] NormalizeAxis(IEnumerable<double>? guides)
    {
        double[] normalized = guides?.Select(ClampToPercent).OrderBy(value => value).ToArray() ??
            (double[])DefaultGuides.Clone();

        if (normalized.Length == 0)
        {
            return [];
        }

        var unique = new List<double>(normalized.Length);
        foreach (double guide in normalized)
        {
            if (guide <= 0d || guide >= 100d)
            {
                continue;
            }

            if (unique.Count == 0 || Math.Abs(unique[^1] - guide) >= 0.01d)
            {
                unique.Add(guide);
            }
        }

        return unique.ToArray();
    }

    public static SliceSegmentDefinition[] NormalizeSegments(IEnumerable<SliceSegmentDefinition>? segments, int requiredCount)
    {
        int count = Math.Max(1, requiredCount);
        SliceSegmentDefinition[] normalized = BuildDefaultSegments(count);
        if (segments is null)
        {
            return normalized;
        }

        int index = 0;
        foreach (SliceSegmentDefinition segment in segments)
        {
            if (index >= normalized.Length)
            {
                break;
            }

            normalized[index] = segment ?? new SliceSegmentDefinition(GetDefaultMode(index, normalized.Length));
            index++;
        }

        return normalized;
    }

    public static void EnsureWithinGridCap(TwentyFiveSliceData data)
    {
        int columns = data.XGuidesPercent.Length + 1;
        int rows = data.YGuidesPercent.Length + 1;
        if (columns > MaxSegmentsPerAxis || rows > MaxSegmentsPerAxis)
        {
            throw new InvalidOperationException($"Variable slice grids are capped at {MaxSegmentsPerAxis} x {MaxSegmentsPerAxis} cells.");
        }
    }

    private static SliceSegmentDefinition[] BuildDefaultSegments(int count)
    {
        var segments = new SliceSegmentDefinition[count];
        for (int index = 0; index < count; index++)
        {
            segments[index] = new SliceSegmentDefinition(GetDefaultMode(index, count));
        }

        return segments;
    }

    private static SliceSegmentMode GetDefaultMode(int index, int count)
    {
        if (index == 0 || index == count - 1)
        {
            return SliceSegmentMode.Fixed;
        }

        return index % 2 == 1 ? SliceSegmentMode.Stretch : SliceSegmentMode.Fixed;
    }

    private static bool TryReadDoubleArray(JsonElement element, out double[] values)
    {
        values = [];
        if (element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parsed = new List<double>();
        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.TryGetDouble(out double value))
            {
                parsed.Add(value);
            }
        }

        values = parsed.ToArray();
        return true;
    }

    private static double ClampToPercent(double value)
    {
        return Math.Clamp(value, 0d, 100d);
    }
}
