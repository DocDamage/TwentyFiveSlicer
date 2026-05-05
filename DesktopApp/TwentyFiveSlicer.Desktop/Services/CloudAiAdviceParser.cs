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
            int end = advice.IndexOf('}', start + 1);
            while (end > start)
            {
                string candidate = advice[start..(end + 1)];
                if (TryParseJsonObject(candidate, out data))
                {
                    return true;
                }

                end = advice.IndexOf('}', end + 1);
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
            if (!TryReadArray(root, ["verticalBorders", "vertical_borders", "vertical borders"], out double[]? vertical) ||
                !TryReadArray(root, ["horizontalBorders", "horizontal_borders", "horizontal borders"], out double[]? horizontal))
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

    private static bool TryReadArray(JsonElement root, string[] propertyNames, out double[]? values)
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

        if (!found || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() < 4)
        {
            return false;
        }

        var parsed = new List<double>();
        foreach (JsonElement item in array.EnumerateArray().Take(4))
        {
            if (!TryReadNumber(item, out double value))
            {
                return false;
            }

            parsed.Add(value);
        }

        values = parsed.ToArray();
        return values.Length == 4;
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
