using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed record SpriteSourceLoadResult(BitmapSource SourceImage, SpriteAssetContext? Context);

public static class SpriteAssetContextResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static SpriteSourceLoadResult Load(string imagePath, SpriteAssetContext? preferredContext = null, bool preferProvidedContext = false)
    {
        BitmapSource texture = LoadBitmap(imagePath);
        SpriteAssetContext? context = ResolveContext(imagePath, texture, preferredContext, preferProvidedContext);
        BitmapSource source = CreateSourceBitmap(texture, context);
        return new SpriteSourceLoadResult(source, context);
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

            SpriteAssetContext? parsed = JsonSerializer.Deserialize<SpriteAssetContext>(File.ReadAllText(candidatePath), JsonOptions);
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