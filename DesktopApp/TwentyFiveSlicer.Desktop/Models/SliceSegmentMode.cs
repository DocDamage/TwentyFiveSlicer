using System.Text.Json.Serialization;

namespace TwentyFiveSlicer.Desktop.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SliceSegmentMode>))]
public enum SliceSegmentMode
{
    Fixed,
    Stretch,
    Hidden
}
