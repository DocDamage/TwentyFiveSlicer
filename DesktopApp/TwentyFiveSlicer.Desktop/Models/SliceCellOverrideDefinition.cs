using System.Text.Json.Serialization;

namespace TwentyFiveSlicer.Desktop.Models;

public sealed class SliceCellOverrideDefinition
{
    [JsonPropertyName("column")]
    public int Column { get; set; }

    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("sourceRectPercent")]
    public SliceCellRectOverride? SourceRectPercent { get; set; }

    [JsonPropertyName("destinationRectPercent")]
    public SliceCellRectOverride? DestinationRectPercent { get; set; }

    public SliceCellOverrideDefinition Clone()
    {
        return new SliceCellOverrideDefinition
        {
            Column = Column,
            Row = Row,
            SourceRectPercent = SourceRectPercent?.Clone(),
            DestinationRectPercent = DestinationRectPercent?.Clone()
        };
    }
}

public sealed class SliceCellRectOverride
{
    [JsonPropertyName("xPercent")]
    public double XPercent { get; set; }

    [JsonPropertyName("yPercent")]
    public double YPercent { get; set; }

    [JsonPropertyName("widthPercent")]
    public double WidthPercent { get; set; }

    [JsonPropertyName("heightPercent")]
    public double HeightPercent { get; set; }

    public SliceCellRectOverride Clone()
    {
        return new SliceCellRectOverride
        {
            XPercent = XPercent,
            YPercent = YPercent,
            WidthPercent = WidthPercent,
            HeightPercent = HeightPercent
        };
    }
}