using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed record SpriteSourceLoadResult(BitmapSource TextureImage, BitmapSource SourceImage, SpriteAssetContext? Context, IReadOnlyList<SpriteAssetContext> AutoDetectedCandidates);

public static class SpriteAssetContextResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static SpriteSourceLoadResult Load(
        string imagePath,
        SpriteAssetContext? preferredContext = null,
        bool preferProvidedContext = false,
        int minimumAutoDetectedRegionPixelCount = SpriteRegionDetectionService.DefaultMinimumPixelCount)
    {
        BitmapSource texture = LoadBitmap(imagePath);
        SpriteAssetContext? context = ResolveContext(imagePath, texture, preferredContext, preferProvidedContext);
        IReadOnlyList<SpriteAssetContext> autoDetectedCandidates = ShouldExposeAutoDetectedCandidates(context)
            ? DetectAutoDetectedContexts(imagePath, texture, minimumAutoDetectedRegionPixelCount)
            : [];

        if (context is null && autoDetectedCandidates.Count > 0)
        {
            context = autoDetectedCandidates[0].Clone();
        }
        else if (context is not null && autoDetectedCandidates.Count > 0)
        {
            SpriteAssetContext? matchedContext = FindMatchingAutoDetectedContext(context, autoDetectedCandidates);
            if (matchedContext is not null)
            {
                context = matchedContext;
            }
            else if (string.Equals(context.SpriteName, SpriteRegionDetectionService.AutoDetectedSpriteName, StringComparison.Ordinal))
            {
                context = autoDetectedCandidates[0].Clone();
            }
        }
        else if (context is not null &&
                 autoDetectedCandidates.Count == 0 &&
                 string.Equals(context.SpriteName, SpriteRegionDetectionService.AutoDetectedSpriteName, StringComparison.Ordinal))
        {
            context = null;
        }

        BitmapSource source = CreateSourceBitmap(texture, context);
        return new SpriteSourceLoadResult(texture, source, context, autoDetectedCandidates);
    }

    private static SpriteAssetContext? ResolveContext(string imagePath, BitmapSource texture, SpriteAssetContext? preferredContext, bool preferProvidedContext)
    {
        if (preferProvidedContext && preferredContext is not null)
        {
            return SpriteAssetContext.Normalize(preferredContext, texture.PixelWidth, texture.PixelHeight, imagePath, preferredContext.SidecarPath);
        }

        foreach (string candidatePath in EnumerateSidecarPaths(imagePath))
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            SpriteAssetContext? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<SpriteAssetContext>(File.ReadAllText(candidatePath), JsonOptions);
            }
            catch (IOException)
            {
                continue;
            }
            catch (JsonException)
            {
                continue;
            }

            if (parsed is null)
            {
                continue;
            }

            return SpriteAssetContext.Normalize(parsed, texture.PixelWidth, texture.PixelHeight, imagePath, candidatePath);
        }

        return preferredContext is null
            ? null
            : SpriteAssetContext.Normalize(preferredContext, texture.PixelWidth, texture.PixelHeight, imagePath, preferredContext.SidecarPath);
    }

    private static IReadOnlyList<SpriteAssetContext> DetectAutoDetectedContexts(string imagePath, BitmapSource texture, int minimumAutoDetectedRegionPixelCount)
    {
        return SpriteRegionDetectionService
            .DetectSpriteCandidates(texture, minimumPixelCount: minimumAutoDetectedRegionPixelCount)
            .Where(region => !IsFullTextureBounds(region.Bounds, texture))
            .Select(region => SpriteAssetContext.Normalize(
                new SpriteAssetContext
                {
                    SpriteName = SpriteRegionDetectionService.AutoDetectedSpriteName,
                    TexturePath = imagePath,
                    CoordinateOrigin = "top-left",
                    SpriteRect = new SpritePixelRect
                    {
                        X = region.Bounds.X,
                        Y = region.Bounds.Y,
                        Width = region.Bounds.Width,
                        Height = region.Bounds.Height
                    },
                    PivotPixels = new SpritePixelPoint
                    {
                        X = Math.Round(region.Bounds.Width / 2d, 3),
                        Y = Math.Round(region.Bounds.Height / 2d, 3)
                    },
                    PixelsPerUnit = 100d
                },
                texture.PixelWidth,
                texture.PixelHeight,
                imagePath,
                sidecarPath: string.Empty))
            .ToArray();
    }

    private static bool ShouldExposeAutoDetectedCandidates(SpriteAssetContext? context)
    {
        return context is null || string.Equals(context.SpriteName, SpriteRegionDetectionService.AutoDetectedSpriteName, StringComparison.Ordinal);
    }

    private static SpriteAssetContext? FindMatchingAutoDetectedContext(SpriteAssetContext currentContext, IReadOnlyList<SpriteAssetContext> candidates)
    {
        foreach (SpriteAssetContext candidate in candidates)
        {
            if (candidate.SpriteRect.X == currentContext.SpriteRect.X &&
                candidate.SpriteRect.Y == currentContext.SpriteRect.Y &&
                candidate.SpriteRect.Width == currentContext.SpriteRect.Width &&
                candidate.SpriteRect.Height == currentContext.SpriteRect.Height)
            {
                return candidate.Clone();
            }
        }

        return null;
    }

    private static bool IsFullTextureBounds(Int32Rect bounds, BitmapSource texture)
    {
        return bounds.X == 0 &&
               bounds.Y == 0 &&
               bounds.Width == texture.PixelWidth &&
               bounds.Height == texture.PixelHeight;
    }

    private static BitmapSource CreateSourceBitmap(BitmapSource texture, SpriteAssetContext? context)
    {
        if (context is null)
        {
            return texture;
        }

        if (context.SpriteRect.X == 0 &&
            context.SpriteRect.Y == 0 &&
            context.SpriteRect.Width == texture.PixelWidth &&
            context.SpriteRect.Height == texture.PixelHeight)
        {
            return texture;
        }

        var cropped = new CroppedBitmap(texture, new Int32Rect(
            context.SpriteRect.X,
            context.SpriteRect.Y,
            context.SpriteRect.Width,
            context.SpriteRect.Height));
        cropped.Freeze();
        return cropped;
    }

    private static BitmapSource LoadBitmap(string imagePath)
    {
        using FileStream stream = File.OpenRead(imagePath);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static IEnumerable<string> EnumerateSidecarPaths(string imagePath)
    {
        string directory = Path.GetDirectoryName(imagePath) ?? string.Empty;
        string fileName = Path.GetFileName(imagePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);

        string[] candidates =
        [
            Path.Combine(directory, $"{fileName}.sprite.json"),
            Path.Combine(directory, $"{fileNameWithoutExtension}.sprite.json"),
            Path.Combine(directory, $"{fileName}.sprite-context.json"),
            Path.Combine(directory, $"{fileNameWithoutExtension}.sprite-context.json")
        ];

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }
}