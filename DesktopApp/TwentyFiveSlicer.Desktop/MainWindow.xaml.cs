using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Text.Json;
using Microsoft.Win32;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

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
    private TextBox[] _verticalTextBoxes = Array.Empty<TextBox>();
    private TextBox[] _horizontalTextBoxes = Array.Empty<TextBox>();
    private readonly RecentFileList _recentFiles = new();
    private readonly SliceAssistantService _assistant = new();
    private readonly Dictionary<string, TwentyFiveSliceData> _presetLibrary = new()
    {
        ["Preset: Default"] = TwentyFiveSliceData.CreateDefault(),
        ["Preset: Button"] = new TwentyFiveSliceData([12d, 42d, 58d, 88d], [12d, 42d, 58d, 88d]),
        ["Preset: Panel"] = new TwentyFiveSliceData([8d, 34d, 66d, 92d], [8d, 34d, 66d, 92d]),
        ["Preset: Thin Frame"] = new TwentyFiveSliceData([5d, 38d, 62d, 95d], [5d, 38d, 62d, 95d])
    };
    private SliceHistory? _history;

    private BitmapImage? _sourceImage;
    private string? _imagePath;
    private bool _isUpdatingUi;
    private bool _isSyncingTargetSize;

    public MainWindow()
    {
        InitializeComponent();

        _verticalSliders = [Vertical1Slider, Vertical2Slider, Vertical3Slider, Vertical4Slider];
        _horizontalSliders = [Horizontal1Slider, Horizontal2Slider, Horizontal3Slider, Horizontal4Slider];
        _verticalValueTexts = [Vertical1ValueText, Vertical2ValueText, Vertical3ValueText, Vertical4ValueText];
        _horizontalValueTexts = [Horizontal1ValueText, Horizontal2ValueText, Horizontal3ValueText, Horizontal4ValueText];
        _verticalTextBoxes = [Vertical1TextBox, Vertical2TextBox, Vertical3TextBox, Vertical4TextBox];
        _horizontalTextBoxes = [Horizontal1TextBox, Horizontal2TextBox, Horizontal3TextBox, Horizontal4TextBox];
        PresetComboBox.ItemsSource = _presetLibrary.Keys;

        ApplyBorderStateToUi();
        _history = new SliceHistory(CreateEditorState());
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
            LoadSliceDataFromFile(dialog.FileName);
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

        File.WriteAllText(dialog.FileName, CreateSliceJson());
    }

    private void CopyPreset_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(CreateSliceJson());
        PreviewHintText.Text = "Slice JSON copied to the clipboard.";
    }

    private void ExportPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceImage is null)
        {
            MessageBox.Show(this, "Load an image before exporting the preview.", "No image loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            DefaultExt = ".png",
            FileName = _imagePath is null
                ? "twenty-five-slice-preview.png"
                : $"{Path.GetFileNameWithoutExtension(_imagePath)}.preview.png",
            Title = "Export rendered preview"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            BitmapSource bitmap = PreviewControl.RenderOutputBitmap(ExportDebugCheckBox.IsChecked == true);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using FileStream stream = File.Create(dialog.FileName);
            encoder.Save(stream);
            PreviewHintText.Text = $"Exported {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to export preview", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetBorders_Click(object sender, RoutedEventArgs e)
    {
        ApplySliceData(TwentyFiveSliceData.CreateDefault());
        RememberCurrentState();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_history?.CanUndo != true)
        {
            return;
        }

        ApplyEditorState(_history.Undo());
        PreviewHintText.Text = "Undid the last slice edit.";
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (_history?.CanRedo != true)
        {
            return;
        }

        ApplyEditorState(_history.Redo());
        PreviewHintText.Text = "Redid the slice edit.";
    }

    private void SuggestBorders_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceImage is null)
        {
            MessageBox.Show(this, "Load an image before asking for border suggestions.", "No image loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ApplySliceData(ImageBorderSuggestionService.SuggestBorders(_sourceImage));
        RememberCurrentState();
        AssistantResultText.Text = "Suggested borders from transparent image padding.";
    }

    private void ApplyAssistantPrompt_Click(object sender, RoutedEventArgs e)
    {
        SliceAssistantResult result = _assistant.Apply(
            AssistantPromptBox.Text,
            new TwentyFiveSliceData(_verticalBorders, _horizontalBorders));

        if (result.Applied)
        {
            ApplySliceData(result.SliceData);
            RememberCurrentState();
        }

        AssistantResultText.Text = result.Message;
    }

    private void OptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySliceData(new TwentyFiveSliceData([12d, 42d, 58d, 88d], [12d, 42d, 58d, 88d]));
        RememberCurrentState();
        AssistantResultText.Text = "Applied a balanced button preset with protected corners and centered stretch bands.";
    }

    private void BatchCheck_Click(object sender, RoutedEventArgs e)
    {
        UpdateBatchResults();
    }

    private void BatchExport_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceImage is null)
        {
            MessageBox.Show(this, "Load an image before batch export.", "No image loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Choose batch export folder"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        PreviewTarget[] targets = GetCommonPreviewTargets();
        SliceEditorState previous = CreateEditorState();
        foreach (PreviewTarget target in targets)
        {
            SetTargetSize(target.Width, target.Height);
            BitmapSource bitmap = PreviewControl.RenderOutputBitmap(ExportDebugCheckBox.IsChecked == true);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            string baseName = _imagePath is null ? "twenty-five-slice" : Path.GetFileNameWithoutExtension(_imagePath);
            string fileName = $"{baseName}.{target.Width:0}x{target.Height:0}.png";
            using FileStream stream = File.Create(Path.Combine(dialog.FolderName, fileName));
            encoder.Save(stream);
        }

        ApplyEditorState(previous);
        BatchResultsText.Text = $"Exported {targets.Length} previews to {dialog.FolderName}.";
    }

    private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || PresetComboBox.SelectedItem is not string presetName || !_presetLibrary.TryGetValue(presetName, out TwentyFiveSliceData? data))
        {
            return;
        }

        ApplySliceData(data);
        RememberCurrentState();
        AssistantResultText.Text = $"Applied {presetName}.";
    }

    private void PreviewSizePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string preset)
        {
            return;
        }

        if (preset.Equals("Source", StringComparison.OrdinalIgnoreCase))
        {
            if (_sourceImage is null)
            {
                MessageBox.Show(this, "Load an image before using the source-size preset.", "No image loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetTargetSize(_sourceImage.PixelWidth, _sourceImage.PixelHeight);
            return;
        }

        string[] parts = preset.Split('x', StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            double.TryParse(parts[0], out double width) &&
            double.TryParse(parts[1], out double height))
        {
            SetTargetSize(width, height);
        }
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
        RememberCurrentState();
    }

    private void PreviewSettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        SyncLockedAspect(sender);
        UpdatePreview();
        RememberCurrentState();
    }

    private void BorderTextBoxCommitted(object sender, RoutedEventArgs e)
    {
        ApplyBorderTextBoxValue(sender);
    }

    private void BorderTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        ApplyBorderTextBoxValue(sender);
        Keyboard.ClearFocus();
        e.Handled = true;
    }

    private void PreviewControl_GuideEditChanged(object? sender, SliceGuideEditEventArgs e)
    {
        double[] borders = e.IsVertical ? _verticalBorders : _horizontalBorders;
        borders[e.Index] = e.Percent;
        NormalizeBorders(borders);
        ApplyBorderStateToUi();
        UpdatePreview();

        if (e.IsFinal)
        {
            RememberCurrentState();
        }
    }

    private void RecentFilesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || RecentFilesComboBox.SelectedItem is not string filePath)
        {
            return;
        }

        try
        {
            LoadImage(filePath);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to load recent image", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void KeepAspectChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        PreviewHintText.Text = KeepAspectCheckBox.IsChecked == true
            ? "Aspect lock enabled for preview size edits."
            : "Aspect lock disabled.";
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDroppedFile(e) is null ? DragDropEffects.None : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        string? filePath = GetDroppedFile(e);
        if (filePath is null)
        {
            return;
        }

        try
        {
            string extension = Path.GetExtension(filePath);
            if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                LoadSliceDataFromFile(filePath);
            }
            else
            {
                LoadImage(filePath);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to load dropped file", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
        _recentFiles.Add(filePath);
        UpdateRecentFilesUi();

        ConfigureTargetSize(bitmap.PixelWidth, bitmap.PixelHeight);
        _history?.Reset(CreateEditorState());
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

    private void SetTargetSize(double width, double height)
    {
        _isUpdatingUi = true;
        TargetWidthSlider.Maximum = Math.Max(TargetWidthSlider.Maximum, width);
        TargetHeightSlider.Maximum = Math.Max(TargetHeightSlider.Maximum, height);
        TargetWidthSlider.Value = Math.Clamp(width, TargetWidthSlider.Minimum, TargetWidthSlider.Maximum);
        TargetHeightSlider.Value = Math.Clamp(height, TargetHeightSlider.Minimum, TargetHeightSlider.Maximum);
        _isUpdatingUi = false;

        UpdatePreview();
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
            _verticalValueTexts[index].Text = _sourceImage is null
                ? $"{_verticalBorders[index]:0.#}%"
                : $"{_verticalBorders[index]:0.#}% / {(_sourceImage.PixelWidth * _verticalBorders[index] / 100d):0}px";
            _verticalTextBoxes[index].Text = $"{_verticalBorders[index]:0.#}";
        }

        for (int index = 0; index < _horizontalSliders.Length; index++)
        {
            double minimum = index == 0 ? 0d : _horizontalBorders[index - 1];
            double maximum = index == _horizontalBorders.Length - 1 ? 100d : _horizontalBorders[index + 1];
            _horizontalSliders[index].Minimum = minimum;
            _horizontalSliders[index].Maximum = maximum;
            _horizontalSliders[index].Value = _horizontalBorders[index];
            _horizontalValueTexts[index].Text = _sourceImage is null
                ? $"{_horizontalBorders[index]:0.#}%"
                : $"{_horizontalBorders[index]:0.#}% / {(_sourceImage.PixelHeight * _horizontalBorders[index] / 100d):0}px";
            _horizontalTextBoxes[index].Text = $"{_horizontalBorders[index]:0.#}";
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
        PreviewControl.ShowSourceGuides = SourceComparisonCheckBox.IsChecked == true;
        PreviewControl.FlipX = FlipXCheckBox.IsChecked == true;
        PreviewControl.FlipY = FlipYCheckBox.IsChecked == true;

        PreviewSummaryText.Text = _sourceImage is null
            ? "Load an image to render the 25-slice layout."
            : $"{_sourceImage.PixelWidth} x {_sourceImage.PixelHeight} source -> {TargetWidthSlider.Value:0} x {TargetHeightSlider.Value:0} preview";
        UpdateValidation();
    }

    private void LoadSliceDataFromFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        TwentyFiveSliceData? loaded = JsonSerializer.Deserialize<TwentyFiveSliceData>(json, JsonOptions);
        if (loaded is null)
        {
            throw new InvalidDataException("The selected file does not contain slice data.");
        }

        ApplySliceData(new TwentyFiveSliceData(loaded.VerticalBorders, loaded.HorizontalBorders));
        RememberCurrentState();
        PreviewHintText.Text = $"Loaded {Path.GetFileName(filePath)}.";
    }

    private string CreateSliceJson()
    {
        var data = new TwentyFiveSliceData(_verticalBorders, _horizontalBorders);
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    private void SyncLockedAspect(object sender)
    {
        if (_isSyncingTargetSize || KeepAspectCheckBox.IsChecked != true)
        {
            return;
        }

        double ratio = GetAspectRatio();
        if (ratio <= 0d)
        {
            return;
        }

        _isSyncingTargetSize = true;
        try
        {
            if (ReferenceEquals(sender, TargetWidthSlider))
            {
                TargetHeightSlider.Value = Math.Clamp(TargetWidthSlider.Value / ratio, TargetHeightSlider.Minimum, TargetHeightSlider.Maximum);
            }
            else if (ReferenceEquals(sender, TargetHeightSlider))
            {
                TargetWidthSlider.Value = Math.Clamp(TargetHeightSlider.Value * ratio, TargetWidthSlider.Minimum, TargetWidthSlider.Maximum);
            }
        }
        finally
        {
            _isSyncingTargetSize = false;
        }
    }

    private double GetAspectRatio()
    {
        if (_sourceImage is not null && _sourceImage.PixelHeight > 0)
        {
            return (double)_sourceImage.PixelWidth / _sourceImage.PixelHeight;
        }

        return TargetHeightSlider.Value <= 0d ? 0d : TargetWidthSlider.Value / TargetHeightSlider.Value;
    }

    private static string? GetDroppedFile(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        return e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files
            ? files[0]
            : null;
    }

    private void ApplyBorderTextBoxValue(object sender)
    {
        if (_isUpdatingUi || sender is not TextBox textBox || textBox.Tag is not string tag || tag.Length != 2)
        {
            return;
        }

        if (!double.TryParse(textBox.Text, out double value))
        {
            ApplyBorderStateToUi();
            return;
        }

        int index = int.Parse(tag[1].ToString());
        double[] borders = tag[0] == 'V' ? _verticalBorders : _horizontalBorders;
        borders[index] = value;
        NormalizeBorders(borders);
        ApplyBorderStateToUi();
        UpdatePreview();
        RememberCurrentState();
    }

    private SliceEditorState CreateEditorState()
    {
        return new SliceEditorState(
            (double[])_verticalBorders.Clone(),
            (double[])_horizontalBorders.Clone(),
            TargetWidthSlider.Value,
            TargetHeightSlider.Value);
    }

    private void ApplyEditorState(SliceEditorState state)
    {
        Array.Copy(TwentyFiveSliceData.NormalizeAxis(state.VerticalBorders), _verticalBorders, _verticalBorders.Length);
        Array.Copy(TwentyFiveSliceData.NormalizeAxis(state.HorizontalBorders), _horizontalBorders, _horizontalBorders.Length);
        SetTargetSize(state.TargetWidth, state.TargetHeight);
        ApplyBorderStateToUi();
        UpdatePreview();
    }

    private void RememberCurrentState()
    {
        if (_isUpdatingUi)
        {
            return;
        }

        _history?.Push(CreateEditorState());
    }

    private void UpdateRecentFilesUi()
    {
        _isUpdatingUi = true;
        RecentFilesComboBox.ItemsSource = null;
        RecentFilesComboBox.ItemsSource = _recentFiles.Files;
        RecentFilesComboBox.SelectedItem = _imagePath;
        _isUpdatingUi = false;
    }

    private void UpdateValidation()
    {
        if (_sourceImage is null)
        {
            ValidationText.Text = "Load an image to validate this slice.";
            return;
        }

        IReadOnlyList<SliceValidationMessage> messages = SliceValidationService.Validate(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            TargetWidthSlider.Value,
            TargetHeightSlider.Value,
            new TwentyFiveSliceData(_verticalBorders, _horizontalBorders));

        ValidationText.Text = string.Join(Environment.NewLine, messages.Select(FormatValidationMessage));
    }

    private void UpdateBatchResults()
    {
        if (_sourceImage is null)
        {
            BatchResultsText.Text = "Load an image before running batch checks.";
            return;
        }

        PreviewTarget[] targets = GetCommonPreviewTargets();

        IReadOnlyList<BatchPreviewResult> results = BatchPreviewAnalyzer.Analyze(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            new TwentyFiveSliceData(_verticalBorders, _horizontalBorders),
            targets);

        var builder = new StringBuilder();
        foreach (BatchPreviewResult result in results)
        {
            string status = result.HasWarnings ? "WARN" : "PASS";
            builder.Append(status)
                .Append(' ')
                .Append(result.Target.Name)
                .Append(": ");
            builder.AppendLine(string.Join(" ", result.Messages.Select(message => message.Text)));
        }

        BatchResultsText.Text = builder.ToString().Trim();
        AssistantResultText.Text = "Batch check complete.";
    }

    private static string FormatValidationMessage(SliceValidationMessage message)
    {
        return $"{message.Severity}: {message.Text}";
    }

    private static PreviewTarget[] GetCommonPreviewTargets()
    {
        return
        [
            new("128 square", 128d, 128d),
            new("256 wide", 256d, 96d),
            new("512 square", 512d, 512d),
            new("1080p panel", 1920d, 1080d)
        ];
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
