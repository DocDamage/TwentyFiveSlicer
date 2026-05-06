using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static partial class CloudAiAdviceParser
{
    public static bool TryParseSliceData(string advice, out TwentyFiveSliceData? data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(advice))
        {
            return false;
        }

        return TryParseJsonSliceData(advice, out data) ||
            TryParseInlineSliceData(advice, out data);
    }

    private static bool TryParseJsonSliceData(string advice, out TwentyFiveSliceData? data)
    {
        data = null;
        int start = advice.IndexOf('{');
        while (start >= 0)
        {
            int depth = 0;
            for (int index = start; index < advice.Length; index++)
            {
                if (advice[index] == '{')
                {
                    depth++;
                }

                if (advice[index] != '}')
                {
                    continue;
                }

                depth--;
                if (depth == 0)
                {
                    string candidate = advice[start..(index + 1)];
                    if (TryParseJsonObject(candidate, out data))
                    {
                        return true;
                    }

                    break;
                }
            }

            start = advice.IndexOf('{', start + 1);
        }

        return false;
    }

    private static bool TryParseJsonObject(string json, out TwentyFiveSliceData? data)
    {
        data = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (TryReadArray(root, ["xGuidesPercent", "x_guides_percent", "x guides percent"], minimumCount: 1, maximumCount: null, out double[]? xGuides) &&
                TryReadArray(root, ["yGuidesPercent", "y_guides_percent", "y guides percent"], minimumCount: 1, maximumCount: null, out double[]? yGuides))
            {
                SliceSegmentDefinition[]? xSegments = TryReadSegments(root, ["xSegments", "x_segments", "x segments"]);
                SliceSegmentDefinition[]? ySegments = TryReadSegments(root, ["ySegments", "y_segments", "y segments"]);
                data = new TwentyFiveSliceData(xGuides, yGuides, xSegments, ySegments);
                return true;
            }

            if (!TryReadArray(root, ["verticalBorders", "vertical_borders", "vertical borders"], minimumCount: 4, maximumCount: 4, out double[]? vertical) ||
                !TryReadArray(root, ["horizontalBorders", "horizontal_borders", "horizontal borders"], minimumCount: 4, maximumCount: 4, out double[]? horizontal))
            {
                return false;
            }

            data = new TwentyFiveSliceData(vertical, horizontal);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseInlineSliceData(string advice, out TwentyFiveSliceData? data)
    {
        data = null;
        Match verticalMatch = VerticalBordersRegex().Match(advice);
        Match horizontalMatch = HorizontalBordersRegex().Match(advice);
        if (!verticalMatch.Success || !horizontalMatch.Success)
        {
            return false;
        }

        if (!TryParseNumberList(verticalMatch.Groups["values"].Value, out double[]? vertical) ||
            !TryParseNumberList(horizontalMatch.Groups["values"].Value, out double[]? horizontal))
        {
            return false;
        }

        data = new TwentyFiveSliceData(vertical, horizontal);
        return true;
    }

    private static bool TryReadArray(JsonElement root, string[] propertyNames, int minimumCount, int? maximumCount, out double[]? values)
    {
        values = null;
        JsonElement array = default;
        bool found = false;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (propertyNames.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                array = property.Value;
                found = true;
                break;
            }
        }

        if (!found || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() < minimumCount)
        {
            return false;
        }

        var parsed = new List<double>();
        IEnumerable<JsonElement> items = maximumCount is int max
            ? array.EnumerateArray().Take(max)
            : array.EnumerateArray();
        foreach (JsonElement item in items)
        {
            if (!TryReadNumber(item, out double value))
            {
                return false;
            }

            parsed.Add(value);
        }

        values = parsed.ToArray();
        return values.Length >= minimumCount;
    }

    private static SliceSegmentDefinition[]? TryReadSegments(JsonElement root, string[] propertyNames)
    {
        JsonElement array = default;
        bool found = false;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (propertyNames.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                array = property.Value;
                found = true;
                break;
            }
        }

        if (!found || array.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var segments = new List<SliceSegmentDefinition>();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("mode", out JsonElement modeElement) &&
                modeElement.ValueKind == JsonValueKind.String &&
                Enum.TryParse(modeElement.GetString(), ignoreCase: true, out SliceSegmentMode mode))
            {
                segments.Add(new SliceSegmentDefinition(mode));
                continue;
            }

            if (item.ValueKind == JsonValueKind.String &&
                Enum.TryParse(item.GetString(), ignoreCase: true, out mode))
            {
                segments.Add(new SliceSegmentDefinition(mode));
            }
        }

        return segments.Count == 0 ? null : segments.ToArray();
    }

    private static bool TryReadNumber(JsonElement element, out double value)
    {
        value = 0d;
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetDouble(out value);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return TryParsePercentNumber(element.GetString(), out value);
        }

        return false;
    }

    private static bool TryParseNumberList(string text, out double[]? values)
    {
        values = NumberRegex().Matches(text)
            .Select(match => double.Parse(match.Value.TrimEnd('%').Trim(), CultureInfo.InvariantCulture))
            .Take(4)
            .ToArray();

        return values.Length == 4;
    }

    private static bool TryParsePercentNumber(string? text, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(text) &&
            double.TryParse(text.Trim().TrimEnd('%').Trim(), CultureInfo.InvariantCulture, out value);
    }

    [GeneratedRegex(@"vertical\s*_?\s*borders\s*[:=]\s*(?:\[(?<values>[^\]]+)\]|(?<values>[-+0-9.,%/\s]+))", RegexOptions.IgnoreCase)]
    private static partial Regex VerticalBordersRegex();

    [GeneratedRegex(@"horizontal\s*_?\s*borders\s*[:=]\s*(?:\[(?<values>[^\]]+)\]|(?<values>[-+0-9.,%/\s]+))", RegexOptions.IgnoreCase)]
    private static partial Regex HorizontalBordersRegex();

    [GeneratedRegex(@"[-+]?\d+(?:\.\d+)?%?")]
    private static partial Regex NumberRegex();
}
