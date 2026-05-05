namespace TwentyFiveSlicer.Desktop.Models;

public sealed record UiAssetManifest(int Version, IReadOnlyList<UiAssetDescriptor> Assets);
