namespace TwentyFiveSlicer.Desktop.Models;

public sealed record UiAssetDescriptor(
    string Id,
    string Path,
    string Source,
    string License,
    string Kind,
    string? FallbackText = null);
