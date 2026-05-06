using System.Text.Json.Serialization;

namespace TwentyFiveSlicer.Desktop.Models;

public sealed class DesktopHandoffEnvelope
{
    public const int CurrentSchemaVersion = 1;
    public const string DefaultTargetKind = "spriteAsset";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("targetKind")]
    public string TargetKind { get; set; } = DefaultTargetKind;

    [JsonPropertyName("targetName")]
    public string TargetName { get; set; } = string.Empty;

    [JsonPropertyName("sliceData")]
    public TwentyFiveSliceData? SliceData { get; set; }

    [JsonPropertyName("spriteContext")]
    public SpriteAssetContext? SpriteContext { get; set; }

    [JsonPropertyName("runtimeSettings")]
    public UnityRuntimeSettings? RuntimeSettings { get; set; }
}

public sealed record DesktopHandoffEnvelopeImportResult(
    TwentyFiveSliceData SliceData,
    SpriteAssetContext? SpriteContext,
    UnityRuntimeSettings RuntimeSettings,
    string TargetKind,
    string TargetName,
    string? ResolvedImagePath,
    string EnvelopePath);

public sealed record UnityImportCompatibilityReport(
    bool IsCompatible,
    IReadOnlyList<string> Issues);