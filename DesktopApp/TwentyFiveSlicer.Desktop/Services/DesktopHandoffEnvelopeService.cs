using System.IO;
using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class DesktopHandoffEnvelopeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    public static DesktopHandoffEnvelope CreateEnvelope(
        TwentyFiveSliceData sliceData,
        SpriteAssetContext? spriteContext,
        UnityRuntimeSettings? runtimeSettings,
        string? targetKind = null,
        string? targetName = null)
    {
        ArgumentNullException.ThrowIfNull(sliceData);

        SpriteAssetContext? envelopeSpriteContext = CreateEnvelopeSpriteContext(spriteContext);
        return new DesktopHandoffEnvelope
        {
            SchemaVersion = DesktopHandoffEnvelope.CurrentSchemaVersion,
            TargetKind = string.IsNullOrWhiteSpace(targetKind) ? DesktopHandoffEnvelope.DefaultTargetKind : targetKind.Trim(),
            TargetName = ResolveTargetName(targetName, envelopeSpriteContext),
            SliceData = sliceData.Clone(),
            SpriteContext = envelopeSpriteContext,
            RuntimeSettings = UnityRuntimeSettings.Normalize(runtimeSettings)
        };
    }

    public static void Export(string envelopePath, DesktopHandoffEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        string? directory = Path.GetDirectoryName(envelopePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(envelopePath, JsonSerializer.Serialize(CreateSerializableEnvelope(envelope), JsonOptions));
    }

    public static UnityImportCompatibilityReport EvaluateUnityImportCompatibility(TwentyFiveSliceData sliceData)
    {
        ArgumentNullException.ThrowIfNull(sliceData);

        var issues = new List<string>();
        if (sliceData.VerticalBorders.Length != 4 || sliceData.HorizontalBorders.Length != 4)
        {
            issues.Add("Unity currently supports importing only fixed 25-slice envelopes with exactly four X guides and four Y guides.");
        }

        if (sliceData.CellOverrides.Length > 0)
        {
            issues.Add("Unity does not support the desktop app's per-cell freeform overrides.");
        }

        AppendSegmentPatternIssue(sliceData.XSegments, "X", issues);
        AppendSegmentPatternIssue(sliceData.YSegments, "Y", issues);
        return new UnityImportCompatibilityReport(issues.Count == 0, issues);
    }

    public static bool LooksLikeEnvelope(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object &&
               document.RootElement.TryGetProperty("sliceData", out _);
    }

    public static DesktopHandoffEnvelopeImportResult Import(string envelopePath)
    {
        string json = File.ReadAllText(envelopePath);
        DesktopHandoffEnvelope? envelope = JsonSerializer.Deserialize<DesktopHandoffEnvelope>(json, JsonOptions);
        if (envelope?.SliceData is null)
        {
            throw new InvalidDataException("The selected file does not contain a valid desktop handoff envelope.");
        }

        SpriteAssetContext? spriteContext = NormalizeSpriteContext(envelope.SpriteContext);
        return new DesktopHandoffEnvelopeImportResult(
            envelope.SliceData,
            spriteContext,
            UnityRuntimeSettings.Normalize(envelope.RuntimeSettings),
            string.IsNullOrWhiteSpace(envelope.TargetKind) ? "spriteAsset" : envelope.TargetKind.Trim(),
            envelope.TargetName?.Trim() ?? string.Empty,
            ResolveImagePath(envelopePath, spriteContext),
            envelopePath);
    }

    private static string ResolveTargetName(string? targetName, SpriteAssetContext? spriteContext)
    {
        if (!string.IsNullOrWhiteSpace(targetName))
        {
            return targetName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(spriteContext?.SpriteName))
        {
            return spriteContext.SpriteName.Trim();
        }

        return "Sprite";
    }

    private static void AppendSegmentPatternIssue(IReadOnlyList<SliceSegmentDefinition> segments, string axisName, List<string> issues)
    {
        if (segments.Count == 0)
        {
            return;
        }

        SliceSegmentMode[] requiredPattern =
        [
            SliceSegmentMode.Fixed,
            SliceSegmentMode.Stretch,
            SliceSegmentMode.Fixed,
            SliceSegmentMode.Stretch,
            SliceSegmentMode.Fixed
        ];

        if (segments.Count != requiredPattern.Length)
        {
            issues.Add($"Unity currently supports importing only fixed 25-slice {axisName} segment layouts with {requiredPattern.Length} entries.");
            return;
        }

        for (int index = 0; index < requiredPattern.Length; index++)
        {
            if (segments[index]?.Mode != requiredPattern[index])
            {
                issues.Add($"Unity currently supports importing only the default 25-slice {axisName} segment pattern: fixed, stretch, fixed, stretch, fixed.");
                return;
            }
        }
    }

    private static object CreateSerializableEnvelope(DesktopHandoffEnvelope envelope)
    {
        return new
        {
            schemaVersion = envelope.SchemaVersion,
            targetKind = envelope.TargetKind,
            targetName = envelope.TargetName,
            sliceData = envelope.SliceData,
            spriteContext = envelope.SpriteContext is null
                ? null
                : new
                {
                    schemaVersion = envelope.SpriteContext.SchemaVersion,
                    spriteName = envelope.SpriteContext.SpriteName,
                    texturePath = envelope.SpriteContext.TexturePath,
                    textureWidth = envelope.SpriteContext.TextureWidth,
                    textureHeight = envelope.SpriteContext.TextureHeight,
                    coordinateOrigin = envelope.SpriteContext.CoordinateOrigin,
                    spriteRect = new
                    {
                        x = envelope.SpriteContext.SpriteRect.X,
                        y = envelope.SpriteContext.SpriteRect.Y,
                        width = envelope.SpriteContext.SpriteRect.Width,
                        height = envelope.SpriteContext.SpriteRect.Height
                    },
                    pivotPixels = new
                    {
                        x = envelope.SpriteContext.PivotPixels.X,
                        y = envelope.SpriteContext.PivotPixels.Y
                    },
                    pixelsPerUnit = envelope.SpriteContext.PixelsPerUnit
                },
            runtimeSettings = envelope.RuntimeSettings
        };
    }

    private static SpriteAssetContext? CreateEnvelopeSpriteContext(SpriteAssetContext? spriteContext)
    {
        SpriteAssetContext? normalized = NormalizeSpriteContext(spriteContext);
        if (normalized is null)
        {
            return null;
        }

        normalized.SidecarPath = string.Empty;
        return normalized;
    }

    private static SpriteAssetContext? NormalizeSpriteContext(SpriteAssetContext? spriteContext)
    {
        if (spriteContext is null)
        {
            return null;
        }

        int textureWidth = spriteContext.TextureWidth > 0
            ? spriteContext.TextureWidth
            : Math.Max(1, spriteContext.SpriteRect.X + spriteContext.SpriteRect.Width);
        int textureHeight = spriteContext.TextureHeight > 0
            ? spriteContext.TextureHeight
            : Math.Max(1, spriteContext.SpriteRect.Y + spriteContext.SpriteRect.Height);

        return SpriteAssetContext.Normalize(spriteContext, textureWidth, textureHeight, spriteContext.TexturePath, spriteContext.SidecarPath);
    }

    private static string? ResolveImagePath(string envelopePath, SpriteAssetContext? spriteContext)
    {
        if (spriteContext is null || string.IsNullOrWhiteSpace(spriteContext.TexturePath))
        {
            return null;
        }

        string texturePath = spriteContext.TexturePath.Trim();
        if (Path.IsPathRooted(texturePath) && File.Exists(texturePath))
        {
            return Path.GetFullPath(texturePath);
        }

        string envelopeDirectory = Path.GetDirectoryName(envelopePath) ?? string.Empty;
        var candidates = new List<string>();

        string fileName = Path.GetFileName(texturePath);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            candidates.Add(Path.Combine(envelopeDirectory, fileName));
        }

        candidates.Add(Path.Combine(envelopeDirectory, texturePath.Replace('/', Path.DirectorySeparatorChar)));

        DirectoryInfo? directory = new DirectoryInfo(envelopeDirectory);
        while (directory is not null)
        {
            candidates.Add(Path.Combine(directory.FullName, texturePath.Replace('/', Path.DirectorySeparatorChar)));
            directory = directory.Parent;
        }

        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }
}