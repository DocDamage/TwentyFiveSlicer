using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

namespace TwentyFiveSlicer.Desktop.Controls;

public sealed class SliceSkinPanel : ContentControl
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(string),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SliceDataSourceProperty = DependencyProperty.Register(
        nameof(SliceDataSource),
        typeof(string),
        typeof(SliceSkinPanel),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string SliceDataSource
    {
        get => (string)GetValue(SliceDataSourceProperty);
        set => SetValue(SliceDataSourceProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        BitmapSource? image = LoadBitmap(Source);
        TwentyFiveSliceData? sliceData = LoadSliceData(SliceDataSource);
        if (image is null || sliceData is null || ActualWidth <= 0d || ActualHeight <= 0d)
        {
            return;
        }

        IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
            image.PixelWidth,
            image.PixelHeight,
            ActualWidth,
            ActualHeight,
            sliceData);

        foreach (SliceRegion region in regions)
        {
            int sourceX = Math.Clamp((int)Math.Round(region.Source.X), 0, image.PixelWidth);
            int sourceY = Math.Clamp((int)Math.Round(region.Source.Y), 0, image.PixelHeight);
            int sourceRight = Math.Clamp((int)Math.Round(region.Source.X + region.Source.Width), 0, image.PixelWidth);
            int sourceBottom = Math.Clamp((int)Math.Round(region.Source.Y + region.Source.Height), 0, image.PixelHeight);
            int sourceWidth = sourceRight - sourceX;
            int sourceHeight = sourceBottom - sourceY;
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                continue;
            }

            var sourceRect = new Int32Rect(sourceX, sourceY, sourceWidth, sourceHeight);
            var cropped = new CroppedBitmap(image, sourceRect);
            drawingContext.DrawImage(cropped, new Rect(
                region.Destination.X,
                region.Destination.Y,
                region.Destination.Width,
                region.Destination.Height));
        }
    }

    private static BitmapSource? LoadBitmap(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "TwentyFiveSlicer.Desktop";
        var resourceUri = new Uri($"/{assemblyName};component/{resourcePath}", UriKind.Relative);
        System.Windows.Resources.StreamResourceInfo? resource = Application.GetResourceStream(resourceUri);

        if (resource is not null)
        {
            using Stream stream = resource.Stream;
            return LoadBitmap(stream);
        }

        string filePath = Path.Combine(AppContext.BaseDirectory, resourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
        {
            return null;
        }

        using FileStream fileStream = File.OpenRead(filePath);
        return LoadBitmap(fileStream);
    }

    private static BitmapSource LoadBitmap(Stream stream)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static TwentyFiveSliceData? LoadSliceData(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "TwentyFiveSlicer.Desktop";
        var resourceUri = new Uri($"/{assemblyName};component/{resourcePath}", UriKind.Relative);
        System.Windows.Resources.StreamResourceInfo? resource = Application.GetResourceStream(resourceUri);

        if (resource is not null)
        {
            using Stream stream = resource.Stream;
            return JsonSerializer.Deserialize<TwentyFiveSliceData>(stream);
        }

        string filePath = Path.Combine(AppContext.BaseDirectory, resourcePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(filePath)
            ? JsonSerializer.Deserialize<TwentyFiveSliceData>(File.ReadAllText(filePath))
            : null;
    }
}
