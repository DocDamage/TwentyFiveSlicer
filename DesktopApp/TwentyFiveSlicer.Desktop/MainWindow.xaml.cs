using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.ComponentModel;
using Microsoft.Win32;
using TwentyFiveSlicer.Desktop.Controls;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

namespace TwentyFiveSlicer.Desktop;

public partial class MainWindow : Window
{
    private static readonly string AppStatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TwentyFiveSlicer",
        "desktop-app-state.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private double[] _verticalBorders = { 20d, 40d, 60d, 80d };
    private double[] _horizontalBorders = { 20d, 40d, 60d, 80d };
    private SliceSegmentDefinition[] _xSegments = TwentyFiveSliceData.NormalizeSegments(null, 5);
    private SliceSegmentDefinition[] _ySegments = TwentyFiveSliceData.NormalizeSegments(null, 5);
    private Slider[] _verticalSliders = Array.Empty<Slider>();
    private Slider[] _horizontalSliders = Array.Empty<Slider>();
    private TextBlock[] _verticalValueTexts = Array.Empty<TextBlock>();
    private TextBlock[] _horizontalValueTexts = Array.Empty<TextBlock>();
    private TextBox[] _verticalTextBoxes = Array.Empty<TextBox>();
    private TextBox[] _horizontalTextBoxes = Array.Empty<TextBox>();
    private readonly RecentFileList _recentFiles = new();
    private readonly SliceAssistantService _assistant = new();
    private readonly SliceChatService _chat = new();
    private readonly List<string> _chatMessages = [];
    private readonly AppStateStore _appStateStore = new(AppStatePath);
    private readonly CloudAiSecretStore _cloudAiSecretStore = CloudAiSecretStore.CreateDefault();
    private readonly IReadOnlyList<CloudAiProviderDescriptor> _cloudAiProviders = CloudAiProviderCatalog.GetAll();
    private readonly CloudAiClient _cloudAiClient;
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
    private SliceAssistantAnalysis? _lastAssistantAnalysis;
    private IReadOnlyList<SliceCandidateSuggestion> _lastCandidateSuggestions = [];
    private SliceEditorState? _candidatePreviewReturnState;
    private SliceEditorState? _chatPreviewReturnState;
    private TwentyFiveSliceData? _pendingChatProposal;
    private SliceSuggestionReview? _pendingChatReview;
    private bool _isUpdatingUi;
    private bool _isSyncingTargetSize;
    private bool _isRestoringSession;
    private bool _isWindowReady;
    private double? _lastPreviewXPercent;
    private double? _lastPreviewYPercent;
    private int _selectedXGuideIndex = -1;
    private int _selectedYGuideIndex = -1;

    public MainWindow()
    {
        _cloudAiClient = new CloudAiClient(_cloudAiSecretStore);
        InitializeComponent();
        SliceSkinPanel.SetCandySkinEnabled(this, CandySkinCheckBox.IsChecked == true);

        _verticalSliders = [Vertical1Slider, Vertical2Slider, Vertical3Slider, Vertical4Slider];
        _horizontalSliders = [Horizontal1Slider, Horizontal2Slider, Horizontal3Slider, Horizontal4Slider];
        _verticalValueTexts = [Vertical1ValueText, Vertical2ValueText, Vertical3ValueText, Vertical4ValueText];
        _horizontalValueTexts = [Horizontal1ValueText, Horizontal2ValueText, Horizontal3ValueText, Horizontal4ValueText];
        _verticalTextBoxes = [Vertical1TextBox, Vertical2TextBox, Vertical3TextBox, Vertical4TextBox];
        _horizontalTextBoxes = [Horizontal1TextBox, Horizontal2TextBox, Horizontal3TextBox, Horizontal4TextBox];
        CloudAiProviderComboBox.ItemsSource = _cloudAiProviders;
        CloudAiProviderComboBox.SelectedValuePath = nameof(CloudAiProviderDescriptor.Id);
        ChatMessagesListBox.ItemsSource = _chatMessages;
        XSegmentModeComboBox.ItemsSource = Enum.GetValues<SliceSegmentMode>();
        YSegmentModeComboBox.ItemsSource = Enum.GetValues<SliceSegmentMode>();
        PreviewControl.PreviewZoomChanged += PreviewControl_PreviewZoomChanged;
        LoadAppState();
        UpdatePresetLibraryUi();

        ApplyBorderStateToUi();
        _history = new SliceHistory(CreateEditorState());
        _isWindowReady = true;
        UpdatePreview();
    }

    private void CandySkinChanged(object sender, RoutedEventArgs e)
    {
        if (!_isWindowReady || CandySkinCheckBox is null)
        {
            return;
        }

        SliceSkinPanel.SetCandySkinEnabled(this, CandySkinCheckBox.IsChecked == true);
        SaveAppState();
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

        SliceBorderSuggestion suggestion = ImageBorderSuggestionService.SuggestBordersDetailed(_sourceImage);
        ApplySliceData(suggestion.SliceData);
        RememberCurrentState();
        AssistantResultText.Text = AssistantReportFormatter.FormatBorderSuggestion(suggestion);
    }

    private void AnalyzeAssistant_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceImage is null)
        {
            AssistantResultText.Text = "Load an image before running slice analysis.";
            return;
        }

        _lastAssistantAnalysis = _assistant.AnalyzeDetailed(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            TargetWidthSlider.Value,
            TargetHeightSlider.Value,
            CreateCurrentSliceData());
        AssistantResultText.Text = AssistantReportFormatter.FormatAnalysis(_lastAssistantAnalysis);
    }

    private void ApplyAssistantPrompt_Click(object sender, RoutedEventArgs e)
    {
        SliceAssistantResult result = _assistant.Apply(
            AssistantPromptBox.Text,
            CreateCurrentSliceData());

        if (result.Applied)
        {
            ApplySliceData(result.SliceData);
            RememberCurrentState();
        }

        AssistantResultText.Text = result.Message;
    }

    private void SendChat_Click(object sender, RoutedEventArgs e)
    {
        _ = SendChatMessageAsync();
    }

    private void PreviewChatProposal_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingChatProposal is null)
        {
            AddChatMessage("Assistant: No chat proposal is waiting to preview.");
            return;
        }

        _chatPreviewReturnState ??= CreateEditorState();
        ApplySliceData(_pendingChatProposal);
        AddChatMessage("Assistant: Previewing the latest chat proposal.");
    }

    private void ApplyChatProposal_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingChatProposal is null)
        {
            AddChatMessage("Assistant: No chat proposal is waiting to apply.");
            return;
        }

        if (_pendingChatReview?.SafeToApply != true)
        {
            AddChatMessage("Assistant: I am not applying that automatically because the deterministic review found risks.");
            return;
        }

        ApplySliceData(_pendingChatProposal);
        RememberCurrentState();
        _pendingChatProposal = null;
        _pendingChatReview = null;
        _chatPreviewReturnState = null;
        AddChatMessage("Assistant: Applied the reviewed chat proposal.");
    }

    private void RejectChatProposal_Click(object sender, RoutedEventArgs e)
    {
        if (_chatPreviewReturnState is not null)
        {
            ApplyEditorState(_chatPreviewReturnState);
        }

        _pendingChatProposal = null;
        _pendingChatReview = null;
        _chatPreviewReturnState = null;
        AddChatMessage("Assistant: Rejected the pending chat proposal.");
    }

    private async void AskCloudAi_Click(object sender, RoutedEventArgs e)
    {
        if (CloudAiEnabledCheckBox.IsChecked != true)
        {
            AssistantResultText.Text = "Cloud AI is disabled. Enable Cloud AI, choose a provider, and save settings first. Local assistant features are still available.";
            return;
        }

        CloudAiProviderDescriptor? provider = GetSelectedCloudAiProvider();
        if (provider is null)
        {
            AssistantResultText.Text = "Choose a cloud AI provider before asking cloud AI.";
            return;
        }

        string prompt = BuildCloudAiPrompt();
        CloudAiSettings settings = CreateCloudAiSettings();
        CloudAiImageInput? image = CreateCloudAiImageInput(provider);
        string providerLabel = CloudAiProviderLabelFormatter.Format(provider.DisplayName);
        AssistantResultText.Text = image is null
            ? $"Asking {providerLabel}..."
            : $"Asking {providerLabel} with image context...";

        CloudAiResult result = await _cloudAiClient.AskAsync(provider, settings, prompt, image);
        if (!result.Success)
        {
            AssistantResultText.Text = $"Cloud AI was unavailable: {result.ErrorMessage}\n\nLocal fallback:\n{BuildLocalFallbackAdvice()}";
            return;
        }

        if (CloudAiAdviceParser.TryParseSliceData(result.Advice, out TwentyFiveSliceData? parsedSliceData) &&
            parsedSliceData is TwentyFiveSliceData suggestedSliceData)
        {
            SliceSuggestionReview review = _sourceImage is null
                ? SliceSuggestionReviewService.Review(
                    CreateCurrentSliceData(),
                    suggestedSliceData,
                    TargetWidthSlider.Value,
                    TargetHeightSlider.Value,
                    TargetWidthSlider.Value,
                    TargetHeightSlider.Value)
                : SliceSuggestionReviewService.Review(
                    CreateCurrentSliceData(),
                    suggestedSliceData,
                    _sourceImage,
                    TargetWidthSlider.Value,
                    TargetHeightSlider.Value);

            if (review.SafeToApply)
            {
                ApplySliceData(suggestedSliceData);
                RememberCurrentState();
                AssistantResultText.Text = $"Cloud AI ({providerLabel}) advice passed deterministic review and was applied.\nScore: {review.Score:P0}\n{FormatSuggestionReview(review)}\n\nAdvice:\n{result.Advice}";
                return;
            }

            AssistantResultText.Text = $"Cloud AI ({providerLabel}) suggested borders, but deterministic review did not auto-apply them.\nScore: {review.Score:P0}\n{FormatSuggestionReview(review)}\n\nAdvice:\n{result.Advice}";
            return;
        }

        AssistantResultText.Text = $"Cloud AI ({providerLabel}) advice:\n{result.Advice}";
    }

    private void OptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySliceData(new TwentyFiveSliceData([12d, 42d, 58d, 88d], [12d, 42d, 58d, 88d]));
        RememberCurrentState();
        AssistantResultText.Text = "Applied a balanced button preset with protected corners and centered stretch bands.";
    }

    private void ApplyTopRecommendation_Click(object sender, RoutedEventArgs e)
    {
        if (_lastAssistantAnalysis is null)
        {
            AnalyzeAssistant_Click(sender, e);
        }

        SliceAssistantAction? action = _lastAssistantAnalysis?.RecommendedActions.FirstOrDefault();
        if (action is null)
        {
            AssistantResultText.Text = "No assistant recommendation is available yet.";
            return;
        }

        SliceAssistantResult result = _assistant.Apply(action.Command, CreateCurrentSliceData());
        if (result.Applied)
        {
            ApplySliceData(result.SliceData);
            RememberCurrentState();
        }

        AssistantResultText.Text = $"{action.Label}: {result.Message}";
    }

    private void FindBestFit_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceImage is null)
        {
            AssistantResultText.Text = "Load an image before finding a best-fit slice.";
            return;
        }

        IReadOnlyList<SliceCandidateSuggestion> candidates = _assistant.RecommendCandidates(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            TargetWidthSlider.Value,
            TargetHeightSlider.Value,
            CreateCurrentSliceData());

        SetCandidateSuggestions(candidates);
        AssistantResultText.Text = AssistantReportFormatter.FormatCandidateList(candidates);
    }

    private void OptimizeAcrossSizes_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceImage is null)
        {
            AssistantResultText.Text = "Load an image before optimizing across sizes.";
            return;
        }

        IReadOnlyList<SliceCandidateSuggestion> candidates = _assistant.RecommendCandidatesForTargets(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            PreviewTargetCatalog.GetCommonTargets(),
            CreateCurrentSliceData());

        SetCandidateSuggestions(candidates);
        AssistantResultText.Text = AssistantReportFormatter.FormatCandidateList(candidates);
        UpdateBatchResults();
    }

    private void PreviewCandidate_Click(object sender, RoutedEventArgs e)
    {
        SliceCandidateSuggestion? candidate = GetSelectedCandidate();
        if (candidate is null)
        {
            AssistantResultText.Text = "Generate and select a candidate before previewing.";
            return;
        }

        _candidatePreviewReturnState ??= CreateEditorState();
        ApplySliceData(candidate.SliceData);
        AssistantResultText.Text = $"Previewing {candidate.Name}. Use Apply Candidate to keep it, or choose another candidate.";
    }

    private void ApplyCandidate_Click(object sender, RoutedEventArgs e)
    {
        SliceCandidateSuggestion? candidate = GetSelectedCandidate();
        if (candidate is null)
        {
            AssistantResultText.Text = "Generate and select a candidate before applying.";
            return;
        }

        ApplySliceData(candidate.SliceData);
        RememberCurrentState();
        _candidatePreviewReturnState = null;
        AssistantResultText.Text = AssistantReportFormatter.FormatCandidateSuggestions(_lastCandidateSuggestions);
    }

    private void SaveUserPreset_Click(object sender, RoutedEventArgs e)
    {
        string presetName = PresetNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(presetName))
        {
            AssistantResultText.Text = "Enter a preset name before saving.";
            return;
        }

        string key = presetName.StartsWith("User:", StringComparison.OrdinalIgnoreCase)
            ? presetName
            : $"User: {presetName}";
        _presetLibrary[key] = CreateCurrentSliceData();
        UpdatePresetLibraryUi(key);
        SaveAppState();
        AssistantResultText.Text = $"Saved {key}.";
    }

    private void SaveCloudAiSettings_Click(object sender, RoutedEventArgs e)
    {
        SaveAppState();
        CloudAiProviderDescriptor? provider = GetSelectedCloudAiProvider();
        AssistantResultText.Text = provider is null
            ? "Cloud AI settings saved."
            : $"Cloud AI settings saved for {CloudAiProviderLabelFormatter.Format(provider.DisplayName)}. Keys use environment variables or the encrypted Windows user vault.";
        UpdateCloudAiKeyStatus();
    }

    private void SaveCloudAiApiKey_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedCloudAiProvider() is not CloudAiProviderDescriptor provider)
        {
            AssistantResultText.Text = "Choose a cloud AI provider before saving a key.";
            return;
        }

        try
        {
            CloudAiSettings settings = CreateCloudAiSettings();
            _cloudAiSecretStore.SaveSecret(provider, settings, CloudAiApiKeyBox.Password);
            CloudAiApiKeyBox.Clear();
            SaveAppState();
            AssistantResultText.Text = $"Secure key saved for {CloudAiProviderLabelFormatter.Format(provider.DisplayName)}.";
            UpdateCloudAiKeyStatus();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or CryptographicException)
        {
            AssistantResultText.Text = $"Secure key was not saved: {exception.Message}";
        }
    }

    private void DeleteCloudAiApiKey_Click(object sender, RoutedEventArgs e)
    {
        if (GetSelectedCloudAiProvider() is not CloudAiProviderDescriptor provider)
        {
            AssistantResultText.Text = "Choose a cloud AI provider before deleting a key.";
            return;
        }

        bool removed = _cloudAiSecretStore.DeleteSecret(provider, CreateCloudAiSettings());
        CloudAiApiKeyBox.Clear();
        AssistantResultText.Text = removed
            ? $"Secure key deleted for {CloudAiProviderLabelFormatter.Format(provider.DisplayName)}."
            : $"No secure key was saved for {CloudAiProviderLabelFormatter.Format(provider.DisplayName)}.";
        UpdateCloudAiKeyStatus();
    }

    private void CloudAiProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || GetSelectedCloudAiProvider() is not CloudAiProviderDescriptor provider)
        {
            return;
        }

        CloudAiModelBox.Text = provider.DefaultModel;
        CloudAiApiKeyEnvBox.Text = provider.ApiKeyEnvironmentVariable;
        CloudAiEndpointBox.Text = provider.Endpoint;
        CloudAiApiKeyBox.Clear();
        UpdateCloudAiKeyStatus();
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

        PreviewTarget[] targets = PreviewTargetCatalog.GetCommonTargets();
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

    private void AddXGuide_Click(object sender, RoutedEventArgs e)
    {
        if (TryAddGuide(ref _verticalBorders, _lastPreviewXPercent))
        {
            _selectedXGuideIndex = FindNearestGuideIndex(_verticalBorders, _lastPreviewXPercent);
            _selectedYGuideIndex = -1;
            ApplyBorderStateToUi();
            UpdatePreview();
            PreviewControl.SelectGuide(isVertical: true, _selectedXGuideIndex);
            RememberCurrentState();
        }
    }

    private void AddYGuide_Click(object sender, RoutedEventArgs e)
    {
        if (TryAddGuide(ref _horizontalBorders, _lastPreviewYPercent))
        {
            _selectedYGuideIndex = FindNearestGuideIndex(_horizontalBorders, _lastPreviewYPercent);
            _selectedXGuideIndex = -1;
            ApplyBorderStateToUi();
            UpdatePreview();
            PreviewControl.SelectGuide(isVertical: false, _selectedYGuideIndex);
            RememberCurrentState();
        }
    }

    private void RemoveXGuide_Click(object sender, RoutedEventArgs e)
    {
        if (TryRemoveGuide(ref _verticalBorders, _selectedXGuideIndex))
        {
            _selectedXGuideIndex = Math.Min(_selectedXGuideIndex, _verticalBorders.Length - 1);
            ApplyBorderStateToUi();
            UpdatePreview();
            PreviewControl.SelectGuide(isVertical: true, _selectedXGuideIndex);
            RememberCurrentState();
        }
    }

    private void RemoveYGuide_Click(object sender, RoutedEventArgs e)
    {
        if (TryRemoveGuide(ref _horizontalBorders, _selectedYGuideIndex))
        {
            _selectedYGuideIndex = Math.Min(_selectedYGuideIndex, _horizontalBorders.Length - 1);
            ApplyBorderStateToUi();
            UpdatePreview();
            PreviewControl.SelectGuide(isVertical: false, _selectedYGuideIndex);
            RememberCurrentState();
        }
    }

    private void SegmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        SyncSelectedSegmentModeControls();
    }

    private void GuideComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || sender is not ComboBox comboBox)
        {
            return;
        }

        if (ReferenceEquals(comboBox, XGuideComboBox) && comboBox.SelectedIndex >= 0)
        {
            SelectGuideFromSidebar(isVertical: true, comboBox.SelectedIndex);
        }
        else if (ReferenceEquals(comboBox, YGuideComboBox) && comboBox.SelectedIndex >= 0)
        {
            SelectGuideFromSidebar(isVertical: false, comboBox.SelectedIndex);
        }
    }

    private void ApplyGuidePercent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string axis)
        {
            ApplyGuidePercent(axis);
        }
    }

    private void NudgeGuide_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
        {
            return;
        }

        string[] parts = tag.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !double.TryParse(parts[1], out double deltaPercent))
        {
            return;
        }

        NudgeGuidePercent(parts[0], deltaPercent);
    }

    private void GuidePercentBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (ReferenceEquals(sender, XGuidePercentBox))
        {
            ApplyGuidePercent("X");
        }
        else if (ReferenceEquals(sender, YGuidePercentBox))
        {
            ApplyGuidePercent("Y");
        }

        Keyboard.ClearFocus();
        e.Handled = true;
    }

    private void SegmentModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || sender is not ComboBox comboBox || comboBox.SelectedItem is not SliceSegmentMode mode)
        {
            return;
        }

        if (ReferenceEquals(comboBox, XSegmentModeComboBox) && XSegmentComboBox.SelectedIndex >= 0 && XSegmentComboBox.SelectedIndex < _xSegments.Length)
        {
            _xSegments[XSegmentComboBox.SelectedIndex] = new SliceSegmentDefinition(mode);
        }
        else if (ReferenceEquals(comboBox, YSegmentModeComboBox) && YSegmentComboBox.SelectedIndex >= 0 && YSegmentComboBox.SelectedIndex < _ySegments.Length)
        {
            _ySegments[YSegmentComboBox.SelectedIndex] = new SliceSegmentDefinition(mode);
        }
        else
        {
            return;
        }

        UpdatePreview();
        RememberCurrentState();
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

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        PreviewControl.AdjustZoomFromMouseWheel(-120);
    }

    private void ResetZoom_Click(object sender, RoutedEventArgs e)
    {
        PreviewControl.ResetPreviewView();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        PreviewControl.AdjustZoomFromMouseWheel(120);
    }

    private void BorderSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isWindowReady || _isUpdatingUi || sender is not Slider slider || slider.Tag is not string tag || tag.Length != 2)
        {
            return;
        }

        int index = int.Parse(tag[1].ToString());
        double[] borders = tag[0] == 'V' ? _verticalBorders : _horizontalBorders;
        if (index >= borders.Length)
        {
            return;
        }

        borders[index] = slider.Value;
        NormalizeBorders(borders);
        ApplyBorderStateToUi();
        UpdatePreview();
        RememberCurrentState();
    }

    private void PreviewSettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_isWindowReady || _isUpdatingUi)
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

    private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _ = SendChatMessageAsync();
            e.Handled = true;
        }
    }

    private void PreviewControl_GuideEditChanged(object? sender, SliceGuideEditEventArgs e)
    {
        double[] borders = e.IsVertical ? _verticalBorders : _horizontalBorders;
        if (e.Index < 0 || e.Index >= borders.Length)
        {
            return;
        }

        borders[e.Index] = e.Percent;
        NormalizeBorders(borders);
        if (e.IsVertical)
        {
            _selectedXGuideIndex = e.Index;
            _selectedYGuideIndex = -1;
        }
        else
        {
            _selectedYGuideIndex = e.Index;
            _selectedXGuideIndex = -1;
        }

        ApplyBorderStateToUi();
        UpdatePreview();

        if (e.IsFinal)
        {
            RememberCurrentState();
        }
    }

    private void PreviewControl_GuideSelectionChanged(object? sender, SliceGuideSelectionEventArgs e)
    {
        if (e.IsVertical)
        {
            _selectedXGuideIndex = e.Index;
            _selectedYGuideIndex = -1;
        }
        else
        {
            _selectedYGuideIndex = e.Index;
            _selectedXGuideIndex = -1;
        }

        UpdateSelectedGuideText();
        RefreshGuideControls();
    }

    private void PreviewControl_GuidePointerChanged(object? sender, SliceGuidePointerEventArgs e)
    {
        _lastPreviewXPercent = e.XPercent;
        _lastPreviewYPercent = e.YPercent;
        UpdateSelectedGuideText();
    }

    private void PreviewControl_PreviewZoomChanged(object? sender, double zoom)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        _isUpdatingUi = true;
        PreviewZoomSlider.Value = Math.Clamp(zoom, PreviewZoomSlider.Minimum, PreviewZoomSlider.Maximum);
        PreviewZoomValueText.Text = $"{PreviewZoomSlider.Value * 100d:0}%";
        _isUpdatingUi = false;
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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.O:
                OpenImage_Click(sender, e);
                e.Handled = true;
                break;
            case Key.S:
                SavePreset_Click(sender, e);
                e.Handled = true;
                break;
            case Key.Z:
                Undo_Click(sender, e);
                e.Handled = true;
                break;
            case Key.Y:
                Redo_Click(sender, e);
                e.Handled = true;
                break;
            case Key.E:
                ExportPreview_Click(sender, e);
                e.Handled = true;
                break;
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        SaveAppState();
        _cloudAiClient.Dispose();
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
        if (!_isRestoringSession)
        {
            SaveAppState();
        }

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
        _verticalBorders = TwentyFiveSliceData.NormalizeAxis(data.VerticalBorders);
        _horizontalBorders = TwentyFiveSliceData.NormalizeAxis(data.HorizontalBorders);
        _xSegments = TwentyFiveSliceData.NormalizeSegments(data.XSegments, _verticalBorders.Length + 1);
        _ySegments = TwentyFiveSliceData.NormalizeSegments(data.YSegments, _horizontalBorders.Length + 1);

        ApplyBorderStateToUi();
        UpdatePreview();
    }

    private void ApplyBorderStateToUi()
    {
        NormalizeBorders(_verticalBorders);
        NormalizeBorders(_horizontalBorders);
        _xSegments = TwentyFiveSliceData.NormalizeSegments(_xSegments, _verticalBorders.Length + 1);
        _ySegments = TwentyFiveSliceData.NormalizeSegments(_ySegments, _horizontalBorders.Length + 1);
        _selectedXGuideIndex = _selectedXGuideIndex >= _verticalBorders.Length ? -1 : _selectedXGuideIndex;
        _selectedYGuideIndex = _selectedYGuideIndex >= _horizontalBorders.Length ? -1 : _selectedYGuideIndex;

        _isUpdatingUi = true;
        GridSizeText.Text = $"{_verticalBorders.Length + 1} x {_horizontalBorders.Length + 1} cells ({_verticalBorders.Length} X guides, {_horizontalBorders.Length} Y guides)";
        UpdateSelectedGuideText();
        RefreshGuideControls();
        RefreshSegmentControls();

        for (int index = 0; index < _verticalSliders.Length; index++)
        {
            if (index >= _verticalBorders.Length)
            {
                _verticalSliders[index].IsEnabled = false;
                _verticalValueTexts[index].Text = "n/a";
                _verticalTextBoxes[index].Text = string.Empty;
                continue;
            }

            _verticalSliders[index].IsEnabled = true;
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
            if (index >= _horizontalBorders.Length)
            {
                _horizontalSliders[index].IsEnabled = false;
                _horizontalValueTexts[index].Text = "n/a";
                _horizontalTextBoxes[index].Text = string.Empty;
                continue;
            }

            _horizontalSliders[index].IsEnabled = true;
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

    private void RefreshSegmentControls()
    {
        int selectedX = Math.Clamp(XSegmentComboBox.SelectedIndex, 0, Math.Max(0, _xSegments.Length - 1));
        int selectedY = Math.Clamp(YSegmentComboBox.SelectedIndex, 0, Math.Max(0, _ySegments.Length - 1));

        XSegmentComboBox.ItemsSource = BuildSegmentLabels("X", _verticalBorders);
        YSegmentComboBox.ItemsSource = BuildSegmentLabels("Y", _horizontalBorders);
        XSegmentComboBox.SelectedIndex = _xSegments.Length == 0 ? -1 : selectedX;
        YSegmentComboBox.SelectedIndex = _ySegments.Length == 0 ? -1 : selectedY;
        SyncSelectedSegmentModeControls();
    }

    private void SyncSelectedSegmentModeControls()
    {
        bool wasUpdating = _isUpdatingUi;
        _isUpdatingUi = true;
        try
        {
            XSegmentModeComboBox.SelectedItem = XSegmentComboBox.SelectedIndex >= 0 && XSegmentComboBox.SelectedIndex < _xSegments.Length
                ? _xSegments[XSegmentComboBox.SelectedIndex].Mode
                : null;
            YSegmentModeComboBox.SelectedItem = YSegmentComboBox.SelectedIndex >= 0 && YSegmentComboBox.SelectedIndex < _ySegments.Length
                ? _ySegments[YSegmentComboBox.SelectedIndex].Mode
                : null;
        }
        finally
        {
            _isUpdatingUi = wasUpdating;
        }
    }

    private void RefreshGuideControls()
    {
        bool wasUpdating = _isUpdatingUi;
        _isUpdatingUi = true;
        try
        {
            int selectedX = _selectedXGuideIndex >= 0 && _selectedXGuideIndex < _verticalBorders.Length ? _selectedXGuideIndex : -1;
            int selectedY = _selectedYGuideIndex >= 0 && _selectedYGuideIndex < _horizontalBorders.Length ? _selectedYGuideIndex : -1;

            XGuideComboBox.ItemsSource = BuildGuideLabels("X", _verticalBorders, _sourceImage?.PixelWidth);
            YGuideComboBox.ItemsSource = BuildGuideLabels("Y", _horizontalBorders, _sourceImage?.PixelHeight);
            XGuideComboBox.SelectedIndex = selectedX;
            YGuideComboBox.SelectedIndex = selectedY;
            XGuidePercentBox.Text = selectedX >= 0 ? $"{_verticalBorders[selectedX]:0.##}" : string.Empty;
            YGuidePercentBox.Text = selectedY >= 0 ? $"{_horizontalBorders[selectedY]:0.##}" : string.Empty;
            XGuidePercentBox.IsEnabled = selectedX >= 0;
            YGuidePercentBox.IsEnabled = selectedY >= 0;
        }
        finally
        {
            _isUpdatingUi = wasUpdating;
        }
    }

    private void SelectGuideFromSidebar(bool isVertical, int index)
    {
        if (isVertical)
        {
            _selectedXGuideIndex = index;
            _selectedYGuideIndex = -1;
        }
        else
        {
            _selectedYGuideIndex = index;
            _selectedXGuideIndex = -1;
        }

        PreviewControl.SelectGuide(isVertical, index);
        UpdateSelectedGuideText();
        RefreshGuideControls();
    }

    private void UpdateSelectedGuideText()
    {
        if (SelectedGuideText is null)
        {
            return;
        }

        string cursorText = _lastPreviewXPercent.HasValue && _lastPreviewYPercent.HasValue
            ? $"Cursor {_lastPreviewXPercent.Value:0.#}% X, {_lastPreviewYPercent.Value:0.#}% Y"
            : "Cursor unavailable";

        if (_selectedXGuideIndex >= 0 && _selectedXGuideIndex < _verticalBorders.Length)
        {
            double percent = _verticalBorders[_selectedXGuideIndex];
            string pixels = _sourceImage is null ? string.Empty : $" / {(_sourceImage.PixelWidth * percent / 100d):0}px";
            SelectedGuideText.Text = $"Selected X guide {_selectedXGuideIndex + 1}: {percent:0.##}%{pixels}. {cursorText}.";
            return;
        }

        if (_selectedYGuideIndex >= 0 && _selectedYGuideIndex < _horizontalBorders.Length)
        {
            double percent = _horizontalBorders[_selectedYGuideIndex];
            string pixels = _sourceImage is null ? string.Empty : $" / {(_sourceImage.PixelHeight * percent / 100d):0}px";
            SelectedGuideText.Text = $"Selected Y guide {_selectedYGuideIndex + 1}: {percent:0.##}%{pixels}. {cursorText}.";
            return;
        }

        SelectedGuideText.Text = $"{cursorText}. No guide selected.";
    }

    private void UpdatePreview()
    {
        TargetWidthValueText.Text = $"{TargetWidthSlider.Value:0} px";
        TargetHeightValueText.Text = $"{TargetHeightSlider.Value:0} px";
        PreviewZoomValueText.Text = $"{PreviewZoomSlider.Value * 100d:0}%";

        PreviewControl.SourceImage = _sourceImage;
        PreviewControl.SliceData = new TwentyFiveSliceData(_verticalBorders, _horizontalBorders, _xSegments, _ySegments);
        PreviewControl.TargetWidth = TargetWidthSlider.Value;
        PreviewControl.TargetHeight = TargetHeightSlider.Value;
        PreviewControl.PreviewZoom = PreviewZoomSlider.Value;
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

        ApplySliceData(loaded);
        RememberCurrentState();
        PreviewHintText.Text = $"Loaded {Path.GetFileName(filePath)}.";
    }

    private string CreateSliceJson()
    {
        var data = new TwentyFiveSliceData(_verticalBorders, _horizontalBorders, _xSegments, _ySegments);
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
        if (index >= borders.Length)
        {
            return;
        }

        borders[index] = value;
        NormalizeBorders(borders);
        ApplyBorderStateToUi();
        UpdatePreview();
        RememberCurrentState();
    }

    private void ApplyGuidePercent(string axis)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        if (TryReadGuideSelection(axis, out bool isVertical, out int selectedIndex, out _, out TextBox textBox) &&
            double.TryParse(textBox.Text, out double value))
        {
            ApplyGuideValue(isVertical, selectedIndex, value);
        }
        else
        {
            ApplyBorderStateToUi();
        }
    }

    private void NudgeGuidePercent(string axis, double deltaPercent)
    {
        if (_isUpdatingUi)
        {
            return;
        }

        if (!TryReadGuideSelection(axis, out bool isVertical, out int selectedIndex, out double[] borders, out _))
        {
            return;
        }

        ApplyGuideValue(isVertical, selectedIndex, borders[selectedIndex] + deltaPercent);
    }

    private bool TryReadGuideSelection(string axis, out bool isVertical, out int selectedIndex, out double[] borders, out TextBox textBox)
    {
        isVertical = axis.Equals("X", StringComparison.OrdinalIgnoreCase);
        selectedIndex = isVertical ? _selectedXGuideIndex : _selectedYGuideIndex;
        double[] selectedBorders = isVertical ? _verticalBorders : _horizontalBorders;
        TextBox selectedTextBox = isVertical ? XGuidePercentBox : YGuidePercentBox;
        if (selectedIndex < 0 || selectedIndex >= selectedBorders.Length)
        {
            borders = selectedBorders;
            textBox = selectedTextBox;
            return false;
        }

        borders = selectedBorders;
        textBox = selectedTextBox;
        return true;
    }

    private void ApplyGuideValue(bool isVertical, int selectedIndex, double value)
    {
        double[] borders = isVertical ? _verticalBorders : _horizontalBorders;
        value = Math.Clamp(value, 0.01d, 99.99d);
        double[] updated = (double[])borders.Clone();
        updated[selectedIndex] = value;
        double[] normalized = TwentyFiveSliceData.NormalizeAxis(updated);
        int newSelectedIndex = FindNearestGuideIndex(normalized, value);

        if (isVertical)
        {
            _verticalBorders = normalized;
            _selectedXGuideIndex = newSelectedIndex;
            _selectedYGuideIndex = -1;
        }
        else
        {
            _horizontalBorders = normalized;
            _selectedYGuideIndex = newSelectedIndex;
            _selectedXGuideIndex = -1;
        }

        ApplyBorderStateToUi();
        UpdatePreview();
        PreviewControl.SelectGuide(isVertical, newSelectedIndex);
        RememberCurrentState();
    }

    private SliceEditorState CreateEditorState()
    {
        return new SliceEditorState(
            (double[])_verticalBorders.Clone(),
            (double[])_horizontalBorders.Clone(),
            TargetWidthSlider.Value,
            TargetHeightSlider.Value,
            _xSegments.ToArray(),
            _ySegments.ToArray());
    }

    private void ApplyEditorState(SliceEditorState state)
    {
        _verticalBorders = TwentyFiveSliceData.NormalizeAxis(state.VerticalBorders);
        _horizontalBorders = TwentyFiveSliceData.NormalizeAxis(state.HorizontalBorders);
        _xSegments = TwentyFiveSliceData.NormalizeSegments(state.XSegments, _verticalBorders.Length + 1);
        _ySegments = TwentyFiveSliceData.NormalizeSegments(state.YSegments, _horizontalBorders.Length + 1);
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

    private void UpdatePresetLibraryUi(string? selectedPreset = null)
    {
        _isUpdatingUi = true;
        PresetComboBox.ItemsSource = null;
        PresetComboBox.ItemsSource = _presetLibrary.Keys.OrderBy(key => key).ToArray();
        PresetComboBox.SelectedItem = selectedPreset;
        _isUpdatingUi = false;
    }

    private void SetCandidateSuggestions(IReadOnlyList<SliceCandidateSuggestion> candidates)
    {
        _lastCandidateSuggestions = candidates;
        _candidatePreviewReturnState = null;
        CandidateComboBox.ItemsSource = null;
        CandidateComboBox.ItemsSource = _lastCandidateSuggestions;
        CandidateComboBox.SelectedIndex = _lastCandidateSuggestions.Count > 0 ? 0 : -1;
    }

    private async Task SendChatMessageAsync()
    {
        string message = ChatInputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        AddChatMessage($"You: {message}");
        if (HandleChatCommand(SliceChatService.ParseCommand(message)))
        {
            ChatInputBox.Text = string.Empty;
            SaveAppState();
            return;
        }

        SliceChatContext context = CreateChatContext();
        SliceChatResponse response = await SendChatToSelectedAssistantAsync(message, context);
        _pendingChatProposal = response.ProposedSliceData;
        _pendingChatReview = response.Review;
        _chatPreviewReturnState = null;
        AddChatMessage($"Assistant: {response.AssistantMessage}");

        if (response.Review is not null)
        {
            AddChatMessage($"Review: {FormatSuggestionReview(response.Review)}");
        }

        AssistantResultText.Text = response.AssistantMessage;
        ChatInputBox.Text = string.Empty;
        SaveAppState();
    }

    private bool HandleChatCommand(SliceChatCommand command)
    {
        switch (command)
        {
            case SliceChatCommand.PreviewProposal:
                PreviewChatProposal_Click(this, new RoutedEventArgs());
                return true;
            case SliceChatCommand.ApplyProposal:
                ApplyChatProposal_Click(this, new RoutedEventArgs());
                return true;
            case SliceChatCommand.RejectProposal:
                RejectChatProposal_Click(this, new RoutedEventArgs());
                return true;
            case SliceChatCommand.ExplainRisk:
                AddChatMessage(_pendingChatReview is null
                    ? "Assistant: There is no pending reviewed proposal to explain."
                    : $"Assistant: {FormatSuggestionReview(_pendingChatReview)}");
                return true;
            case SliceChatCommand.TrySaferProposal:
                TrySaferChatProposal();
                return true;
            default:
                return false;
        }
    }

    private void TrySaferChatProposal()
    {
        if (_sourceImage is null)
        {
            AddChatMessage("Assistant: Load an image first so I can check a safer version against the actual asset.");
            return;
        }

        IReadOnlyList<SliceCandidateSuggestion> candidates = _assistant.RecommendCandidates(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            TargetWidthSlider.Value,
            TargetHeightSlider.Value,
            CreateCurrentSliceData());

        foreach (SliceCandidateSuggestion candidate in candidates)
        {
            SliceSuggestionReview review = SliceSuggestionReviewService.Review(
                CreateCurrentSliceData(),
                candidate.SliceData,
                _sourceImage,
                TargetWidthSlider.Value,
                TargetHeightSlider.Value);
            if (!review.SafeToApply)
            {
                continue;
            }

            _pendingChatProposal = candidate.SliceData;
            _pendingChatReview = review;
            _chatPreviewReturnState = null;
            AddChatMessage($"Assistant: I found a safer proposal: {candidate.Name}. Score: {review.Score:P0}.");
            AddChatMessage($"Review: {FormatSuggestionReview(review)}");
            return;
        }

        AddChatMessage("Assistant: I could not find a safer proposal that passed deterministic review for this target.");
    }

    private async Task<SliceChatResponse> SendChatToSelectedAssistantAsync(string message, SliceChatContext context)
    {
        if (CloudAiEnabledCheckBox.IsChecked != true)
        {
            return _chat.Send(message, context);
        }

        CloudAiProviderDescriptor? provider = GetSelectedCloudAiProvider();
        if (provider is null)
        {
            return new SliceChatResponse("Choose a cloud AI provider before using cloud chat.", null, null);
        }

        AddChatMessage($"Assistant: Asking {CloudAiProviderLabelFormatter.Format(provider.DisplayName)}...");
        CloudAiResult result = await _cloudAiClient.AskAsync(
            provider,
            CreateCloudAiSettings(),
            BuildCloudChatPrompt(message),
            CreateCloudAiImageInput(provider));

        if (!result.Success)
        {
            SliceChatResponse fallback = _chat.Send(message, context);
            return new SliceChatResponse($"Cloud AI was unavailable: {result.ErrorMessage}. Local fallback: {fallback.AssistantMessage}", fallback.ProposedSliceData, fallback.Review);
        }

        return _chat.FromCloudAdvice(result.Advice, context, provider.DisplayName, _sourceImage);
    }

    private SliceChatContext CreateChatContext()
    {
        double sourceWidth = _sourceImage?.PixelWidth ?? TargetWidthSlider.Value;
        double sourceHeight = _sourceImage?.PixelHeight ?? TargetHeightSlider.Value;
        return new SliceChatContext(
            CreateCurrentSliceData(),
            sourceWidth,
            sourceHeight,
            TargetWidthSlider.Value,
            TargetHeightSlider.Value);
    }

    private void AddChatMessage(string message)
    {
        _chatMessages.Add(message);
        while (_chatMessages.Count > 80)
        {
            _chatMessages.RemoveAt(0);
        }

        ChatMessagesListBox.Items.Refresh();
        if (_chatMessages.Count > 0)
        {
            ChatMessagesListBox.ScrollIntoView(_chatMessages[^1]);
        }
    }

    private SliceCandidateSuggestion? GetSelectedCandidate()
    {
        return CandidateComboBox.SelectedItem as SliceCandidateSuggestion ??
            _lastCandidateSuggestions.FirstOrDefault();
    }

    private void LoadAppState()
    {
        DesktopAppState state = _appStateStore.Load();
        _recentFiles.Replace(state.RecentFiles);
        foreach ((string name, TwentyFiveSliceData data) in state.UserPresets)
        {
            _presetLibrary[name.StartsWith("User:", StringComparison.OrdinalIgnoreCase) ? name : $"User: {name}"] = data;
        }

        UpdateRecentFilesUi();
        ApplyCloudAiSettings(state.CloudAi);
        if (state.LastSession is not null)
        {
            ApplySessionState(state.LastSession);
        }
    }

    private void SaveAppState()
    {
        if (!_isWindowReady)
        {
            return;
        }

        var state = new DesktopAppState
        {
            RecentFiles = _recentFiles.Files.ToList(),
            LastSession = CreateSessionState(),
            CloudAi = CreateCloudAiSettings()
        };

        foreach ((string name, TwentyFiveSliceData data) in _presetLibrary.Where(pair => pair.Key.StartsWith("User:", StringComparison.OrdinalIgnoreCase)))
        {
            state.UserPresets[name] = data;
        }

        _appStateStore.Save(state);
    }

    private CloudAiSettings CreateCloudAiSettings()
    {
        CloudAiProviderDescriptor? provider = GetSelectedCloudAiProvider();
        return new CloudAiSettings
        {
            Enabled = CloudAiEnabledCheckBox.IsChecked == true,
            ProviderId = provider?.Id ?? "local",
            ModelId = string.IsNullOrWhiteSpace(CloudAiModelBox.Text) ? provider?.DefaultModel : CloudAiModelBox.Text.Trim(),
            EndpointOverride = string.IsNullOrWhiteSpace(CloudAiEndpointBox.Text) || string.Equals(CloudAiEndpointBox.Text.Trim(), provider?.Endpoint, StringComparison.OrdinalIgnoreCase)
                ? null
                : CloudAiEndpointBox.Text.Trim(),
            ApiKeyEnvironmentVariable = string.IsNullOrWhiteSpace(CloudAiApiKeyEnvBox.Text) ? provider?.ApiKeyEnvironmentVariable : CloudAiApiKeyEnvBox.Text.Trim(),
            UseSecureApiKeyStore = true
        };
    }

    private void ApplyCloudAiSettings(CloudAiSettings settings)
    {
        _isUpdatingUi = true;
        try
        {
            CloudAiProviderDescriptor? provider = CloudAiProviderCatalog.Find(settings.ProviderId) ?? _cloudAiProviders.FirstOrDefault();
            CloudAiEnabledCheckBox.IsChecked = settings.Enabled;
            CloudAiProviderComboBox.SelectedValue = provider?.Id;
            CloudAiModelBox.Text = string.IsNullOrWhiteSpace(settings.ModelId) ? provider?.DefaultModel ?? string.Empty : settings.ModelId;
            CloudAiApiKeyEnvBox.Text = string.IsNullOrWhiteSpace(settings.ApiKeyEnvironmentVariable)
                ? provider?.ApiKeyEnvironmentVariable ?? string.Empty
                : settings.ApiKeyEnvironmentVariable;
            CloudAiEndpointBox.Text = string.IsNullOrWhiteSpace(settings.EndpointOverride)
                ? provider?.Endpoint ?? string.Empty
                : settings.EndpointOverride;
            CloudAiApiKeyBox.Clear();
        }
        finally
        {
            _isUpdatingUi = false;
        }

        UpdateCloudAiKeyStatus();
    }

    private CloudAiProviderDescriptor? GetSelectedCloudAiProvider()
    {
        return CloudAiProviderComboBox.SelectedItem as CloudAiProviderDescriptor ??
            (CloudAiProviderComboBox.SelectedValue is string id ? CloudAiProviderCatalog.Find(id) : null);
    }

    private void UpdateCloudAiKeyStatus()
    {
        if (CloudAiKeyStatusText is null)
        {
            return;
        }

        CloudAiProviderDescriptor? provider = GetSelectedCloudAiProvider();
        CloudAiKeyStatusText.Text = provider is null
            ? "Choose a provider to check key status."
            : _cloudAiSecretStore.DescribeStatus(provider, CreateCloudAiSettings());
    }

    private TwentyFiveSliceData CreateCurrentSliceData()
    {
        return new TwentyFiveSliceData(_verticalBorders, _horizontalBorders, _xSegments, _ySegments);
    }

    private string BuildCloudAiPrompt()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Analyze this variable-grid slice UI asset configuration and recommend accurate guide edits.");
        builder.AppendLine($"Source image: {(_sourceImage is null ? "not loaded" : $"{_sourceImage.PixelWidth} x {_sourceImage.PixelHeight}px")}");
        builder.AppendLine($"Target preview: {TargetWidthSlider.Value:0} x {TargetHeightSlider.Value:0}px");
        builder.AppendLine($"Schema version: {TwentyFiveSliceData.CurrentSchemaVersion}");
        builder.AppendLine($"X guides: {string.Join(", ", _verticalBorders.Select(value => $"{value:0.#}%"))}");
        builder.AppendLine($"Y guides: {string.Join(", ", _horizontalBorders.Select(value => $"{value:0.#}%"))}");
        builder.AppendLine($"X segment modes: {string.Join(", ", _xSegments.Select(segment => segment.Mode.ToString().ToLowerInvariant()))}");
        builder.AppendLine($"Y segment modes: {string.Join(", ", _ySegments.Select(segment => segment.Mode.ToString().ToLowerInvariant()))}");
        builder.AppendLine($"Debug overlay: {DebuggingViewCheckBox.IsChecked == true}; source guides: {SourceComparisonCheckBox.IsChecked == true}; flip X: {FlipXCheckBox.IsChecked == true}; flip Y: {FlipYCheckBox.IsChecked == true}");
        builder.AppendLine("Return concise advice. If edits are recommended, include exact JSON with schemaVersion: 2, xGuidesPercent, yGuidesPercent, xSegments, and ySegments. Segment mode must be fixed, stretch, or hidden. Keep guides ordered, unique, and within 0-100.");

        if (!string.IsNullOrWhiteSpace(AssistantPromptBox.Text))
        {
            builder.AppendLine($"User request: {AssistantPromptBox.Text.Trim()}");
        }

        return builder.ToString();
    }

    private string BuildCloudChatPrompt(string userMessage)
    {
        var builder = new StringBuilder();
        builder.Append(BuildCloudAiPrompt());
        builder.AppendLine();
        builder.AppendLine("This request came from the chat panel.");
        builder.AppendLine("Recent chat:");
        foreach (string chatMessage in _chatMessages.TakeLast(8))
        {
            builder.AppendLine(chatMessage);
        }

        builder.AppendLine($"Latest user chat request: {userMessage.Trim()}");
        return builder.ToString();
    }

    private CloudAiImageInput? CreateCloudAiImageInput(CloudAiProviderDescriptor provider)
    {
        if (_sourceImage is null || !provider.SupportsVision)
        {
            return null;
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_sourceImage));
        using var stream = new MemoryStream();
        encoder.Save(stream);

        return new CloudAiImageInput("image/png", Convert.ToBase64String(stream.ToArray()));
    }

    private string BuildLocalFallbackAdvice()
    {
        if (_sourceImage is null)
        {
            return "Load an image, then use Analyze, Find Best Fit, or Optimize Across Sizes for local recommendations.";
        }

        SliceAssistantAnalysis analysis = _assistant.AnalyzeDetailed(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            TargetWidthSlider.Value,
            TargetHeightSlider.Value,
            CreateCurrentSliceData());
        return AssistantReportFormatter.FormatAnalysis(analysis);
    }

    private static string FormatSuggestionReview(SliceSuggestionReview review)
    {
        var builder = new StringBuilder();
        if (review.Improvements.Count > 0)
        {
            builder.AppendLine("Improvements:");
            foreach (string improvement in review.Improvements)
            {
                builder.AppendLine($"- {improvement}");
            }
        }

        if (review.Risks.Count > 0)
        {
            builder.AppendLine("Risks:");
            foreach (string risk in review.Risks)
            {
                builder.AppendLine($"- {risk}");
            }
        }

        builder.AppendLine($"Vertical guide pixel deltas: {string.Join(", ", review.VerticalPixelDeltas.Select(delta => $"{delta:+0.##;-0.##;0}px"))}");
        builder.AppendLine($"Horizontal guide pixel deltas: {string.Join(", ", review.HorizontalPixelDeltas.Select(delta => $"{delta:+0.##;-0.##;0}px"))}");
        return builder.ToString().Trim();
    }

    private DesktopSessionState CreateSessionState()
    {
        return new DesktopSessionState
        {
            ImagePath = _imagePath,
            SliceData = CreateCurrentSliceData(),
            TargetWidth = TargetWidthSlider.Value,
            TargetHeight = TargetHeightSlider.Value,
            PreviewZoom = PreviewZoomSlider.Value,
            PreviewPanX = PreviewControl.PreviewPanX,
            PreviewPanY = PreviewControl.PreviewPanY,
            KeepAspect = KeepAspectCheckBox.IsChecked == true,
            DebugOverlay = DebuggingViewCheckBox.IsChecked == true,
            ExportDebug = ExportDebugCheckBox.IsChecked == true,
            SourceGuides = SourceComparisonCheckBox.IsChecked == true,
            FlipX = FlipXCheckBox.IsChecked == true,
            FlipY = FlipYCheckBox.IsChecked == true,
            CandySkinEnabled = CandySkinCheckBox.IsChecked == true,
            AssistantOutput = AssistantResultText.Text,
            ChatMessages = _chatMessages.ToList()
        };
    }

    private void ApplySessionState(DesktopSessionState session)
    {
        _isRestoringSession = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(session.ImagePath) && File.Exists(session.ImagePath))
            {
                LoadImage(session.ImagePath);
            }

            ApplySliceData(session.SliceData);

            _isUpdatingUi = true;
            KeepAspectCheckBox.IsChecked = session.KeepAspect;
            PreviewZoomSlider.Value = Math.Clamp(session.PreviewZoom <= 0d ? 1d : session.PreviewZoom, PreviewZoomSlider.Minimum, PreviewZoomSlider.Maximum);
            DebuggingViewCheckBox.IsChecked = session.DebugOverlay;
            ExportDebugCheckBox.IsChecked = session.ExportDebug;
            SourceComparisonCheckBox.IsChecked = session.SourceGuides;
            FlipXCheckBox.IsChecked = session.FlipX;
            FlipYCheckBox.IsChecked = session.FlipY;
            CandySkinCheckBox.IsChecked = session.CandySkinEnabled;
            SliceSkinPanel.SetCandySkinEnabled(this, session.CandySkinEnabled);
            _isUpdatingUi = false;

            SetTargetSize(session.TargetWidth, session.TargetHeight);
            PreviewControl.PreviewPanX = session.PreviewPanX;
            PreviewControl.PreviewPanY = session.PreviewPanY;

            if (!string.IsNullOrWhiteSpace(session.AssistantOutput))
            {
                AssistantResultText.Text = session.AssistantOutput;
            }

            _chatMessages.Clear();
            _chatMessages.AddRange(session.ChatMessages);
            ChatMessagesListBox.Items.Refresh();
        }
        catch (Exception exception)
        {
            PreviewHintText.Text = $"Unable to restore the last session: {exception.Message}";
        }
        finally
        {
            _isUpdatingUi = false;
            _isRestoringSession = false;
        }
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
            CreateCurrentSliceData());

        ValidationText.Text = string.Join(Environment.NewLine, messages.Select(FormatValidationMessage));
    }

    private void UpdateBatchResults()
    {
        if (_sourceImage is null)
        {
            BatchResultsText.Text = "Load an image before running batch checks.";
            return;
        }

        PreviewTarget[] targets = PreviewTargetCatalog.GetCommonTargets();

        IReadOnlyList<BatchPreviewResult> results = BatchPreviewAnalyzer.Analyze(
            _sourceImage.PixelWidth,
            _sourceImage.PixelHeight,
            CreateCurrentSliceData(),
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

    private static bool TryAddGuide(ref double[] guides, double? preferredPercent = null)
    {
        if (guides.Length >= TwentyFiveSliceData.MaxSegmentsPerAxis - 1)
        {
            return false;
        }

        if (preferredPercent.HasValue)
        {
            double preferred = Math.Clamp(preferredPercent.Value, 0.01d, 99.99d);
            if (guides.All(guide => Math.Abs(guide - preferred) >= 0.5d))
            {
                guides = TwentyFiveSliceData.NormalizeAxis([.. guides, preferred]);
                return true;
            }
        }

        double[] stops = [0d, .. TwentyFiveSliceData.NormalizeAxis(guides), 100d];
        double bestStart = 0d;
        double bestEnd = 100d;
        double bestSize = -1d;
        for (int index = 0; index < stops.Length - 1; index++)
        {
            double size = stops[index + 1] - stops[index];
            if (size > bestSize)
            {
                bestSize = size;
                bestStart = stops[index];
                bestEnd = stops[index + 1];
            }
        }

        guides = TwentyFiveSliceData.NormalizeAxis([.. guides, (bestStart + bestEnd) / 2d]);
        return true;
    }

    private static bool TryRemoveGuide(ref double[] guides, int selectedIndex = -1)
    {
        if (guides.Length == 0)
        {
            return false;
        }

        int removeIndex = selectedIndex >= 0 && selectedIndex < guides.Length ? selectedIndex : guides.Length - 1;
        guides = TwentyFiveSliceData.NormalizeAxis(guides.Where((_, index) => index != removeIndex));
        return true;
    }

    private static int FindNearestGuideIndex(IReadOnlyList<double> guides, double? percent)
    {
        if (!percent.HasValue || guides.Count == 0)
        {
            return guides.Count - 1;
        }

        int bestIndex = 0;
        double bestDistance = double.MaxValue;
        for (int index = 0; index < guides.Count; index++)
        {
            double distance = Math.Abs(guides[index] - percent.Value);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static string[] BuildSegmentLabels(string axis, IReadOnlyList<double> guides)
    {
        double[] stops = [0d, .. TwentyFiveSliceData.NormalizeAxis(guides), 100d];
        var labels = new string[stops.Length - 1];
        for (int index = 0; index < labels.Length; index++)
        {
            labels[index] = $"{axis}{index}: {stops[index]:0.#}% - {stops[index + 1]:0.#}%";
        }

        return labels;
    }

    private static string[] BuildGuideLabels(string axis, IReadOnlyList<double> guides, int? sourcePixels)
    {
        var labels = new string[guides.Count];
        for (int index = 0; index < labels.Length; index++)
        {
            string pixelText = sourcePixels.HasValue ? $" / {(sourcePixels.Value * guides[index] / 100d):0}px" : string.Empty;
            labels[index] = $"{axis}{index + 1}: {guides[index]:0.##}%{pixelText}";
        }

        return labels;
    }
}
