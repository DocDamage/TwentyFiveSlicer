using System.Text.Json.Serialization;

namespace TwentyFiveSlicer.Desktop.Models;

public sealed class TwentyFiveSliceData
{
    private static readonly double[] DefaultBorders = { 20d, 40d, 60d, 80d };

    [JsonPropertyName("verticalBorders")]
    public double[] VerticalBorders { get; set; }

    [JsonPropertyName("horizontalBorders")]
    public double[] HorizontalBorders { get; set; }

    public TwentyFiveSliceData()
        : this(null, null)
    {
    }

    public TwentyFiveSliceData(IEnumerable<double>? verticalBorders, IEnumerable<double>? horizontalBorders)
    {
        VerticalBorders = NormalizeAxis(verticalBorders);
        HorizontalBorders = NormalizeAxis(horizontalBorders);
    }

    public TwentyFiveSliceData Clone()
    {
        return new TwentyFiveSliceData(VerticalBorders, HorizontalBorders);
    }

    public static TwentyFiveSliceData CreateDefault()
    {
        return new TwentyFiveSliceData(DefaultBorders, DefaultBorders);
    }

    public static double[] NormalizeAxis(IEnumerable<double>? borders)
    {
        var normalized = (double[])DefaultBorders.Clone();

        if (borders is not null)
        {
            int index = 0;
            foreach (double border in borders)
            {
                if (index >= normalized.Length)
                {
                    break;
                }

                normalized[index] = border;
                index++;
            }
        }

        normalized[0] = ClampToPercent(normalized[0]);
        for (int index = 1; index < normalized.Length; index++)
        {
            normalized[index] = Math.Clamp(normalized[index], normalized[index - 1], 100d);
        }

        for (int index = normalized.Length - 2; index >= 0; index--)
        {
            normalized[index] = Math.Min(normalized[index], normalized[index + 1]);
        }

        return normalized;
    }

    private static double ClampToPercent(double value)
    {
        return Math.Clamp(value, 0d, 100d);
    }
}