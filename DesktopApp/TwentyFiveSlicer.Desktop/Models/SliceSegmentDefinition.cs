using System.Text.Json.Serialization;

namespace TwentyFiveSlicer.Desktop.Models;

public sealed record SliceSegmentDefinition(
    [property: JsonPropertyName("mode")] SliceSegmentMode Mode = SliceSegmentMode.Fixed);
