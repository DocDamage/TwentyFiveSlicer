using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TwentyFiveSlicer.Desktop.Services;

public enum SpriteRegionSegmentationMode
{
    AlphaThreshold,
    BackgroundColor
}

public sealed record DetectedSpriteRegion(
    Int32Rect Bounds,
    int PixelCount,
    SpriteRegionSegmentationMode SegmentationMode,
    string BackgroundColorHex,
    bool UsedOpaqueFallback);

public static class SpriteRegionDetectionService
{
    public const string AutoDetectedSpriteName = "Auto-detected sprite";
    public const int DefaultMaxCandidateCount = 12;
    public const int DefaultMinimumPixelCount = 4;

    private const byte AlphaThreshold = 16;
    private const double BackgroundColorTolerance = 30d;

    public static DetectedSpriteRegion? DetectPrimarySprite(BitmapSource texture, int minimumPixelCount = DefaultMinimumPixelCount)
    {
        return DetectSpriteCandidates(texture, minimumPixelCount: minimumPixelCount).FirstOrDefault();
    }

    public static IReadOnlyList<DetectedSpriteRegion> DetectSpriteCandidates(
        BitmapSource texture,
        int maxCandidateCount = DefaultMaxCandidateCount,
        int minimumPixelCount = DefaultMinimumPixelCount)
    {
        ArgumentNullException.ThrowIfNull(texture);

        BitmapSource source = EnsureBgra32(texture);
        int width = source.PixelWidth;
        int height = source.PixelHeight;
        if (width <= 0 || height <= 0)
        {
            return [];
        }

        int stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);

        bool hasTransparency = HasTransparency(pixels);
        string backgroundColorHex = "#000000";
        SpriteRegionSegmentationMode segmentationMode = SpriteRegionSegmentationMode.AlphaThreshold;
        Color backgroundColor = Colors.Black;

        Func<int, bool> isForeground = index => pixels[(index * 4) + 3] > AlphaThreshold;
        if (!hasTransparency)
        {
            segmentationMode = SpriteRegionSegmentationMode.BackgroundColor;
            backgroundColor = GetDominantCornerColor(pixels, width, height);
            backgroundColorHex = $"#{backgroundColor.R:X2}{backgroundColor.G:X2}{backgroundColor.B:X2}";
            isForeground = index => GetColorDistance(pixels, index, backgroundColor) > BackgroundColorTolerance;
        }

        int minimumPixels = Math.Max(1, minimumPixelCount);
        var visited = new bool[width * height];
        var candidates = new List<RegionCandidate>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                if (visited[index] || !isForeground(index))
                {
                    continue;
                }

                RegionCandidate candidate = FloodFill(visited, width, height, x, y, isForeground);
                if (candidate.PixelCount < minimumPixels)
                {
                    continue;
                }

                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return [];
        }

        return candidates
            .OrderByDescending(candidate => candidate.PixelCount)
            .ThenByDescending(candidate => candidate.Width * candidate.Height)
            .ThenBy(candidate => GetCenterDistanceSquared(candidate, width / 2d, height / 2d))
            .Take(Math.Max(1, maxCandidateCount))
            .Select(candidate => new DetectedSpriteRegion(
                new Int32Rect(candidate.MinX, candidate.MinY, candidate.MaxX - candidate.MinX + 1, candidate.MaxY - candidate.MinY + 1),
                candidate.PixelCount,
                segmentationMode,
                backgroundColorHex,
                !hasTransparency))
            .ToArray();
    }

    private static bool HasTransparency(byte[] pixels)
    {
        for (int index = 3; index < pixels.Length; index += 4)
        {
            if (pixels[index] < 255)
            {
                return true;
            }
        }

        return false;
    }

    private static Color GetDominantCornerColor(byte[] pixels, int width, int height)
    {
        int[] cornerIndices =
        [
            0,
            Math.Max(0, width - 1),
            Math.Max(0, (height - 1) * width),
            Math.Max(0, (height * width) - 1)
        ];

        return cornerIndices
            .Select(index => ReadColor(pixels, index))
            .GroupBy(color => (color.A, color.R, color.G, color.B))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.R)
            .Select(group => Color.FromArgb(group.Key.A, group.Key.R, group.Key.G, group.Key.B))
            .First();
    }

    private static Color ReadColor(byte[] pixels, int pixelIndex)
    {
        int index = pixelIndex * 4;
        return Color.FromArgb(pixels[index + 3], pixels[index + 2], pixels[index + 1], pixels[index]);
    }

    private static double GetColorDistance(byte[] pixels, int pixelIndex, Color backgroundColor)
    {
        int index = pixelIndex * 4;
        double blue = pixels[index] - backgroundColor.B;
        double green = pixels[index + 1] - backgroundColor.G;
        double red = pixels[index + 2] - backgroundColor.R;
        return Math.Sqrt((red * red) + (green * green) + (blue * blue));
    }

    private static RegionCandidate FloodFill(
        bool[] visited,
        int width,
        int height,
        int startX,
        int startY,
        Func<int, bool> isForeground)
    {
        var stack = new Stack<(int X, int Y)>();
        stack.Push((startX, startY));

        int minX = startX;
        int maxX = startX;
        int minY = startY;
        int maxY = startY;
        int pixelCount = 0;

        while (stack.Count > 0)
        {
            (int x, int y) = stack.Pop();
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                continue;
            }

            int index = (y * width) + x;
            if (visited[index])
            {
                continue;
            }

            visited[index] = true;
            if (!isForeground(index))
            {
                continue;
            }

            pixelCount++;
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);

            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }

        return new RegionCandidate(minX, minY, maxX, maxY, pixelCount);
    }

    private static double GetCenterDistanceSquared(RegionCandidate candidate, double centerX, double centerY)
    {
        double candidateCenterX = candidate.MinX + ((candidate.Width - 1) / 2d);
        double candidateCenterY = candidate.MinY + ((candidate.Height - 1) / 2d);
        double deltaX = candidateCenterX - centerX;
        double deltaY = candidateCenterY - centerY;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    private static BitmapSource EnsureBgra32(BitmapSource sourceImage)
    {
        if (sourceImage.Format == PixelFormats.Bgra32)
        {
            return sourceImage;
        }

        var converted = new FormatConvertedBitmap();
        converted.BeginInit();
        converted.Source = sourceImage;
        converted.DestinationFormat = PixelFormats.Bgra32;
        converted.EndInit();
        converted.Freeze();
        return converted;
    }

    private sealed record RegionCandidate(int MinX, int MinY, int MaxX, int MaxY, int PixelCount)
    {
        public int Width => Math.Max(0, MaxX - MinX + 1);

        public int Height => Math.Max(0, MaxY - MinY + 1);
    }
}