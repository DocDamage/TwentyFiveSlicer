using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Text.Json;
using Microsoft.Win32;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly double[] _verticalBorders = { 20d, 40d, 60d, 80d };
    private readonly double[] _horizontalBorders = { 20d, 40d, 60d, 80d };
    private Slider[] _verticalSliders = Array.Empty<Slider>();
    private Slider[] _horizontalSliders = Array.Empty<Slider>();
    private TextBlock[] _verticalValueTexts = Array.Empty<TextBlock>();
    private TextBlock[] _horizontalValueTexts = Array.Empty<TextBlock>();

    private BitmapImage? _sourceImage;
    private string? _imagePath;
    private bool _isUpdatingUi;

    public MainWindow()
    {
        InitializeComponent();

        _verticalSliders = [Vertical1Slider, Vertical2Slider, Vertical3Slider, Vertical4Slider];
        _horizontalSliders = [Horizontal1Slider, Horizontal2Slider, Horizontal3Slider, Horizontal4Slider];
        _verticalValueTexts = [Vertical1ValueText, Vertical2ValueText, Vertical3ValueText, Vertical4ValueText];
        _horizontalValueTexts = [Horizontal1ValueText, Horizontal2ValueText, Horizontal3ValueText, Horizontal4ValueText];

        ApplyBorderStateToUi();
        UpdatePreview();
    }

    private void OpenImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All Files|*.*",
            Title = "Open source image"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            LoadImage(dialog.FileName);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to load image", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadPreset_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Slice JSON|*.json|All Files|*.*",
            Title = "Load slice configuration"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            TwentyFiveSliceData? loaded = JsonSerializer.Deserialize<TwentyFiveSliceData>(json, JsonOptions);
            if (loaded is null)
            {
                throw new InvalidDataException("The selected file does not contain slice data.");
            }

            ApplySliceData(new TwentyFiveSliceData(loaded.VerticalBorders, loaded.HorizontalBorders));
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to load slice data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SavePreset_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Slice JSON|*.json|All Files|*.*",
            DefaultExt = ".json",
            FileName = _imagePath is null
                ? "twenty-five-slice.json"
                : $"{Path.GetFileNameWithoutExtension(_imagePath)}.25slice.json",
            Title = "Save slice configuration"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var data = new TwentyFiveSliceData(_verticalBorders, _horizontalBorders);
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(data, JsonOptions));
    }

    private void ResetBorders_Click(object sender, RoutedEventArgs e)
    {
        ApplySliceData(TwentyFiveSliceData.CreateDefault());
    }

    private void BorderSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi || sender is not Slider slider || slider.Tag is not string tag || tag.Length != 2)
        {
            return;
        }

        int index = int.Parse(tag[1].ToString());
        double[] borders = tag[0] == 'V' ? _verticalBorders : _horizontalBorders;
        borders[index] = slider.Value;
        NormalizeBorders(borders);
        ApplyBorderStateToUi();
        UpdatePreview();
    }

    private void PreviewSettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        UpdatePreview();
    }

    private void LoadImage(string filePath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(filePath);
        bitmap.EndInit();
        bitmap.Freeze();

        _sourceImage = bitmap;
        _imagePath = filePath;

        ImagePathText.Text = filePath;
        ImageSizeText.Text = $"{bitmap.PixelWidth} x {bitmap.PixelHeight} px";

        ConfigureTargetSize(bitmap.PixelWidth, bitmap.PixelHeight);
        UpdatePreview();
    }

    private void ConfigureTargetSize(int sourceWidth, int sourceHeight)
    {
        _isUpdatingUi = true;

        TargetWidthSlider.Maximum = Math.Max(2048d, sourceWidth * 4d);
        TargetHeightSlider.Maximum = Math.Max(2048d, sourceHeight * 4d);
        TargetWidthSlider.Value = Math.Max(TargetWidthSlider.Minimum, sourceWidth);
        TargetHeightSlider.Value = Math.Max(TargetHeightSlider.Minimum, sourceHeight);

        _isUpdatingUi = false;
    }

    private void ApplySliceData(TwentyFiveSliceData data)
    {
        double[] verticalBorders = TwentyFiveSliceData.NormalizeAxis(data.VerticalBorders);
        double[] horizontalBorders = TwentyFiveSliceData.NormalizeAxis(data.HorizontalBorders);

        Array.Copy(verticalBorders, _verticalBorders, _verticalBorders.Length);
        Array.Copy(horizontalBorders, _horizontalBorders, _horizontalBorders.Length);

        ApplyBorderStateToUi();
        UpdatePreview();
    }

    private void ApplyBorderStateToUi()
    {
        NormalizeBorders(_verticalBorders);
        NormalizeBorders(_horizontalBorders);

        _isUpdatingUi = true;

        for (int index = 0; index < _verticalSliders.Length; index++)
        {
            double minimum = index == 0 ? 0d : _verticalBorders[index - 1];
            double maximum = index == _verticalBorders.Length - 1 ? 100d : _verticalBorders[index + 1];
            _verticalSliders[index].Minimum = minimum;
            _verticalSliders[index].Maximum = maximum;
            _verticalSliders[index].Value = _verticalBorders[index];
            _verticalValueTexts[index].Text = $"{_verticalBorders[index]:0.#}%";
        }

        for (int index = 0; index < _horizontalSliders.Length; index++)
        {
            double minimum = index == 0 ? 0d : _horizontalBorders[index - 1];
            double maximum = index == _horizontalBorders.Length - 1 ? 100d : _horizontalBorders[index + 1];
            _horizontalSliders[index].Minimum = minimum;
            _horizontalSliders[index].Maximum = maximum;
            _horizontalSliders[index].Value = _horizontalBorders[index];
            _horizontalValueTexts[index].Text = $"{_horizontalBorders[index]:0.#}%";
        }

        _isUpdatingUi = false;
    }

    private void UpdatePreview()
    {
        TargetWidthValueText.Text = $"{TargetWidthSlider.Value:0} px";
        TargetHeightValueText.Text = $"{TargetHeightSlider.Value:0} px";

        PreviewControl.SourceImage = _sourceImage;
        PreviewControl.SliceData = new TwentyFiveSliceData(_verticalBorders, _horizontalBorders);
        PreviewControl.TargetWidth = TargetWidthSlider.Value;
        PreviewControl.TargetHeight = TargetHeightSlider.Value;
        PreviewControl.DebuggingView = DebuggingViewCheckBox.IsChecked == true;
        PreviewControl.FlipX = FlipXCheckBox.IsChecked == true;
        PreviewControl.FlipY = FlipYCheckBox.IsChecked == true;

        PreviewSummaryText.Text = _sourceImage is null
            ? "Load an image to render the 25-slice layout."
            : $"{_sourceImage.PixelWidth} x {_sourceImage.PixelHeight} source -> {TargetWidthSlider.Value:0} x {TargetHeightSlider.Value:0} preview";
    }

    private static void NormalizeBorders(double[] borders)
    {
        if (borders.Length == 0)
        {
            return;
        }

        borders[0] = Math.Clamp(borders[0], 0d, 100d);
        for (int index = 1; index < borders.Length; index++)
        {
            borders[index] = Math.Clamp(borders[index], borders[index - 1], 100d);
        }

        for (int index = borders.Length - 2; index >= 0; index--)
        {
            borders[index] = Math.Min(borders[index], borders[index + 1]);
        }
    }
}