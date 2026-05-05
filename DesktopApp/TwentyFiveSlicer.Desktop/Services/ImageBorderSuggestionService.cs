using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class ImageBorderSuggestionService
{
    public static TwentyFiveSliceData SuggestBorders(BitmapSource image)
    {
        return SuggestBordersDetailed(image).SliceData;
    }

    public static SliceBorderSuggestion SuggestBordersDetailed(BitmapSource image)
    {
        int width = image.PixelWidth;
        int height = image.PixelHeight;
        if (width <= 0 || height <= 0)
        {
            return new SliceBorderSuggestion(TwentyFiveSliceData.CreateDefault(), 0.1d, ["Image has no measurable dimensions."]);
        }

        BitmapSource source = image.Format == PixelFormats.Bgra32
            ? image
            : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0d);

        int stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);

        int left = FirstOpaqueColumn(pixels, width, height, stride);
        int right = width - LastOpaqueColumn(pixels, width, height, stride) - 1;
        int top = FirstOpaqueRow(pixels, width, height, stride);
        int bottom = height - LastOpaqueRow(pixels, width, height, stride) - 1;

        if (left < 0 || top < 0)
        {
            return new SliceBorderSuggestion(TwentyFiveSliceData.CreateDefault(), 0.2d, ["No opaque pixels were found, so default borders were used."]);
        }

        var sliceData = new TwentyFiveSliceData(
            BuildSuggestedAxis(left, right, width),
            BuildSuggestedAxis(top, bottom, height));
        double horizontalPadding = (left + right) / (double)Math.Max(1, width);
        double verticalPadding = (top + bottom) / (double)Math.Max(1, height);
        double confidence = Math.Clamp(0.55d + ((horizontalPadding + verticalPadding) * 0.9d), 0.55d, 0.95d);

        var reasons = new List<string>
        {
            $"Detected transparent padding: left {left}px, right {right}px, top {top}px, bottom {bottom}px."
        };

        if (left == right && top == bottom)
        {
            reasons.Add("Padding is symmetrical, which raises confidence.");
            confidence = Math.Min(0.98d, confidence + 0.08d);
        }

        return new SliceBorderSuggestion(sliceData, confidence, reasons);
    }

    private static double[] BuildSuggestedAxis(int leadingPadding, int trailingPadding, int totalSize)
    {
        double leading = Math.Round(leadingPadding * 100d / totalSize, 1);
        double trailing = Math.Round(100d - (trailingPadding * 100d / totalSize), 1);
        double innerStart = Math.Min(50d, Math.Max(leading + 10d, 40d));
        double innerEnd = Math.Max(50d, Math.Min(trailing - 10d, 60d));

        return TwentyFiveSliceData.NormalizeAxis([leading, innerStart, innerEnd, trailing]);
    }

    private static int FirstOpaqueColumn(byte[] pixels, int width, int height, int stride)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (pixels[(y * stride) + (x * 4) + 3] > 16)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    private static int LastOpaqueColumn(byte[] pixels, int width, int height, int stride)
    {
        for (int x = width - 1; x >= 0; x--)
        {
            for (int y = 0; y < height; y++)
            {
                if (pixels[(y * stride) + (x * 4) + 3] > 16)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    private static int FirstOpaqueRow(byte[] pixels, int width, int height, int stride)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[(y * stride) + (x * 4) + 3] > 16)
                {
                    return y;
                }
            }
        }

        return -1;
    }

    private static int LastOpaqueRow(byte[] pixels, int width, int height, int stride)
    {
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[(y * stride) + (x * 4) + 3] > 16)
                {
                    return y;
                }
            }
        }

        return -1;
    }
}
