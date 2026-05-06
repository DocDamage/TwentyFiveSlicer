using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop;
using TwentyFiveSlicer.Desktop.Controls;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

var tests = new (string Name, Action Test)[]
{
    ("SliceHistory restores undo and redo states", SliceHistoryRestoresUndoAndRedoStates),
    ("Layout calculator matches Unity fixed stretch distribution", LayoutCalculatorMatchesUnityFixedStretchDistribution),
    ("Variable slice plan is saved", VariableSlicePlanIsSaved),
    ("Slice data upgrades legacy JSON to variable grid", SliceDataUpgradesLegacyJsonToVariableGrid),
    ("Slice data round trips variable grid", SliceDataRoundTripsVariableGrid),
    ("Layout calculator supports extra guides", LayoutCalculatorSupportsExtraGuides),
    ("Layout calculator skips hidden variable segments", LayoutCalculatorSkipsHiddenVariableSegments),
    ("Layout calculator enforces variable grid cap", LayoutCalculatorEnforcesVariableGridCap),
    ("Layout calculator scales fixed regions when target is too small", LayoutCalculatorScalesFixedRegionsWhenTargetIsTooSmall),
    ("Layout calculator flips source regions without moving destinations", LayoutCalculatorFlipsSourceRegionsWithoutMovingDestinations),
    ("Preview control renders export bitmap", PreviewControlRendersExportBitmap),
    ("Preview control export bitmap encodes as PNG", PreviewControlExportBitmapEncodesAsPng),
    ("Preview control clamps zoom", PreviewControlClampsZoomValue),
    ("Preview control adjusts zoom from mouse wheel", PreviewControlAdjustsZoomFromMouseWheel),
    ("Preview control resets zoom", PreviewControlResetsZoomValue),
    ("Preview control accepts variable guide selection", PreviewControlAcceptsVariableGuideSelection),
    ("Preview viewport clamps pan to visible overflow", PreviewViewportClampsPanToVisibleOverflow),
    ("Preview control resets zoom and pan", PreviewControlResetsZoomAndPan),
    ("SliceGuideInteraction detects intersection before single guides", SliceGuideInteractionDetectsIntersectionBeforeSingleGuides),
    ("SliceGuideInteraction detects variable guides beyond original four", SliceGuideInteractionDetectsVariableGuidesBeyondOriginalFour),
    ("SliceGuideInteraction converts point to guide percent", SliceGuideInteractionConvertsPointToGuidePercent),
    ("Slice data JSON stays Unity compatible", SliceDataJsonStaysUnityCompatible),
    ("SliceValidation warns when fixed columns exceed target width", SliceValidationWarnsWhenFixedColumnsExceedTargetWidth),
    ("SliceValidation warns for variable grid hazards", SliceValidationWarnsForVariableGridHazards),
    ("RecentFileList keeps newest unique files first", RecentFileListKeepsNewestUniqueFilesFirst),
    ("BatchPreviewAnalyzer returns warnings per target", BatchPreviewAnalyzerReturnsWarningsPerTarget),
    ("ImageBorderSuggestionService detects transparent padding", ImageBorderSuggestionServiceDetectsTransparentPadding),
    ("SliceAssistant applies natural language edits", SliceAssistantAppliesNaturalLanguageEdits),
    ("SliceHistory ignores duplicate pushed states", SliceHistoryIgnoresDuplicatePushedStates),
    ("AppStateStore round trips recent files and presets", AppStateStoreRoundTripsRecentFilesAndPresets),
    ("SliceAssistant analyzes current slice warnings", SliceAssistantAnalyzesCurrentSliceWarnings),
    ("SliceAssistant returns structured recommendations", SliceAssistantReturnsStructuredRecommendations),
    ("ImageBorderSuggestionService explains confidence", ImageBorderSuggestionServiceExplainsConfidence),
    ("SliceAssistant understands broader prompt intents", SliceAssistantUnderstandsBroaderPromptIntents),
    ("SliceAssistant understands conversational complaints", SliceAssistantUnderstandsConversationalComplaints),
    ("SliceAssistant ranks candidate presets", SliceAssistantRanksCandidatePresets),
    ("SliceAssistant candidate scoring reflects warnings", SliceAssistantCandidateScoringReflectsWarnings),
    ("SliceAssistant ranks candidates across target sizes", SliceAssistantRanksCandidatesAcrossTargetSizes),
    ("SliceAssistant aggregate candidates include target coverage", SliceAssistantAggregateCandidatesIncludeTargetCoverage),
    ("AssistantReportFormatter formats candidate summaries", AssistantReportFormatterFormatsCandidateSummaries),
    ("AssistantReportFormatter formats candidate list without applying", AssistantReportFormatterFormatsCandidateListWithoutApplying),
    ("PreviewTargetCatalog exposes common targets", PreviewTargetCatalogExposesCommonTargets),
    ("AppStateStore round trips last session", AppStateStoreRoundTripsLastSession),
    ("CloudAiProviderCatalog includes common providers", CloudAiProviderCatalogIncludesCommonProviders),
    ("CloudAiRequestBuilder builds OpenAI compatible requests", CloudAiRequestBuilderBuildsOpenAiCompatibleRequests),
    ("CloudAiRequestBuilder builds Anthropic requests", CloudAiRequestBuilderBuildsAnthropicRequests),
    ("CloudAiRequestBuilder builds Gemini requests", CloudAiRequestBuilderBuildsGeminiRequests),
    ("CloudAiRequestBuilder includes OpenAI image payload", CloudAiRequestBuilderIncludesOpenAiImagePayload),
    ("CloudAiRequestBuilder includes Anthropic image payload", CloudAiRequestBuilderIncludesAnthropicImagePayload),
    ("CloudAiRequestBuilder includes Gemini image payload", CloudAiRequestBuilderIncludesGeminiImagePayload),
    ("CloudAiRequestBuilder omits image payload for text-only providers", CloudAiRequestBuilderOmitsImagePayloadForTextOnlyProviders),
    ("AppStateStore round trips cloud AI settings", AppStateStoreRoundTripsCloudAiSettings),
    ("CloudAiClient parses OpenAI compatible responses", CloudAiClientParsesOpenAiCompatibleResponses),
    ("CloudAiClient parses Anthropic responses", CloudAiClientParsesAnthropicResponses),
    ("CloudAiClient parses Gemini responses", CloudAiClientParsesGeminiResponses),
    ("CloudAiClient reports HTTP failures", CloudAiClientReportsHttpFailures),
    ("CloudAiClient reports missing API key", CloudAiClientReportsMissingApiKey),
    ("CloudAiSecretStore protects API keys at rest", CloudAiSecretStoreProtectsApiKeysAtRest),
    ("CloudAiRequestBuilder resolves secure stored keys", CloudAiRequestBuilderResolvesSecureStoredKeys),
    ("CloudAiClient uses secure stored keys", CloudAiClientUsesSecureStoredKeys),
    ("CloudAiAdviceParser extracts JSON border suggestions", CloudAiAdviceParserExtractsJsonBorderSuggestions),
    ("CloudAiAdviceParser extracts inline border suggestions", CloudAiAdviceParserExtractsInlineBorderSuggestions),
    ("CloudAiAdviceParser extracts fenced snake case JSON suggestions", CloudAiAdviceParserExtractsFencedSnakeCaseJsonSuggestions),
    ("CloudAiAdviceParser extracts spaced percentage labels", CloudAiAdviceParserExtractsSpacedPercentageLabels),
    ("CloudAiAdviceParser extracts variable grid suggestions", CloudAiAdviceParserExtractsVariableGridSuggestions),
    ("IdeAssistantBridge returns discoverable provider JSON", IdeAssistantBridgeReturnsDiscoverableProviderJson),
    ("IdeAssistantBridge returns local analysis JSON", IdeAssistantBridgeReturnsLocalAnalysisJson),
    ("IdeAssistantBridge applies prompt as JSON", IdeAssistantBridgeAppliesPromptAsJson),
    ("IdeAssistantBridge returns prompt preview JSON", IdeAssistantBridgeReturnsPromptPreviewJson),
    ("IdeAssistantBridge returns structured error JSON", IdeAssistantBridgeReturnsStructuredErrorJson),
    ("IdeAssistantBridge returns suggestion review JSON", IdeAssistantBridgeReturnsSuggestionReviewJson),
    ("IdeAssistantBridge returns image-aware suggestion review JSON", IdeAssistantBridgeReturnsImageAwareSuggestionReviewJson),
    ("SliceSuggestionReview blocks risky AI cuts", SliceSuggestionReviewBlocksRiskyAiCuts),
    ("SliceSuggestionReview rewards warning reductions", SliceSuggestionReviewRewardsWarningReductions),
    ("SliceSuggestionReview rewards image padding alignment", SliceSuggestionReviewRewardsImagePaddingAlignment),
    ("SliceSuggestionReview blocks image padding mismatch", SliceSuggestionReviewBlocksImagePaddingMismatch),
    ("SliceChatService creates reviewed suggestion", SliceChatServiceCreatesReviewedSuggestion),
    ("SliceChatService creates reviewed cloud suggestion", SliceChatServiceCreatesReviewedCloudSuggestion),
    ("SliceChatService handles conversational edit request", SliceChatServiceHandlesConversationalEditRequest),
    ("SliceChatService recognizes proposal commands", SliceChatServiceRecognizesProposalCommands),
    ("SliceChatService answers analysis without proposal", SliceChatServiceAnswersAnalysisWithoutProposal),
    ("Third-party asset notices mention required sources", ThirdPartyAssetNoticesMentionRequiredSources),
    ("UiAssetCatalog loads manifest assets", UiAssetCatalogLoadsManifestAssets),
    ("UiAssetCatalog rejects duplicate ids", UiAssetCatalogRejectsDuplicateIds),
    ("UiAssetCatalog registered files exist", UiAssetCatalogRegisteredFilesExist),
    ("Candy skin stretch assets include slice metadata", CandySkinStretchAssetsIncludeSliceMetadata),
    ("Cloud AI provider labels prefer friendly names", CloudAiProviderLabelsPreferFriendlyNames),
    ("Cloud AI providers have badge icons", CloudAiProvidersHaveBadgeIcons),
    ("Registered SVG assets render", RegisteredSvgAssetsRender),
    ("Golden samples exist and load", GoldenSamplesExistAndLoad),
    ("ImageBorderSuggestion matches golden samples", ImageBorderSuggestionMatchesGoldenSamples),
    ("Local assistant ranks golden button correctly", LocalAssistantRanksGoldenButtonCorrectly),
    ("Image-aware review accepts golden samples", ImageAwareReviewAcceptsGoldenSamples),
    ("Image-aware review rejects mismatched golden cuts", ImageAwareReviewRejectsMismatchedGoldenCuts),
    ("Cloud parser extracts golden sample advice", CloudParserExtractsGoldenSampleAdvice),
    ("App icon assets exist", AppIconAssetsExist),
    ("Slice skin panel renders Candy asset", SliceSkinPanelRendersCandyAsset),
    ("Slice skin panel renders clean fallback", SliceSkinPanelRendersCleanFallback),
    ("Main window uses skinned card surfaces", MainWindowUsesSkinnedCardSurfaces),
    ("Main window exposes Candy skin toggle and button skin", MainWindowExposesCandySkinToggleAndButtonSkin),
    ("Main window exposes variable grid controls", MainWindowExposesVariableGridControls),
    ("Main window constructs without startup event crash", MainWindowConstructsWithoutStartupEventCrash),
    ("Slice concept diagram renders", SliceConceptDiagramRenders)
};

int failures = 0;
foreach ((string name, Action test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL {name}");
        Console.WriteLine(exception);
    }
}

if (failures > 0)
{
    Environment.Exit(1);
}

static void SliceHistoryRestoresUndoAndRedoStates()
{
    var history = new SliceHistory(new SliceEditorState([20d, 40d, 60d, 80d], [20d, 40d, 60d, 80d], 640d, 360d));
    history.Push(new SliceEditorState([10d, 35d, 65d, 90d], [15d, 38d, 62d, 85d], 800d, 400d));

    Assert.True(history.CanUndo, "Undo should be available after pushing a new state.");
    SliceEditorState first = history.Undo();
    Assert.Equal(20d, first.VerticalBorders[0], "Undo should restore the first vertical border.");

    Assert.True(history.CanRedo, "Redo should be available after undo.");
    SliceEditorState second = history.Redo();
    Assert.Equal(10d, second.VerticalBorders[0], "Redo should restore the pushed vertical border.");
}

static void ThirdPartyAssetNoticesMentionRequiredSources()
{
    string noticesPath = Path.Combine(DesktopProjectRoot(), "Assets", "THIRD_PARTY_NOTICES.md");
    string notices = File.ReadAllText(noticesPath);

    Assert.True(notices.Contains("Candy Pixel Art GUI", StringComparison.Ordinal), "Candy Pixel Art GUI notice should be present.");
    Assert.True(notices.Contains("Tabler Icons", StringComparison.Ordinal), "Tabler Icons notice should be present.");
    Assert.True(notices.Contains("Simple Icons", StringComparison.Ordinal), "Simple Icons notice should be present.");
    Assert.True(notices.Contains("Devicon", StringComparison.Ordinal), "Devicon notice should be present.");
}

static void UiAssetCatalogLoadsManifestAssets()
{
    string manifestPath = Path.Combine(DesktopProjectRoot(), "Assets", "manifest.json");
    UiAssetCatalog catalog = UiAssetCatalog.LoadFromFile(manifestPath);

    UiAssetDescriptor saveIcon = catalog.GetRequired("action.saveJson");

    Assert.Equal("Assets/Icons/Tabler/device-floppy.svg", saveIcon.Path, "The Save JSON icon should resolve from the manifest.");
    Assert.Equal("MIT", saveIcon.License, "Tabler icons should be recorded as MIT licensed.");
}

static void UiAssetCatalogRejectsDuplicateIds()
{
    string tempFile = Path.Combine(Path.GetTempPath(), $"twenty-five-slicer-assets-{Guid.NewGuid():N}.json");
    File.WriteAllText(tempFile, """
        {
          "version": 1,
          "assets": [
            { "id": "action.saveJson", "path": "one.svg", "source": "Test", "license": "MIT", "kind": "icon" },
            { "id": "action.saveJson", "path": "two.svg", "source": "Test", "license": "MIT", "kind": "icon" }
          ]
        }
        """);

    try
    {
        Assert.Throws<InvalidDataException>(() => UiAssetCatalog.LoadFromFile(tempFile), "Duplicate ids should be rejected.");
    }
    finally
    {
        File.Delete(tempFile);
    }
}

static void UiAssetCatalogRegisteredFilesExist()
{
    string desktopRoot = DesktopProjectRoot();
    string manifestPath = Path.Combine(desktopRoot, "Assets", "manifest.json");
    UiAssetCatalog catalog = UiAssetCatalog.LoadFromFile(manifestPath);

    foreach (UiAssetDescriptor asset in catalog.Assets)
    {
        string assetPath = Path.Combine(desktopRoot, asset.Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(assetPath) && string.IsNullOrWhiteSpace(asset.FallbackText))
        {
            throw new FileNotFoundException($"Registered UI asset '{asset.Id}' is missing and has no fallback text.", assetPath);
        }
    }
}

static void CandySkinStretchAssetsIncludeSliceMetadata()
{
    string desktopRoot = DesktopProjectRoot();
    string manifestPath = Path.Combine(desktopRoot, "Assets", "manifest.json");
    UiAssetCatalog catalog = UiAssetCatalog.LoadFromFile(manifestPath);

    foreach (UiAssetDescriptor asset in catalog.Assets.Where(asset => asset.Kind.Equals("skin", StringComparison.OrdinalIgnoreCase)))
    {
        string pngPath = Path.Combine(desktopRoot, asset.Path.Replace('/', Path.DirectorySeparatorChar));
        string slicePath = Path.ChangeExtension(pngPath, ".25slice.json");

        Assert.True(File.Exists(pngPath), $"Skin PNG should exist for {asset.Id}.");
        Assert.True(File.Exists(slicePath), $"Stretch skin asset {asset.Id} should have adjacent .25slice.json metadata.");

        string json = File.ReadAllText(slicePath);
        TwentyFiveSliceData? sliceData = JsonSerializer.Deserialize<TwentyFiveSliceData>(json);
        Assert.True(sliceData is not null, $"Slice metadata for {asset.Id} should deserialize.");
        Assert.Equal(4, sliceData!.VerticalBorders.Length, $"Slice metadata for {asset.Id} should include four vertical borders.");
        Assert.Equal(4, sliceData.HorizontalBorders.Length, $"Slice metadata for {asset.Id} should include four horizontal borders.");
    }
}

static void CloudAiProviderLabelsPreferFriendlyNames()
{
    Assert.Equal("ChatGPT", CloudAiProviderLabelFormatter.Format("OpenAI / ChatGPT"), "OpenAI display should prefer the user-facing ChatGPT label.");
    Assert.Equal("Claude", CloudAiProviderLabelFormatter.Format("Anthropic Claude"), "Anthropic display should prefer the user-facing Claude label.");
    Assert.Equal("Gemini", CloudAiProviderLabelFormatter.Format("Google Gemini"), "Google display should prefer the user-facing Gemini label.");
    Assert.Equal("Moonshot Kimi", CloudAiProviderLabelFormatter.Format("Moonshot Kimi"), "Unknown labels should remain unchanged.");
}

static void CloudAiProvidersHaveBadgeIcons()
{
    string desktopRoot = DesktopProjectRoot();
    foreach (CloudAiProviderDescriptor provider in CloudAiProviderCatalog.GetAll())
    {
        string iconPath = CloudAiProviderIconCatalog.GetIconPath(provider.Id);
        string filePath = Path.Combine(desktopRoot, iconPath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(File.Exists(filePath), $"Provider {provider.Id} should resolve to an existing badge icon.");
    }
}

static void RegisteredSvgAssetsRender()
{
    string desktopRoot = DesktopProjectRoot();
    string manifestPath = Path.Combine(desktopRoot, "Assets", "manifest.json");
    UiAssetCatalog catalog = UiAssetCatalog.LoadFromFile(manifestPath);

    foreach (UiAssetDescriptor asset in catalog.Assets.Where(asset => asset.Path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)))
    {
        string filePath = Path.Combine(desktopRoot, asset.Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(filePath))
        {
            Assert.True(!string.IsNullOrWhiteSpace(asset.FallbackText), $"Missing SVG asset {asset.Id} should have fallback text.");
            continue;
        }

        DrawingGroup drawing = SvgIconRenderer.LoadFile(filePath, Brushes.White);
        Assert.True(drawing.Bounds.Width > 0d, $"SVG asset {asset.Id} should render with positive width.");
        Assert.True(drawing.Bounds.Height > 0d, $"SVG asset {asset.Id} should render with positive height.");
    }
}

static void GoldenSamplesExistAndLoad()
{
    foreach (GoldenSample sample in LoadGoldenSamples())
    {
        Assert.True(File.Exists(sample.ImagePath), $"Golden image should exist for {sample.Name}.");
        Assert.True(File.Exists(sample.SlicePath), $"Golden slice JSON should exist for {sample.Name}.");
        Assert.True(sample.Image.PixelWidth > 0, $"Golden image {sample.Name} should load with positive width.");
        Assert.True(sample.Image.PixelHeight > 0, $"Golden image {sample.Name} should load with positive height.");
        Assert.Equal(4, sample.Expected.VerticalBorders.Length, $"Golden slice {sample.Name} should include four vertical borders.");
        Assert.Equal(4, sample.Expected.HorizontalBorders.Length, $"Golden slice {sample.Name} should include four horizontal borders.");
    }
}

static void ImageBorderSuggestionMatchesGoldenSamples()
{
    foreach (GoldenSample sample in LoadGoldenSamples())
    {
        SliceBorderSuggestion suggestion = ImageBorderSuggestionService.SuggestBordersDetailed(sample.Image);

        AssertAxisNear(sample.Expected.VerticalBorders, suggestion.SliceData.VerticalBorders, 0.15d, $"{sample.Name} vertical suggestion should match golden data.");
        AssertAxisNear(sample.Expected.HorizontalBorders, suggestion.SliceData.HorizontalBorders, 0.15d, $"{sample.Name} horizontal suggestion should match golden data.");
        Assert.True(suggestion.Confidence >= 0.65d, $"{sample.Name} should produce a useful confidence score.");
    }
}

static void LocalAssistantRanksGoldenButtonCorrectly()
{
    GoldenSample button = LoadGoldenSamples().Single(sample => sample.Name == "button");
    var assistant = new SliceAssistantService();

    IReadOnlyList<SliceCandidateSuggestion> candidates = assistant.RecommendCandidates(
        button.Image.PixelWidth,
        button.Image.PixelHeight,
        300d,
        72d,
        TwentyFiveSliceData.CreateDefault());

    SliceCandidateSuggestion top = candidates[0];
    Assert.Equal("Button fit", top.Name, "Wide golden button should rank the button candidate first.");
    AssertAxisNear([10d, 42d, 58d, 90d], top.SliceData.VerticalBorders, 0.01d, "Button candidate vertical borders should remain stable.");
    AssertAxisNear([12d, 42d, 58d, 88d], top.SliceData.HorizontalBorders, 0.01d, "Button candidate horizontal borders should remain stable.");
}

static void ImageAwareReviewAcceptsGoldenSamples()
{
    foreach (GoldenSample sample in LoadGoldenSamples())
    {
        SliceSuggestionReview review = SliceSuggestionReviewService.Review(
            TwentyFiveSliceData.CreateDefault(),
            sample.Expected,
            sample.Image,
            sample.Image.PixelWidth * 2d,
            sample.Image.PixelHeight * 2d);

        Assert.True(review.SafeToApply, $"{sample.Name} golden slice should pass image-aware review.");
        Assert.True(review.Risks.Count == 0, $"{sample.Name} golden slice should not report risks.");
        Assert.Contains(review.Improvements, improvement => improvement.Contains("opaque content bounds", StringComparison.OrdinalIgnoreCase));
    }
}

static void ImageAwareReviewRejectsMismatchedGoldenCuts()
{
    GoldenSample button = LoadGoldenSamples().Single(sample => sample.Name == "button");
    var mismatched = new TwentyFiveSliceData([2d, 40d, 60d, 98d], [2d, 40d, 60d, 98d]);

    SliceSuggestionReview review = SliceSuggestionReviewService.Review(
        TwentyFiveSliceData.CreateDefault(),
        mismatched,
        button.Image,
        300d,
        72d);

    Assert.True(!review.SafeToApply, "Image-aware review should reject cuts that miss detected opaque bounds.");
    Assert.Contains(review.Risks, risk => risk.Contains("opaque content bounds", StringComparison.OrdinalIgnoreCase));
}

static void CloudParserExtractsGoldenSampleAdvice()
{
    GoldenSample panel = LoadGoldenSamples().Single(sample => sample.Name == "panel");
    string advice = """
        The safest cut preserves the detected panel padding:
        ```json
        {
          "vertical_borders": ["11.7%", "40%", "60%", "88.3%"],
          "horizontal_borders": ["17.5%", "40%", "60%", "82.5%"]
        }
        ```
        """;

    Assert.True(CloudAiAdviceParser.TryParseSliceData(advice, out TwentyFiveSliceData? parsed), "Cloud parser should extract golden fenced JSON advice.");
    AssertAxisNear(panel.Expected.VerticalBorders, parsed!.VerticalBorders, 0.01d, "Parsed vertical borders should match golden panel.");
    AssertAxisNear(panel.Expected.HorizontalBorders, parsed.HorizontalBorders, 0.01d, "Parsed horizontal borders should match golden panel.");
}

static void AppIconAssetsExist()
{
    string desktopRoot = DesktopProjectRoot();
    string pngPath = Path.Combine(desktopRoot, "Assets", "App", "app-icon.png");
    string icoPath = Path.Combine(desktopRoot, "Assets", "App", "app-icon.ico");

    Assert.True(File.Exists(pngPath), "Generated app icon PNG should exist.");
    Assert.True(File.Exists(icoPath), "Generated app icon ICO should exist.");
    Assert.True(new FileInfo(pngPath).Length > 0, "Generated app icon PNG should not be empty.");
    Assert.True(new FileInfo(icoPath).Length > 0, "Generated app icon ICO should not be empty.");
}

static void SliceSkinPanelRendersCandyAsset()
{
    RunOnStaThread(() =>
    {
        var control = new SliceSkinPanel
        {
            Source = "Assets/Skin/Candy/panel-subtle.png",
            SliceDataSource = "Assets/Skin/Candy/panel-subtle.25slice.json",
            Width = 260d,
            Height = 120d
        };

        control.Measure(new Size(260d, 120d));
        control.Arrange(new Rect(0d, 0d, 260d, 120d));
        control.UpdateLayout();

        var bitmap = new RenderTargetBitmap(260, 120, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(control);

        Assert.Equal(260, bitmap.PixelWidth, "Skin panel render should preserve requested width.");
        Assert.Equal(120, bitmap.PixelHeight, "Skin panel render should preserve requested height.");
        Assert.True(BitmapHasVisiblePixels(bitmap), "Skin panel render should include visible pixels.");
    });
}

static void SliceSkinPanelRendersCleanFallback()
{
    RunOnStaThread(() =>
    {
        var control = new SliceSkinPanel
        {
            UseSkin = false,
            FallbackBackground = Brushes.DarkSlateGray,
            FallbackBorderBrush = Brushes.White,
            FallbackBorderThickness = new Thickness(1d),
            FallbackCornerRadius = new CornerRadius(8d),
            Width = 180d,
            Height = 80d
        };

        control.Measure(new Size(180d, 80d));
        control.Arrange(new Rect(0d, 0d, 180d, 80d));
        control.UpdateLayout();

        var bitmap = new RenderTargetBitmap(180, 80, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(control);

        Assert.Equal(180, bitmap.PixelWidth, "Fallback skin panel render should preserve requested width.");
        Assert.Equal(80, bitmap.PixelHeight, "Fallback skin panel render should preserve requested height.");
        Assert.True(BitmapHasVisiblePixels(bitmap), "Fallback skin panel render should include visible pixels.");
    });
}

static void MainWindowUsesSkinnedCardSurfaces()
{
    string xamlPath = Path.Combine(DesktopProjectRoot(), "MainWindow.xaml");
    string xaml = File.ReadAllText(xamlPath);

    int skinnedCardCount = CountOccurrences(xaml, "Style=\"{StaticResource SkinnedCardStyle}\"");
    Assert.True(skinnedCardCount >= 6, "Sidebar cards should use the reusable sliced skin panel style.");
    Assert.True(xaml.Contains("x:Key=\"SkinnedCardStyle\"", StringComparison.Ordinal), "Main window should define the skinned card style.");
    Assert.True(xaml.Contains("<ColumnDefinition Width=\"440\" MinWidth=\"420\" />", StringComparison.Ordinal), "Sidebar should keep enough width for skinned controls.");
    Assert.True(xaml.Contains("<Setter Property=\"MinWidth\" Value=\"132\" />", StringComparison.Ordinal), "Action buttons should have a minimum width that protects labels.");
    Assert.True(CountOccurrences(xaml, "<UniformGrid Columns=\"2\"") >= 3, "Dense command groups should use two-column grids instead of clipping in narrow wraps.");
}

static void MainWindowExposesCandySkinToggleAndButtonSkin()
{
    string xamlPath = Path.Combine(DesktopProjectRoot(), "MainWindow.xaml");
    string xaml = File.ReadAllText(xamlPath);

    Assert.True(xaml.Contains("x:Name=\"CandySkinCheckBox\"", StringComparison.Ordinal), "Main window should expose a Candy skin toggle.");
    Assert.True(xaml.Contains("Checked=\"CandySkinChanged\"", StringComparison.Ordinal), "Candy skin toggle should update theme state when enabled.");
    Assert.True(xaml.Contains("Unchecked=\"CandySkinChanged\"", StringComparison.Ordinal), "Candy skin toggle should update theme state when disabled.");
    Assert.True(xaml.Contains("button-normal.png", StringComparison.Ordinal), "Button template should use the Candy normal button asset.");
    Assert.True(xaml.Contains("button-hover.png", StringComparison.Ordinal), "Button template should use the Candy hover button asset.");
    Assert.True(xaml.Contains("button-pressed.png", StringComparison.Ordinal), "Button template should use the Candy pressed button asset.");
    Assert.True(xaml.Contains("FallbackBackground=\"{TemplateBinding Background}\"", StringComparison.Ordinal), "Button template should retain clean fallback styling.");
}

static void MainWindowExposesVariableGridControls()
{
    string xamlPath = Path.Combine(DesktopProjectRoot(), "MainWindow.xaml");
    string xaml = File.ReadAllText(xamlPath);

    Assert.True(xaml.Contains("Text=\"Variable Grid\"", StringComparison.Ordinal), "Main window should expose variable-grid controls.");
    Assert.True(xaml.Contains("x:Name=\"GridSizeText\"", StringComparison.Ordinal), "Main window should show current variable grid size.");
    Assert.True(xaml.Contains("Click=\"AddXGuide_Click\"", StringComparison.Ordinal), "Main window should let users add X guides.");
    Assert.True(xaml.Contains("Click=\"AddYGuide_Click\"", StringComparison.Ordinal), "Main window should let users add Y guides.");
    Assert.True(xaml.Contains("Click=\"RemoveXGuide_Click\"", StringComparison.Ordinal), "Main window should let users remove X guides.");
    Assert.True(xaml.Contains("Click=\"RemoveYGuide_Click\"", StringComparison.Ordinal), "Main window should let users remove Y guides.");
    Assert.True(xaml.Contains("x:Name=\"SelectedGuideText\"", StringComparison.Ordinal), "Main window should show selected guide position and cursor state.");
    Assert.True(xaml.Contains("GuideSelectionChanged=\"PreviewControl_GuideSelectionChanged\"", StringComparison.Ordinal), "Preview should report guide selection to the variable-grid panel.");
    Assert.True(xaml.Contains("GuidePointerChanged=\"PreviewControl_GuidePointerChanged\"", StringComparison.Ordinal), "Preview should report cursor percentages for add-at-cursor guide creation.");
    Assert.True(xaml.Contains("x:Name=\"XGuideComboBox\"", StringComparison.Ordinal), "Main window should expose a dynamic X guide list.");
    Assert.True(xaml.Contains("x:Name=\"YGuideComboBox\"", StringComparison.Ordinal), "Main window should expose a dynamic Y guide list.");
    Assert.True(xaml.Contains("x:Name=\"XGuidePercentBox\"", StringComparison.Ordinal), "Main window should let users edit any selected X guide percentage.");
    Assert.True(xaml.Contains("x:Name=\"YGuidePercentBox\"", StringComparison.Ordinal), "Main window should let users edit any selected Y guide percentage.");
    Assert.True(xaml.Contains("Click=\"ApplyGuidePercent_Click\"", StringComparison.Ordinal), "Main window should commit dynamic guide percentage edits.");
    Assert.True(xaml.Contains("x:Key=\"CompactActionButtonStyle\"", StringComparison.Ordinal), "Compact guide edit buttons should avoid clipping in the sidebar.");
    Assert.True(xaml.Contains("Click=\"NudgeGuide_Click\"", StringComparison.Ordinal), "Main window should support precise guide nudging.");
    Assert.True(xaml.Contains("Tag=\"X:-0.1\"", StringComparison.Ordinal), "X guide controls should include fine negative nudging.");
    Assert.True(xaml.Contains("Tag=\"Y:0.1\"", StringComparison.Ordinal), "Y guide controls should include fine positive nudging.");
}

static void MainWindowConstructsWithoutStartupEventCrash()
{
    RunOnStaThread(() =>
    {
        var window = new MainWindow();
        window.Close();
    });
}

static void SliceConceptDiagramRenders()
{
    RunOnStaThread(() =>
    {
        var control = new SliceConceptDiagramControl
        {
            Width = 220d,
            Height = 140d
        };

        control.Measure(new Size(220d, 140d));
        control.Arrange(new Rect(0d, 0d, 220d, 140d));
        control.UpdateLayout();

        var bitmap = new RenderTargetBitmap(220, 140, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(control);

        Assert.Equal(220, bitmap.PixelWidth, "Diagram render should preserve requested width.");
        Assert.Equal(140, bitmap.PixelHeight, "Diagram render should preserve requested height.");
        Assert.True(BitmapHasVisiblePixels(bitmap), "Diagram render should include visible pixels.");
    });
}

static void LayoutCalculatorMatchesUnityFixedStretchDistribution()
{
    var data = new TwentyFiveSliceData([10d, 30d, 70d, 90d], [10d, 30d, 70d, 90d]);

    IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
        sourceWidth: 100d,
        sourceHeight: 100d,
        targetWidth: 160d,
        targetHeight: 120d,
        data);

    Assert.Equal(25, regions.Count, "All 25 regions should render when every source and destination segment has size.");
    SliceRegion first = regions.Single(region => region.Column == 0 && region.Row == 0);
    Assert.Equal(10d, first.Destination.Width, "Left fixed column should keep original source width.");
    Assert.Equal(10d, first.Destination.Height, "Top fixed row should keep original source height.");

    SliceRegion stretchColumn = regions.Single(region => region.Column == 1 && region.Row == 0);
    Assert.Equal(50d, stretchColumn.Destination.Width, "Stretch columns should receive proportional remaining width.");

    SliceRegion center = regions.Single(region => region.Column == 2 && region.Row == 2);
    Assert.Equal(60d, center.Destination.X, "Center fixed column should start after fixed and stretched columns.");
    Assert.Equal(40d, center.Destination.Y, "Center fixed row should start after fixed and stretched rows.");
    Assert.Equal(40d, center.Destination.Width, "Center fixed column should preserve original width.");
    Assert.Equal(40d, center.Destination.Height, "Center fixed row should preserve original height.");

    SliceRegion last = regions.Single(region => region.Column == 4 && region.Row == 4);
    Assert.Equal(150d, last.Destination.X, "Right fixed column should end at target width.");
    Assert.Equal(110d, last.Destination.Y, "Bottom fixed row should end at target height.");
}

static void VariableSlicePlanIsSaved()
{
    string planPath = Path.Combine(RepositoryRoot(), "docs", "variable-slice-grid-plan.md");

    Assert.True(File.Exists(planPath), "Approved variable slice grid plan should be saved before implementation.");
    string plan = File.ReadAllText(planPath);
    Assert.True(plan.Contains("schemaVersion: 2", StringComparison.Ordinal), "Plan should document v2 schema.");
    Assert.True(plan.Contains("100 x 100", StringComparison.Ordinal), "Plan should document the practical grid cap.");
}

static void SliceDataUpgradesLegacyJsonToVariableGrid()
{
    string legacyJson = """
        {
          "verticalBorders": [10, 30, 70, 90],
          "horizontalBorders": [20, 40, 60, 80]
        }
        """;

    TwentyFiveSliceData data = JsonSerializer.Deserialize<TwentyFiveSliceData>(legacyJson)!;

    Assert.Equal(2, data.SchemaVersion, "Legacy slice JSON should upgrade to schema v2 in memory.");
    Assert.SequenceEqual(new[] { 10d, 30d, 70d, 90d }, data.XGuidesPercent);
    Assert.SequenceEqual(new[] { 20d, 40d, 60d, 80d }, data.YGuidesPercent);
    Assert.Equal(5, data.XSegments.Length, "Four X guides should create five X segments.");
    Assert.Equal(5, data.YSegments.Length, "Four Y guides should create five Y segments.");
    Assert.Equal(SliceSegmentMode.Fixed, data.XSegments[0].Mode, "Outer legacy segment should default to fixed.");
    Assert.Equal(SliceSegmentMode.Stretch, data.XSegments[1].Mode, "Second legacy segment should default to stretch.");
}

static void SliceDataRoundTripsVariableGrid()
{
    var data = new TwentyFiveSliceData(
        xGuidesPercent: [10d, 25d, 40d, 60d, 80d, 92d],
        yGuidesPercent: [15d, 45d, 55d, 85d],
        xSegments:
        [
            new SliceSegmentDefinition(SliceSegmentMode.Fixed),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Hidden),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Fixed),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Fixed)
        ],
        ySegments:
        [
            new SliceSegmentDefinition(SliceSegmentMode.Fixed),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Fixed),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Fixed)
        ]);

    string json = JsonSerializer.Serialize(data);
    TwentyFiveSliceData loaded = JsonSerializer.Deserialize<TwentyFiveSliceData>(json)!;

    Assert.Equal(2, loaded.SchemaVersion, "Variable grid should serialize as v2.");
    Assert.SequenceEqual(data.XGuidesPercent, loaded.XGuidesPercent);
    Assert.SequenceEqual(data.YGuidesPercent, loaded.YGuidesPercent);
    Assert.Equal(SliceSegmentMode.Hidden, loaded.XSegments[2].Mode, "Segment modes should round trip.");
    Assert.True(!json.Contains("verticalBorders", StringComparison.Ordinal), "V2 saves should prefer xGuidesPercent over legacy verticalBorders.");
}

static void LayoutCalculatorSupportsExtraGuides()
{
    var data = new TwentyFiveSliceData(
        xGuidesPercent: [10d, 25d, 40d, 60d, 80d, 92d],
        yGuidesPercent: [20d, 40d, 60d, 80d]);

    IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
        100d,
        100d,
        260d,
        100d,
        data);

    Assert.Equal(35, regions.Count, "Six X guides and four Y guides should render seven by five regions.");
    Assert.True(regions.Any(region => region.Column == 6 && region.Row == 4), "Final variable column and row should render.");
}

static void LayoutCalculatorSkipsHiddenVariableSegments()
{
    var data = new TwentyFiveSliceData(
        xGuidesPercent: [20d, 40d, 60d, 80d],
        yGuidesPercent: [20d, 40d, 60d, 80d],
        xSegments:
        [
            new SliceSegmentDefinition(SliceSegmentMode.Fixed),
            new SliceSegmentDefinition(SliceSegmentMode.Hidden),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Fixed)
        ]);

    IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
        100d,
        100d,
        220d,
        100d,
        data);

    Assert.Equal(20, regions.Count, "Hidden X segment should remove one full column of five regions.");
    Assert.True(!regions.Any(region => region.Column == 1), "Hidden column should not produce render regions.");
    Assert.True(regions.Where(region => region.Column > 1).All(region => region.Destination.X >= 20d), "Columns after hidden segment should close the gap.");
}

static void LayoutCalculatorEnforcesVariableGridCap()
{
    double[] tooManyGuides = Enumerable.Range(1, 100).Select(index => index * 0.5d).ToArray();
    var data = new TwentyFiveSliceData(tooManyGuides, [20d, 40d, 60d, 80d]);

    Assert.Throws<InvalidOperationException>(
        () => TwentyFiveSliceLayoutCalculator.CalculateRegions(100d, 100d, 200d, 200d, data),
        "More than 100 columns should be rejected before rendering.");
}

static void LayoutCalculatorScalesFixedRegionsWhenTargetIsTooSmall()
{
    var data = new TwentyFiveSliceData([10d, 30d, 70d, 90d], [10d, 30d, 70d, 90d]);

    IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
        sourceWidth: 100d,
        sourceHeight: 100d,
        targetWidth: 30d,
        targetHeight: 100d,
        data);

    Assert.Equal(15, regions.Count, "Stretch columns should collapse when fixed columns exceed target width.");
    Assert.True(regions.All(region => region.Column is 0 or 2 or 4), "Only fixed columns should remain when width is below fixed total.");
    SliceRegion left = regions.First(region => region.Column == 0);
    SliceRegion middle = regions.First(region => region.Column == 2);
    SliceRegion right = regions.First(region => region.Column == 4);
    Assert.Equal(5d, left.Destination.Width, "Left fixed column should scale down proportionally.");
    Assert.Equal(20d, middle.Destination.Width, "Center fixed column should scale down proportionally.");
    Assert.Equal(5d, right.Destination.Width, "Right fixed column should scale down proportionally.");
}

static void LayoutCalculatorFlipsSourceRegionsWithoutMovingDestinations()
{
    var data = new TwentyFiveSliceData([10d, 30d, 70d, 90d], [10d, 30d, 70d, 90d]);

    IReadOnlyList<SliceRegion> regions = TwentyFiveSliceLayoutCalculator.CalculateRegions(
        sourceWidth: 100d,
        sourceHeight: 100d,
        targetWidth: 160d,
        targetHeight: 120d,
        data,
        flipX: true,
        flipY: true);

    SliceRegion first = regions.Single(region => region.Column == 0 && region.Row == 0);
    Assert.Equal(0d, first.Destination.X, "Flip should not move destination columns.");
    Assert.Equal(0d, first.Destination.Y, "Flip should not move destination rows.");
    Assert.Equal(90d, first.Source.X, "Flip X should sample the opposite source column.");
    Assert.Equal(90d, first.Source.Y, "Flip Y should sample the opposite source row.");
}

static void PreviewControlRendersExportBitmap()
{
    RunOnStaThread(() =>
    {
        var preview = new TwentyFiveSlicePreviewControl
        {
            SourceImage = CreateTransparentPaddingBitmap(16, 16, 0, 0, 0, 0),
            SliceData = new TwentyFiveSliceData([25d, 40d, 60d, 75d], [25d, 40d, 60d, 75d]),
            TargetWidth = 64d,
            TargetHeight = 48d,
            DebuggingView = true,
            FlipX = true,
            FlipY = true
        };

        BitmapSource bitmap = preview.RenderOutputBitmap(includeDebugOverlay: true);

        Assert.Equal(64, bitmap.PixelWidth, "Export bitmap should use target width.");
        Assert.Equal(48, bitmap.PixelHeight, "Export bitmap should use target height.");
        Assert.True(BitmapHasVisiblePixels(bitmap), "Export bitmap should contain rendered pixels.");
    });
}

static void PreviewControlExportBitmapEncodesAsPng()
{
    RunOnStaThread(() =>
    {
        var preview = new TwentyFiveSlicePreviewControl
        {
            SourceImage = CreateTransparentPaddingBitmap(16, 16, 0, 0, 0, 0),
            SliceData = new TwentyFiveSliceData([25d, 40d, 60d, 75d], [25d, 40d, 60d, 75d]),
            TargetWidth = 64d,
            TargetHeight = 48d
        };

        BitmapSource bitmap = preview.RenderOutputBitmap();
        byte[] pngBytes = EncodePng(bitmap);
        BitmapFrame decoded = BitmapFrame.Create(new MemoryStream(pngBytes), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

        Assert.True(IsPng(pngBytes), "Export encoder should produce a PNG file signature.");
        Assert.Equal(64, decoded.PixelWidth, "Encoded PNG should preserve target width.");
        Assert.Equal(48, decoded.PixelHeight, "Encoded PNG should preserve target height.");
    });
}

static void PreviewControlClampsZoomValue()
{
    RunOnStaThread(() =>
    {
        var preview = new TwentyFiveSlicePreviewControl();

        preview.PreviewZoom = 0.1d;
        Assert.Equal(0.5d, preview.PreviewZoom, "Preview zoom should not go below the Unity editor minimum.");

        preview.PreviewZoom = 2.8d;
        Assert.Equal(2d, preview.PreviewZoom, "Preview zoom should not exceed the Unity editor maximum.");

        preview.PreviewZoom = 1.35d;
        Assert.Equal(1.35d, preview.PreviewZoom, "Preview zoom should preserve valid zoom values.");
    });
}

static void PreviewControlAdjustsZoomFromMouseWheel()
{
    RunOnStaThread(() =>
    {
        var preview = new TwentyFiveSlicePreviewControl();
        double lastZoom = 0d;
        preview.PreviewZoomChanged += (_, zoom) => lastZoom = zoom;

        preview.AdjustZoomFromMouseWheel(120);
        Assert.Equal(1.05d, preview.PreviewZoom, "Mouse wheel up should zoom in by one editor-style increment.");
        Assert.Equal(1.05d, lastZoom, "Preview should notify listeners when wheel zoom changes.");

        preview.AdjustZoomFromMouseWheel(-240);
        Assert.Equal(0.95d, preview.PreviewZoom, "Mouse wheel down should zoom out by proportional increments.");
    });
}

static void PreviewControlResetsZoomValue()
{
    RunOnStaThread(() =>
    {
        var preview = new TwentyFiveSlicePreviewControl
        {
            PreviewZoom = 1.75d
        };

        preview.ResetPreviewZoom();

        Assert.Equal(1d, preview.PreviewZoom, "Reset should restore 100% preview zoom.");
    });
}

static void PreviewControlAcceptsVariableGuideSelection()
{
    RunOnStaThread(() =>
    {
        var preview = new TwentyFiveSlicePreviewControl
        {
            SliceData = new TwentyFiveSliceData([10d, 20d, 30d, 40d, 50d, 60d], [15d, 30d, 45d, 60d, 75d])
        };

        preview.SelectGuide(isVertical: true, index: 5);
        preview.SelectGuide(isVertical: false, index: 4);
        preview.SelectGuide(isVertical: true, index: 99);
    });
}

static void PreviewViewportClampsPanToVisibleOverflow()
{
    var availableRect = new Rect(10d, 20d, 200d, 100d);

    PreviewViewport viewport = PreviewViewportCalculator.Calculate(
        availableRect,
        targetWidth: 100d,
        targetHeight: 50d,
        previewZoom: 2d,
        requestedPanX: 150d,
        requestedPanY: -90d);

    Assert.Equal(100d, viewport.PanX, "Horizontal pan should clamp to half of the zoomed overflow.");
    Assert.Equal(-50d, viewport.PanY, "Vertical pan should clamp to half of the zoomed overflow.");
    Assert.Equal(10d, viewport.PreviewRect.X, "Preview rect should include clamped horizontal pan.");
    Assert.Equal(-80d, viewport.PreviewRect.Y, "Preview rect should include clamped vertical pan.");

    PreviewViewport fittingViewport = PreviewViewportCalculator.Calculate(
        availableRect,
        targetWidth: 100d,
        targetHeight: 50d,
        previewZoom: 1d,
        requestedPanX: 40d,
        requestedPanY: 40d);

    Assert.Equal(0d, fittingViewport.PanX, "Pan should reset on an axis that fits inside the preview area.");
    Assert.Equal(0d, fittingViewport.PanY, "Pan should reset on an axis that fits inside the preview area.");
}

static void PreviewControlResetsZoomAndPan()
{
    RunOnStaThread(() =>
    {
        var preview = new TwentyFiveSlicePreviewControl
        {
            PreviewZoom = 1.75d,
            PreviewPanX = 42d,
            PreviewPanY = -31d
        };

        preview.ResetPreviewView();

        Assert.Equal(1d, preview.PreviewZoom, "Reset view should restore 100% preview zoom.");
        Assert.Equal(0d, preview.PreviewPanX, "Reset view should clear horizontal pan.");
        Assert.Equal(0d, preview.PreviewPanY, "Reset view should clear vertical pan.");
    });
}

static void SliceGuideInteractionDetectsIntersectionBeforeSingleGuides()
{
    var previewRect = new Rect(10d, 20d, 200d, 100d);
    var data = new TwentyFiveSliceData([25d, 50d, 75d, 90d], [20d, 50d, 80d, 90d]);

    SliceGuideHit hit = SliceGuideInteraction.HitTest(previewRect, data, new Point(110d, 70d));

    Assert.Equal(SliceGuideHitKind.Intersection, hit.Kind, "A point near both guide axes should select an intersection handle.");
    Assert.Equal(1, hit.VerticalIndex, "Intersection hit should preserve vertical guide index.");
    Assert.Equal(1, hit.HorizontalIndex, "Intersection hit should preserve horizontal guide index.");
}

static void SliceGuideInteractionDetectsVariableGuidesBeyondOriginalFour()
{
    var previewRect = new Rect(0d, 0d, 500d, 200d);
    var data = new TwentyFiveSliceData([10d, 20d, 30d, 40d, 50d, 60d], [15d, 30d, 45d, 60d, 75d]);

    SliceGuideHit verticalHit = SliceGuideInteraction.HitTest(previewRect, data, new Point(300d, 20d), threshold: 2d);
    SliceGuideHit horizontalHit = SliceGuideInteraction.HitTest(previewRect, data, new Point(20d, 150d), threshold: 2d);

    Assert.Equal(SliceGuideHitKind.Vertical, verticalHit.Kind, "Variable-grid hit testing should include guides after the original four.");
    Assert.Equal(5, verticalHit.VerticalIndex, "Vertical hit should preserve the later guide index.");
    Assert.Equal(SliceGuideHitKind.Horizontal, horizontalHit.Kind, "Variable-grid hit testing should include later Y guides.");
    Assert.Equal(4, horizontalHit.HorizontalIndex, "Horizontal hit should preserve the later guide index.");
}

static void SliceGuideInteractionConvertsPointToGuidePercent()
{
    var previewRect = new Rect(10d, 20d, 200d, 100d);

    Assert.Equal(50d, SliceGuideInteraction.ToVerticalPercent(previewRect, new Point(110d, 90d)), "Vertical percent should be measured from preview left.");
    Assert.Equal(25d, SliceGuideInteraction.ToHorizontalPercent(previewRect, new Point(160d, 45d)), "Horizontal percent should be measured from preview top.");
    Assert.Equal(100d, SliceGuideInteraction.ToVerticalPercent(previewRect, new Point(500d, 90d)), "Vertical percent should clamp to 100.");
    Assert.Equal(0d, SliceGuideInteraction.ToHorizontalPercent(previewRect, new Point(160d, -20d)), "Horizontal percent should clamp to 0.");
}

static void SliceDataJsonStaysUnityCompatible()
{
    var data = new TwentyFiveSliceData([12d, 42d, 58d, 88d], [10d, 40d, 60d, 90d]);

    string json = JsonSerializer.Serialize(data);
    TwentyFiveSliceData? loaded = JsonSerializer.Deserialize<TwentyFiveSliceData>(json);

    Assert.True(json.Contains("\"schemaVersion\":2"), "Standalone JSON should save the v2 variable-grid schema.");
    Assert.True(json.Contains("\"xGuidesPercent\""), "Standalone JSON should use the v2 X guide field name.");
    Assert.True(json.Contains("\"yGuidesPercent\""), "Standalone JSON should use the v2 Y guide field name.");
    Assert.True(loaded is not null, "Standalone JSON should deserialize back into slice data.");
    Assert.SequenceEqual(data.VerticalBorders, loaded!.VerticalBorders);
    Assert.SequenceEqual(data.HorizontalBorders, loaded.HorizontalBorders);
}

static void SliceValidationWarnsWhenFixedColumnsExceedTargetWidth()
{
    var data = new TwentyFiveSliceData([35d, 45d, 55d, 65d], [20d, 40d, 60d, 80d]);

    IReadOnlyList<SliceValidationMessage> messages = SliceValidationService.Validate(
        sourceWidth: 1000d,
        sourceHeight: 400d,
        targetWidth: 200d,
        targetHeight: 300d,
        data);

    Assert.Contains(messages, message => message.Severity == SliceValidationSeverity.Warning && message.Text.Contains("fixed columns"));
}

static void SliceValidationWarnsForVariableGridHazards()
{
    var data = new TwentyFiveSliceData(
        xGuidesPercent: [0.5d, 10d, 30d, 60d, 90d],
        yGuidesPercent: [10d, 20d, 40d, 60d, 80d],
        xSegments:
        [
            new SliceSegmentDefinition(SliceSegmentMode.Fixed),
            new SliceSegmentDefinition(SliceSegmentMode.Hidden),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Fixed),
            new SliceSegmentDefinition(SliceSegmentMode.Stretch),
            new SliceSegmentDefinition(SliceSegmentMode.Fixed)
        ]);

    IReadOnlyList<SliceValidationMessage> messages = SliceValidationService.Validate(
        sourceWidth: 100d,
        sourceHeight: 100d,
        targetWidth: 200d,
        targetHeight: 200d,
        data);

    Assert.Contains(messages, message => message.Text.Contains("hidden", StringComparison.OrdinalIgnoreCase));
    Assert.Contains(messages, message => message.Text.Contains("thinner than 2 pixels", StringComparison.OrdinalIgnoreCase));
    Assert.Contains(messages, message => message.Text.Contains("6 x 6", StringComparison.OrdinalIgnoreCase));
}

static void RecentFileListKeepsNewestUniqueFilesFirst()
{
    var recent = new RecentFileList(capacity: 3);

    recent.Add(@"C:\one.png");
    recent.Add(@"C:\two.png");
    recent.Add(@"C:\three.png");
    recent.Add(@"C:\one.png");
    recent.Add(@"C:\four.png");

    Assert.SequenceEqual(new[] { @"C:\four.png", @"C:\one.png", @"C:\three.png" }, recent.Files);
}

static void BatchPreviewAnalyzerReturnsWarningsPerTarget()
{
    var data = new TwentyFiveSliceData([30d, 45d, 55d, 70d], [30d, 45d, 55d, 70d]);

    IReadOnlyList<BatchPreviewResult> results = BatchPreviewAnalyzer.Analyze(
        sourceWidth: 1000d,
        sourceHeight: 1000d,
        data,
        [new PreviewTarget("Tiny", 120d, 120d), new PreviewTarget("Large", 1200d, 800d)]);

    Assert.Equal(2, results.Count, "Analyzer should return one result per target.");
    Assert.True(results[0].Messages.Count > 0, "Tiny target should have at least one warning.");
    Assert.Equal("Tiny", results[0].Target.Name, "First target name should be preserved.");
}

static void ImageBorderSuggestionServiceDetectsTransparentPadding()
{
    BitmapSource image = CreateTransparentPaddingBitmap(10, 10, 2, 2, 3, 2);

    TwentyFiveSliceData suggestion = ImageBorderSuggestionService.SuggestBorders(image);

    Assert.Equal(20d, suggestion.VerticalBorders[0], "Left transparent padding should be detected as 20%.");
    Assert.Equal(80d, suggestion.VerticalBorders[3], "Right transparent padding should place final vertical guide at 80%.");
    Assert.Equal(30d, suggestion.HorizontalBorders[0], "Top transparent padding should be detected as 30%.");
    Assert.Equal(80d, suggestion.HorizontalBorders[3], "Bottom transparent padding should place final horizontal guide at 80%.");
}

static void SliceAssistantAppliesNaturalLanguageEdits()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([20d, 40d, 60d, 80d], [10d, 35d, 65d, 92d]);

    SliceAssistantResult result = assistant.Apply("make top match bottom and thinner corners", data);

    Assert.True(result.Applied, "Assistant should apply a known command.");
    Assert.Equal(8d, result.SliceData.VerticalBorders[0], "Thinner corners should reduce the left fixed column.");
    Assert.Equal(92d, result.SliceData.HorizontalBorders[3], "Top matching bottom should preserve the bottom guide.");
    Assert.Equal(8d, result.SliceData.HorizontalBorders[0], "Thinner corners should reduce the top fixed row.");
}

static void SliceHistoryIgnoresDuplicatePushedStates()
{
    var state = new SliceEditorState([20d, 40d, 60d, 80d], [20d, 40d, 60d, 80d], 640d, 360d);
    var history = new SliceHistory(state);

    history.Push(state);

    Assert.True(!history.CanUndo, "Pushing the current state again should not create an undo entry.");
}

static void AppStateStoreRoundTripsRecentFilesAndPresets()
{
    string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
    var store = new AppStateStore(path);
    var state = new DesktopAppState
    {
        RecentFiles = [@"C:\art\panel.png", @"C:\art\button.png"],
        UserPresets =
        {
            ["Wide Button"] = new TwentyFiveSliceData([12d, 40d, 60d, 88d], [10d, 42d, 58d, 90d])
        }
    };

    try
    {
        store.Save(state);
        DesktopAppState loaded = store.Load();

        Assert.SequenceEqual(state.RecentFiles, loaded.RecentFiles);
        Assert.True(loaded.UserPresets.ContainsKey("Wide Button"), "User preset should round trip by name.");
        Assert.Equal(88d, loaded.UserPresets["Wide Button"].VerticalBorders[3], "Preset border values should round trip.");
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static void SliceAssistantAnalyzesCurrentSliceWarnings()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([35d, 45d, 55d, 65d], [35d, 45d, 55d, 65d]);

    string report = assistant.Analyze(sourceWidth: 1000d, sourceHeight: 1000d, targetWidth: 200d, targetHeight: 200d, data);

    Assert.True(report.Contains("frame-heavy", StringComparison.OrdinalIgnoreCase), "Analysis should classify the slice shape.");
    Assert.True(report.Contains("fixed columns", StringComparison.OrdinalIgnoreCase), "Analysis should include validation warnings.");
}

static void SliceAssistantReturnsStructuredRecommendations()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([6d, 38d, 62d, 94d], [6d, 38d, 62d, 94d]);

    SliceAssistantAnalysis analysis = assistant.AnalyzeDetailed(
        sourceWidth: 512d,
        sourceHeight: 128d,
        targetWidth: 1024d,
        targetHeight: 128d,
        data);

    Assert.Equal(SliceAssetKind.Button, analysis.AssetKind, "Wide, thin protected edges should classify as button.");
    Assert.True(analysis.Confidence >= 0.65d, "Button classification should have useful confidence.");
    Assert.Contains(analysis.RecommendedActions, action => action.Command.Contains("center stretch", StringComparison.OrdinalIgnoreCase));
}

static void ImageBorderSuggestionServiceExplainsConfidence()
{
    BitmapSource image = CreateTransparentPaddingBitmap(20, 20, 4, 4, 4, 4);

    SliceBorderSuggestion suggestion = ImageBorderSuggestionService.SuggestBordersDetailed(image);

    Assert.True(suggestion.Confidence >= 0.8d, "Transparent padding suggestion should be high confidence.");
    Assert.True(suggestion.Reasons.Any(reason => reason.Contains("transparent padding", StringComparison.OrdinalIgnoreCase)), "Suggestion should explain the source signal.");
    Assert.Equal(20d, suggestion.SliceData.VerticalBorders[0], "Detailed suggestion should include detected slice data.");
}

static void SliceAssistantUnderstandsBroaderPromptIntents()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([20d, 35d, 65d, 80d], [10d, 30d, 70d, 88d]);

    SliceAssistantResult result = assistant.Apply("make this a panel, symmetrize it, and make corners thicker", data);

    Assert.True(result.Applied, "Assistant should apply broader known intents.");
    Assert.Equal(result.SliceData.VerticalBorders[0], 100d - result.SliceData.VerticalBorders[3], "Symmetry should match left and right bands.");
    Assert.Equal(result.SliceData.HorizontalBorders[0], 100d - result.SliceData.HorizontalBorders[3], "Symmetry should match top and bottom bands.");
    Assert.True(result.SliceData.VerticalBorders[0] >= 16d, "Thicker corners should increase protected bands.");
}

static void SliceAssistantUnderstandsConversationalComplaints()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([12d, 32d, 68d, 88d], [12d, 32d, 68d, 88d]);

    SliceAssistantResult warpedCorners = assistant.Apply("the corners are getting warped when I resize it, can you protect them better?", data);
    Assert.True(warpedCorners.Applied, "Conversational corner complaints should be understood.");
    Assert.True(warpedCorners.SliceData.VerticalBorders[0] > data.VerticalBorders[0], "Protecting corners should thicken outer vertical bands.");

    SliceAssistantResult stretchedMiddle = assistant.Apply("the middle area looks weird and stretched, make the center more predictable", data);
    Assert.True(stretchedMiddle.Applied, "Conversational center stretch complaints should be understood.");
    Assert.Equal(40d, stretchedMiddle.SliceData.VerticalBorders[1], "Center stretch intent should set inner vertical guide.");

    SliceAssistantResult wideCta = assistant.Apply("I want this to behave like a wide CTA button without messing up the end caps", data);
    Assert.True(wideCta.Applied, "Conversational button intent should be understood.");
    Assert.Equal(10d, wideCta.SliceData.VerticalBorders[0], "Wide CTA intent should use button-fit borders.");

    SliceAssistantResult uneven = assistant.Apply("left and right don't feel even, can you balance the sides?", data);
    Assert.True(uneven.Applied, "Conversational balance intent should be understood.");
    Assert.Equal(uneven.SliceData.VerticalBorders[0], 100d - uneven.SliceData.VerticalBorders[3], "Balancing should symmetrize horizontal bands.");
}

static void SliceAssistantRanksCandidatePresets()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([20d, 40d, 60d, 80d], [20d, 40d, 60d, 80d]);

    IReadOnlyList<SliceCandidateSuggestion> candidates = assistant.RecommendCandidates(
        sourceWidth: 512d,
        sourceHeight: 128d,
        targetWidth: 1024d,
        targetHeight: 128d,
        data);

    Assert.True(candidates.Count >= 4, "Assistant should return several candidate strategies.");
    Assert.True(candidates[0].Score >= candidates[1].Score, "Candidates should be sorted by descending score.");
    Assert.True(candidates[0].Name.Contains("button", StringComparison.OrdinalIgnoreCase), "Wide assets should prefer a button candidate.");
}

static void SliceAssistantCandidateScoringReflectsWarnings()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([35d, 45d, 55d, 65d], [35d, 45d, 55d, 65d]);

    IReadOnlyList<SliceCandidateSuggestion> candidates = assistant.RecommendCandidates(
        sourceWidth: 1000d,
        sourceHeight: 1000d,
        targetWidth: 160d,
        targetHeight: 160d,
        data);

    Assert.True(candidates.All(candidate => candidate.Score >= 0d && candidate.Score <= 1d), "Candidate scores should be normalized.");
    Assert.True(candidates.Any(candidate => candidate.Reasons.Any(reason => reason.Contains("warning", StringComparison.OrdinalIgnoreCase))), "At least one candidate should explain validation warnings.");
}

static void SliceAssistantRanksCandidatesAcrossTargetSizes()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([30d, 45d, 55d, 70d], [30d, 45d, 55d, 70d]);

    IReadOnlyList<SliceCandidateSuggestion> candidates = assistant.RecommendCandidatesForTargets(
        sourceWidth: 1000d,
        sourceHeight: 1000d,
        targets:
        [
            new PreviewTarget("Tiny", 128d, 128d),
            new PreviewTarget("Medium", 512d, 512d),
            new PreviewTarget("Wide", 1024d, 256d)
        ],
        data);

    Assert.True(candidates.Count >= 4, "Batch optimizer should return several strategies.");
    Assert.True(candidates[0].Score >= candidates[^1].Score, "Batch candidates should be sorted by score.");
    Assert.True(!candidates[0].Name.Equals("Current slice", StringComparison.OrdinalIgnoreCase), "Warning-heavy current settings should not win across target sizes.");
}

static void SliceAssistantAggregateCandidatesIncludeTargetCoverage()
{
    var assistant = new SliceAssistantService();
    var data = new TwentyFiveSliceData([20d, 40d, 60d, 80d], [20d, 40d, 60d, 80d]);

    IReadOnlyList<SliceCandidateSuggestion> candidates = assistant.RecommendCandidatesForTargets(
        sourceWidth: 512d,
        sourceHeight: 128d,
        targets:
        [
            new PreviewTarget("Small button", 256d, 64d),
            new PreviewTarget("Large button", 1024d, 128d)
        ],
        data);

    Assert.True(candidates[0].Reasons.Any(reason => reason.Contains("2 target", StringComparison.OrdinalIgnoreCase)), "Best candidate should explain how many targets were evaluated.");
    Assert.True(candidates[0].Reasons.Any(reason => reason.Contains("average", StringComparison.OrdinalIgnoreCase)), "Best candidate should report an aggregate score reason.");
}

static void AssistantReportFormatterFormatsCandidateSummaries()
{
    var candidates = new[]
    {
        new SliceCandidateSuggestion("Button fit", TwentyFiveSliceData.CreateDefault(), 0.91d, ["No validation warnings.", "Asset classification matches."]),
        new SliceCandidateSuggestion("Panel fit", TwentyFiveSliceData.CreateDefault(), 0.72d, ["No validation warnings."])
    };

    string report = AssistantReportFormatter.FormatCandidateSuggestions(candidates);

    Assert.True(report.Contains("Applied Button fit", StringComparison.OrdinalIgnoreCase), "Report should name the applied candidate.");
    Assert.True(report.Contains("91", StringComparison.OrdinalIgnoreCase), "Report should include candidate score.");
    Assert.True(report.Contains("Other candidates", StringComparison.OrdinalIgnoreCase), "Report should include alternatives.");
}

static void AssistantReportFormatterFormatsCandidateListWithoutApplying()
{
    var candidates = new[]
    {
        new SliceCandidateSuggestion("Button fit", TwentyFiveSliceData.CreateDefault(), 0.91d, ["No validation warnings."]),
        new SliceCandidateSuggestion("Panel fit", TwentyFiveSliceData.CreateDefault(), 0.72d, ["One warning remains."])
    };

    string report = AssistantReportFormatter.FormatCandidateList(candidates);

    Assert.True(report.Contains("Top candidate: Button fit", StringComparison.OrdinalIgnoreCase), "Report should name the top candidate.");
    Assert.True(report.Contains("Select a candidate", StringComparison.OrdinalIgnoreCase), "Report should guide user selection.");
    Assert.True(!report.Contains("Applied Button fit", StringComparison.OrdinalIgnoreCase), "Candidate list should not imply an apply happened.");
}

static void PreviewTargetCatalogExposesCommonTargets()
{
    IReadOnlyList<PreviewTarget> targets = PreviewTargetCatalog.GetCommonTargets();

    Assert.True(targets.Count >= 4, "Catalog should expose multiple common preview targets.");
    Assert.True(targets.Any(target => target.Name.Contains("square", StringComparison.OrdinalIgnoreCase)), "Catalog should include square targets.");
    Assert.True(targets.Any(target => target.Width == 1920d && target.Height == 1080d), "Catalog should include 1080p target.");
}

static void AppStateStoreRoundTripsLastSession()
{
    string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
    var store = new AppStateStore(path);
    var state = new DesktopAppState
    {
        LastSession = new DesktopSessionState
        {
            ImagePath = @"C:\art\button.png",
            SliceData = new TwentyFiveSliceData([12d, 40d, 60d, 88d], [10d, 42d, 58d, 90d]),
            TargetWidth = 1024d,
            TargetHeight = 128d,
            PreviewZoom = 1.5d,
            PreviewPanX = 25d,
            PreviewPanY = -18d,
            KeepAspect = true,
            DebugOverlay = true,
            ExportDebug = true,
            SourceGuides = true,
            FlipX = true,
            FlipY = false,
            CandySkinEnabled = false,
            AssistantOutput = "Best fit: Button",
            ChatMessages = ["You: make this a button", "Assistant: proposal ready"]
        }
    };

    try
    {
        store.Save(state);
        DesktopAppState loaded = store.Load();

        Assert.True(loaded.LastSession is not null, "Last session should round trip.");
        DesktopSessionState session = loaded.LastSession!;
        Assert.Equal(@"C:\art\button.png", session.ImagePath, "Image path should round trip.");
        Assert.Equal(1024d, session.TargetWidth, "Target width should round trip.");
        Assert.Equal(1.5d, session.PreviewZoom, "Preview zoom should round trip.");
        Assert.Equal(25d, session.PreviewPanX, "Preview horizontal pan should round trip.");
        Assert.Equal(-18d, session.PreviewPanY, "Preview vertical pan should round trip.");
        Assert.Equal(true, session.KeepAspect, "Toggle state should round trip.");
        Assert.Equal(false, session.CandySkinEnabled, "Candy skin state should round trip.");
        Assert.Equal(88d, session.SliceData.VerticalBorders[3], "Slice data should round trip.");
        Assert.Equal("Best fit: Button", session.AssistantOutput, "Assistant output should round trip.");
        Assert.SequenceEqual(new[] { "You: make this a button", "Assistant: proposal ready" }, session.ChatMessages);
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static void CloudAiProviderCatalogIncludesCommonProviders()
{
    IReadOnlyList<CloudAiProviderDescriptor> providers = CloudAiProviderCatalog.GetAll();

    Assert.Contains(providers, provider => provider.Id == "openai");
    Assert.Contains(providers, provider => provider.Id == "anthropic");
    Assert.Contains(providers, provider => provider.Id == "gemini");
    Assert.Contains(providers, provider => provider.Id == "kimi");
    Assert.Contains(providers, provider => provider.Id == "deepseek");
    Assert.Contains(providers, provider => provider.Aliases.Contains("deepseel"));
    Assert.Contains(providers, provider => provider.Id == "glm");
    Assert.Contains(providers, provider => provider.Id == "minimax");
    Assert.Contains(providers, provider => provider.Id == "mistral");
    Assert.Contains(providers, provider => provider.Id == "cohere");
    Assert.Contains(providers, provider => provider.Id == "groq");
    Assert.Contains(providers, provider => provider.Id == "xai");
    Assert.Contains(providers, provider => provider.Id == "perplexity");
    Assert.Contains(providers, provider => provider.Id == "openai-compatible");
}

static void CloudAiRequestBuilderBuildsOpenAiCompatibleRequests()
{
    Environment.SetEnvironmentVariable("TFS_TEST_OPENAI_KEY", "openai-test-key");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "openai",
        ModelId = "gpt-5.1",
        ApiKeyEnvironmentVariable = "TFS_TEST_OPENAI_KEY"
    };

    using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.");
    string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

    Assert.Equal("https://api.openai.com/v1/chat/completions", request.RequestUri!.ToString(), "OpenAI request should use chat completions endpoint.");
    Assert.Equal(new AuthenticationHeaderValue("Bearer", "openai-test-key"), request.Headers.Authorization!, "OpenAI request should use bearer auth.");
    Assert.True(body.Contains("\"model\":\"gpt-5.1\""), "OpenAI body should include selected model.");
    Assert.True(body.Contains("Analyze this slice."), "OpenAI body should include prompt.");
    Assert.True(body.Contains("schemaVersion 2", StringComparison.Ordinal), "Cloud requests should steer models toward v2 variable-grid JSON.");
}

static void CloudAiRequestBuilderBuildsAnthropicRequests()
{
    Environment.SetEnvironmentVariable("TFS_TEST_ANTHROPIC_KEY", "anthropic-test-key");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("claude")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "anthropic",
        ModelId = "claude-sonnet-4-5",
        ApiKeyEnvironmentVariable = "TFS_TEST_ANTHROPIC_KEY"
    };

    using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.");
    string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

    Assert.Equal("https://api.anthropic.com/v1/messages", request.RequestUri!.ToString(), "Anthropic request should use Messages endpoint.");
    Assert.True(request.Headers.TryGetValues("x-api-key", out IEnumerable<string>? keys) && keys.Single() == "anthropic-test-key", "Anthropic request should use x-api-key.");
    Assert.True(request.Headers.Contains("anthropic-version"), "Anthropic request should include API version header.");
    Assert.True(body.Contains("\"max_tokens\""), "Anthropic body should include max_tokens.");
}

static void CloudAiRequestBuilderBuildsGeminiRequests()
{
    Environment.SetEnvironmentVariable("TFS_TEST_GEMINI_KEY", "gemini-test-key");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("gemini")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "gemini",
        ModelId = "gemini-3-pro",
        ApiKeyEnvironmentVariable = "TFS_TEST_GEMINI_KEY"
    };

    using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.");
    string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

    Assert.True(request.RequestUri!.ToString().Contains("models/gemini-3-pro:generateContent"), "Gemini request should target model generateContent.");
    Assert.True(request.RequestUri!.ToString().Contains("key=gemini-test-key"), "Gemini request should include API key query parameter.");
    Assert.True(body.Contains("Analyze this slice."), "Gemini body should include prompt.");
}

static void CloudAiRequestBuilderIncludesOpenAiImagePayload()
{
    Environment.SetEnvironmentVariable("TFS_TEST_OPENAI_KEY", "openai-test-key");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "openai",
        ModelId = "gpt-5.1",
        ApiKeyEnvironmentVariable = "TFS_TEST_OPENAI_KEY"
    };

    using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.", CreateCloudImage());
    string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

    Assert.True(body.Contains("\"image_url\""), "OpenAI-compatible vision request should include image_url content.");
    Assert.True(body.Contains("data:image/png;base64,abc123"), "OpenAI-compatible vision request should include a data image URL.");
    Assert.True(body.Contains("\"text\":\"Analyze this slice.\""), "OpenAI-compatible vision request should keep the text prompt.");
}

static void CloudAiRequestBuilderIncludesAnthropicImagePayload()
{
    Environment.SetEnvironmentVariable("TFS_TEST_ANTHROPIC_KEY", "anthropic-test-key");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("claude")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "anthropic",
        ModelId = "claude-sonnet-4-5",
        ApiKeyEnvironmentVariable = "TFS_TEST_ANTHROPIC_KEY"
    };

    using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.", CreateCloudImage());
    string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

    Assert.True(body.Contains("\"type\":\"image\""), "Anthropic vision request should include an image content block.");
    Assert.True(body.Contains("\"media_type\":\"image/png\""), "Anthropic vision request should include the image MIME type.");
    Assert.True(body.Contains("\"data\":\"abc123\""), "Anthropic vision request should include base64 image data.");
}

static void CloudAiRequestBuilderIncludesGeminiImagePayload()
{
    Environment.SetEnvironmentVariable("TFS_TEST_GEMINI_KEY", "gemini-test-key");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("gemini")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "gemini",
        ModelId = "gemini-3-pro",
        ApiKeyEnvironmentVariable = "TFS_TEST_GEMINI_KEY"
    };

    using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.", CreateCloudImage());
    string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

    Assert.True(body.Contains("\"inlineData\""), "Gemini vision request should include inlineData.");
    Assert.True(body.Contains("\"mimeType\":\"image/png\""), "Gemini vision request should include the image MIME type.");
    Assert.True(body.Contains("\"data\":\"abc123\""), "Gemini vision request should include base64 image data.");
}

static void CloudAiRequestBuilderOmitsImagePayloadForTextOnlyProviders()
{
    Environment.SetEnvironmentVariable("TFS_TEST_DEEPSEEK_KEY", "deepseek-test-key");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("deepseek")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "deepseek",
        ModelId = "deepseek-chat",
        ApiKeyEnvironmentVariable = "TFS_TEST_DEEPSEEK_KEY"
    };

    using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.", CreateCloudImage());
    string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

    Assert.True(!body.Contains("abc123"), "Text-only providers should not receive base64 image data.");
    Assert.True(!body.Contains("\"image_url\""), "Text-only providers should not receive image content blocks.");
    Assert.True(body.Contains("Analyze this slice."), "Text-only provider request should still include the prompt.");
}

static void AppStateStoreRoundTripsCloudAiSettings()
{
    string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
    var store = new AppStateStore(path);
    var state = new DesktopAppState
    {
        CloudAi = new CloudAiSettings
        {
            Enabled = true,
            ProviderId = "kimi",
            ModelId = "kimi-k2.5",
            EndpointOverride = "https://example.test/v1/chat/completions",
            ApiKeyEnvironmentVariable = "MOONSHOT_API_KEY"
        }
    };

    try
    {
        store.Save(state);
        DesktopAppState loaded = store.Load();

        Assert.True(loaded.CloudAi.Enabled, "Cloud AI enabled flag should round trip.");
        Assert.Equal("kimi", loaded.CloudAi.ProviderId, "Provider should round trip.");
        Assert.Equal("kimi-k2.5", loaded.CloudAi.ModelId, "Model should round trip.");
        Assert.Equal("MOONSHOT_API_KEY", loaded.CloudAi.ApiKeyEnvironmentVariable, "API key environment variable should round trip.");
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static void CloudAiClientParsesOpenAiCompatibleResponses()
{
    Environment.SetEnvironmentVariable("TFS_TEST_OPENAI_KEY", "openai-test-key");
    using var client = new CloudAiClient(new HttpClient(new FakeHttpHandler("""{"choices":[{"message":{"content":"Use 12/42/58/88 borders."}}]}""")));
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        Enabled = true,
        ProviderId = "openai",
        ApiKeyEnvironmentVariable = "TFS_TEST_OPENAI_KEY"
    };

    CloudAiResult result = client.AskAsync(provider, settings, "Analyze").GetAwaiter().GetResult();

    Assert.True(result.Success, "OpenAI-compatible response should parse successfully.");
    Assert.Equal("Use 12/42/58/88 borders.", result.Advice, "Advice text should be extracted.");
}

static void CloudAiClientParsesAnthropicResponses()
{
    Environment.SetEnvironmentVariable("TFS_TEST_ANTHROPIC_KEY", "anthropic-test-key");
    using var client = new CloudAiClient(new HttpClient(new FakeHttpHandler("""{"content":[{"type":"text","text":"Keep the center stretch narrow."}]}""")));
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("claude")!;
    var settings = new CloudAiSettings
    {
        Enabled = true,
        ProviderId = "anthropic",
        ApiKeyEnvironmentVariable = "TFS_TEST_ANTHROPIC_KEY"
    };

    CloudAiResult result = client.AskAsync(provider, settings, "Analyze").GetAwaiter().GetResult();

    Assert.True(result.Success, "Anthropic response should parse successfully.");
    Assert.Equal("Keep the center stretch narrow.", result.Advice, "Advice text should be extracted.");
}

static void CloudAiClientParsesGeminiResponses()
{
    Environment.SetEnvironmentVariable("TFS_TEST_GEMINI_KEY", "gemini-test-key");
    using var client = new CloudAiClient(new HttpClient(new FakeHttpHandler("""{"candidates":[{"content":{"parts":[{"text":"Use symmetrical edges."}]}}]}""")));
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("gemini")!;
    var settings = new CloudAiSettings
    {
        Enabled = true,
        ProviderId = "gemini",
        ApiKeyEnvironmentVariable = "TFS_TEST_GEMINI_KEY"
    };

    CloudAiResult result = client.AskAsync(provider, settings, "Analyze").GetAwaiter().GetResult();

    Assert.True(result.Success, "Gemini response should parse successfully.");
    Assert.Equal("Use symmetrical edges.", result.Advice, "Advice text should be extracted.");
}

static void CloudAiClientReportsHttpFailures()
{
    Environment.SetEnvironmentVariable("TFS_TEST_OPENAI_KEY", "openai-test-key");
    using var client = new CloudAiClient(new HttpClient(new FakeHttpHandler("""{"error":"bad"}""", System.Net.HttpStatusCode.BadRequest)));
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        Enabled = true,
        ProviderId = "openai",
        ApiKeyEnvironmentVariable = "TFS_TEST_OPENAI_KEY"
    };

    CloudAiResult result = client.AskAsync(provider, settings, "Analyze").GetAwaiter().GetResult();

    Assert.True(!result.Success, "HTTP failures should be reported as unsuccessful.");
    Assert.True(result.ErrorMessage.Contains("400", StringComparison.OrdinalIgnoreCase), "Error should include status code.");
}

static void CloudAiClientReportsMissingApiKey()
{
    Environment.SetEnvironmentVariable("TFS_TEST_MISSING_KEY", null);
    using var client = new CloudAiClient(new HttpClient(new FakeHttpHandler("""{}""")));
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        Enabled = true,
        ProviderId = "openai",
        ApiKeyEnvironmentVariable = "TFS_TEST_MISSING_KEY"
    };

    CloudAiResult result = client.AskAsync(provider, settings, "Analyze").GetAwaiter().GetResult();

    Assert.True(!result.Success, "Missing API key should be reported as unsuccessful.");
    Assert.True(result.ErrorMessage.Contains("TFS_TEST_MISSING_KEY", StringComparison.OrdinalIgnoreCase), "Error should name missing env var.");
}

static void CloudAiSecretStoreProtectsApiKeysAtRest()
{
    string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-secrets.json");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "openai",
        ApiKeyEnvironmentVariable = "TFS_TEST_SECURE_STORE_KEY"
    };

    try
    {
        var store = new CloudAiSecretStore(path, new TestSecretProtector());
        store.SaveSecret(provider, settings, "super-secret-api-key");
        string persisted = File.ReadAllText(path);

        Assert.True(!persisted.Contains("super-secret-api-key", StringComparison.Ordinal), "Secret store should never persist raw API keys.");
        Assert.True(store.HasSecret(provider, settings), "Saved secret should be discoverable by provider and env var.");
        Assert.True(store.TryGetSecret(provider, settings, out string apiKey), "Saved secret should decrypt for the current user scope.");
        Assert.Equal("super-secret-api-key", apiKey, "Decrypted API key should match the saved value.");
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static void CloudAiRequestBuilderResolvesSecureStoredKeys()
{
    Environment.SetEnvironmentVariable("TFS_TEST_SECURE_ONLY_KEY", null);
    string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-secrets.json");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        ProviderId = "openai",
        ModelId = "gpt-5.1",
        ApiKeyEnvironmentVariable = "TFS_TEST_SECURE_ONLY_KEY",
        UseSecureApiKeyStore = true
    };

    try
    {
        var store = new CloudAiSecretStore(path, new TestSecretProtector());
        store.SaveSecret(provider, settings, "secure-store-test-key");

        using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, "Analyze this slice.", secretStore: store);

        Assert.Equal(new AuthenticationHeaderValue("Bearer", "secure-store-test-key"), request.Headers.Authorization!, "Request builder should use encrypted store key when env var is absent.");
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static void CloudAiClientUsesSecureStoredKeys()
{
    Environment.SetEnvironmentVariable("TFS_TEST_CLIENT_SECURE_KEY", null);
    string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-secrets.json");
    CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find("openai")!;
    var settings = new CloudAiSettings
    {
        Enabled = true,
        ProviderId = "openai",
        ApiKeyEnvironmentVariable = "TFS_TEST_CLIENT_SECURE_KEY",
        UseSecureApiKeyStore = true
    };

    try
    {
        var store = new CloudAiSecretStore(path, new TestSecretProtector());
        store.SaveSecret(provider, settings, "secure-client-test-key");
        using var client = new CloudAiClient(new HttpClient(new FakeHttpHandler("""{"choices":[{"message":{"content":"Secure key worked."}}]}""")), store);

        CloudAiResult result = client.AskAsync(provider, settings, "Analyze").GetAwaiter().GetResult();

        Assert.True(result.Success, "Cloud client should send requests with secure stored keys.");
        Assert.Equal("Secure key worked.", result.Advice, "Cloud client should parse response after using secure stored key.");
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static void CloudAiAdviceParserExtractsJsonBorderSuggestions()
{
    string advice = """
    Use slightly thinner edges.
    {
      "verticalBorders": [12, 42, 58, 88],
      "horizontalBorders": [10, 40, 60, 90]
    }
    """;

    Assert.True(CloudAiAdviceParser.TryParseSliceData(advice, out TwentyFiveSliceData? data), "Parser should extract JSON border arrays from advice.");
    Assert.SequenceEqual(new[] { 12d, 42d, 58d, 88d }, data!.VerticalBorders);
    Assert.SequenceEqual(new[] { 10d, 40d, 60d, 90d }, data.HorizontalBorders);
}

static void CloudAiAdviceParserExtractsInlineBorderSuggestions()
{
    string advice = "Suggested verticalBorders: 8, 36, 64, 92 and horizontalBorders: 14 / 44 / 56 / 86.";

    Assert.True(CloudAiAdviceParser.TryParseSliceData(advice, out TwentyFiveSliceData? data), "Parser should extract inline border lists from advice.");
    Assert.SequenceEqual(new[] { 8d, 36d, 64d, 92d }, data!.VerticalBorders);
    Assert.SequenceEqual(new[] { 14d, 44d, 56d, 86d }, data.HorizontalBorders);
}

static void CloudAiAdviceParserExtractsFencedSnakeCaseJsonSuggestions()
{
    string advice = """
    Try this:

    ```json
    {
      "vertical_borders": ["9%", "39%", "61%", "91%"],
      "horizontal_borders": ["11%", "41%", "59%", "89%"]
    }
    ```
    """;

    Assert.True(CloudAiAdviceParser.TryParseSliceData(advice, out TwentyFiveSliceData? data), "Parser should extract fenced snake_case JSON with percentage strings.");
    Assert.SequenceEqual(new[] { 9d, 39d, 61d, 91d }, data!.VerticalBorders);
    Assert.SequenceEqual(new[] { 11d, 41d, 59d, 89d }, data.HorizontalBorders);
}

static void CloudAiAdviceParserExtractsSpacedPercentageLabels()
{
    string advice = "Vertical borders = 7%, 34%, 66%, 93%. Horizontal borders = 15%, 45%, 55%, 85%.";

    Assert.True(CloudAiAdviceParser.TryParseSliceData(advice, out TwentyFiveSliceData? data), "Parser should extract spaced labels with percentage values.");
    Assert.SequenceEqual(new[] { 7d, 34d, 66d, 93d }, data!.VerticalBorders);
    Assert.SequenceEqual(new[] { 15d, 45d, 55d, 85d }, data.HorizontalBorders);
}

static void CloudAiAdviceParserExtractsVariableGridSuggestions()
{
    string advice = """
        Try this grid:
        {
          "schemaVersion": 2,
          "xGuidesPercent": [8, 20, 42, 58, 80, 92],
          "yGuidesPercent": [12, 35, 65, 88],
          "xSegments": [{"mode":"fixed"},{"mode":"stretch"},{"mode":"hidden"},{"mode":"stretch"},{"mode":"fixed"},{"mode":"stretch"},{"mode":"fixed"}],
          "ySegments": [{"mode":"fixed"},{"mode":"stretch"},{"mode":"fixed"},{"mode":"stretch"},{"mode":"fixed"}]
        }
        """;

    Assert.True(CloudAiAdviceParser.TryParseSliceData(advice, out TwentyFiveSliceData? data), "Parser should extract v2 variable-grid JSON suggestions.");
    Assert.SequenceEqual(new[] { 8d, 20d, 42d, 58d, 80d, 92d }, data!.XGuidesPercent);
    Assert.SequenceEqual(new[] { 12d, 35d, 65d, 88d }, data.YGuidesPercent);
    Assert.Equal(SliceSegmentMode.Hidden, data.XSegments[2].Mode, "Variable-grid parser should keep segment modes.");
}

static void IdeAssistantBridgeReturnsDiscoverableProviderJson()
{
    string json = IdeAssistantBridge.ListProvidersJson();
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;

    Assert.Equal("twenty-five-slicer.ai.providers.v1", root.GetProperty("schema").GetString(), "Provider output should expose a stable schema name.");
    JsonElement providers = root.GetProperty("providers");
    Assert.True(providers.GetArrayLength() >= 8, "IDE provider output should include common cloud providers.");
    Assert.True(providers.EnumerateArray().Any(provider => provider.GetProperty("id").GetString() == "openai"), "Provider output should include OpenAI.");
    Assert.True(providers.EnumerateArray().Any(provider => provider.GetProperty("aliases").EnumerateArray().Any(alias => alias.GetString() == "chatgpt")), "Provider aliases should be discoverable.");
}

static void IdeAssistantBridgeReturnsLocalAnalysisJson()
{
    var input = new IdeAssistantInput(
        new TwentyFiveSliceData([35d, 45d, 55d, 65d], [20d, 40d, 60d, 80d]),
        SourceWidth: 1000d,
        SourceHeight: 400d,
        TargetWidth: 200d,
        TargetHeight: 300d);

    string json = IdeAssistantBridge.AnalyzeLocalJson(input);
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;

    Assert.Equal("twenty-five-slicer.ai.analysis.v1", root.GetProperty("schema").GetString(), "Analysis output should expose a stable schema name.");
    Assert.True(root.GetProperty("observations").GetArrayLength() > 0, "Analysis output should include observations.");
    Assert.True(root.GetProperty("validationMessages").GetArrayLength() > 0, "Analysis output should include validation messages.");
    Assert.True(root.GetProperty("recommendedActions").GetArrayLength() > 0, "Analysis output should include recommended actions.");
}

static void IdeAssistantBridgeAppliesPromptAsJson()
{
    var input = new IdeAssistantInput(
        TwentyFiveSliceData.CreateDefault(),
        SourceWidth: 100d,
        SourceHeight: 100d,
        TargetWidth: 320d,
        TargetHeight: 96d);

    string json = IdeAssistantBridge.ApplyPromptJson(input, "make this a button");
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;

    Assert.Equal("twenty-five-slicer.ai.apply.v1", root.GetProperty("schema").GetString(), "Apply output should expose a stable schema name.");
    Assert.True(root.GetProperty("applied").GetBoolean(), "Known prompts should be applied.");
    JsonElement sliceData = root.GetProperty("sliceData");
    Assert.Equal(10d, sliceData.GetProperty("xGuidesPercent")[0].GetDouble(), "Apply output should include v2 X guides.");
    Assert.Equal(90d, sliceData.GetProperty("xGuidesPercent")[3].GetDouble(), "Apply output should include v2 X guides.");
}

static void IdeAssistantBridgeReturnsPromptPreviewJson()
{
    var input = new IdeAssistantInput(
        new TwentyFiveSliceData([12d, 42d, 58d, 88d], [10d, 40d, 60d, 90d]),
        SourceWidth: 256d,
        SourceHeight: 128d,
        TargetWidth: 640d,
        TargetHeight: 160d);

    string json = IdeAssistantBridge.BuildPromptJson(input, "make this safer for a wide button");
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;

    Assert.Equal("twenty-five-slicer.ai.prompt.v1", root.GetProperty("schema").GetString(), "Prompt output should expose a stable schema name.");
    string prompt = root.GetProperty("prompt").GetString()!;
    Assert.True(prompt.Contains("make this safer", StringComparison.OrdinalIgnoreCase), "Prompt preview should include the user request.");
    Assert.True(prompt.Contains("schemaVersion: 2", StringComparison.Ordinal), "Prompt preview should ask IDE assistants for v2 variable-grid JSON.");
    Assert.True(prompt.Contains("X segment modes", StringComparison.Ordinal), "Prompt preview should include per-axis segment modes.");
    Assert.True(root.GetProperty("sliceData").GetProperty("xGuidesPercent").GetArrayLength() == 4, "Prompt preview should include the slice data being analyzed.");
}

static void IdeAssistantBridgeReturnsStructuredErrorJson()
{
    string json = IdeAssistantBridge.ErrorJson("Missing required option --slice.", "ArgumentError");
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;

    Assert.Equal("twenty-five-slicer.ai.error.v1", root.GetProperty("schema").GetString(), "Error output should expose a stable schema name.");
    Assert.Equal("ArgumentError", root.GetProperty("errorCode").GetString(), "Error output should include a machine-readable code.");
    Assert.Equal("Missing required option --slice.", root.GetProperty("message").GetString(), "Error output should include the display message.");
}

static void IdeAssistantBridgeReturnsSuggestionReviewJson()
{
    var input = new IdeAssistantInput(
        TwentyFiveSliceData.CreateDefault(),
        SourceWidth: 1000d,
        SourceHeight: 1000d,
        TargetWidth: 400d,
        TargetHeight: 400d);
    var proposed = new TwentyFiveSliceData([8d, 40d, 60d, 92d], [8d, 40d, 60d, 92d]);

    string json = IdeAssistantBridge.ReviewSuggestionJson(input, proposed);
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;

    Assert.Equal("twenty-five-slicer.ai.review.v1", root.GetProperty("schema").GetString(), "Review output should expose a stable schema name.");
    Assert.True(root.GetProperty("review").GetProperty("safeToApply").GetBoolean(), "Safe suggestions should be marked safe in IDE review JSON.");
    Assert.Equal(4, root.GetProperty("review").GetProperty("verticalPixelDeltas").GetArrayLength(), "Review JSON should expose vertical pixel deltas.");
}

static void IdeAssistantBridgeReturnsImageAwareSuggestionReviewJson()
{
    RunOnStaThread(() =>
    {
        BitmapSource image = CreateTransparentPaddingBitmap(100, 100, 12, 12, 8, 8);
        var input = new IdeAssistantInput(
            TwentyFiveSliceData.CreateDefault(),
            SourceWidth: 100d,
            SourceHeight: 100d,
            TargetWidth: 220d,
            TargetHeight: 220d);
        var proposed = new TwentyFiveSliceData([2d, 40d, 60d, 98d], [2d, 40d, 60d, 98d]);

        string json = IdeAssistantBridge.ReviewSuggestionJson(input, proposed, image);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("twenty-five-slicer.ai.review.v1", root.GetProperty("schema").GetString(), "Image-aware review output should keep the stable review schema.");
        Assert.True(!root.GetProperty("review").GetProperty("safeToApply").GetBoolean(), "Image-aware review should block padding mismatches.");
        Assert.True(root.GetProperty("review").GetProperty("risks").EnumerateArray().Any(risk => risk.GetString()!.Contains("opaque content bounds", StringComparison.OrdinalIgnoreCase)), "Image-aware review should expose content-bound risk.");
    });
}

static void SliceSuggestionReviewBlocksRiskyAiCuts()
{
    var current = TwentyFiveSliceData.CreateDefault();
    var risky = new TwentyFiveSliceData([35d, 45d, 55d, 65d], [35d, 45d, 55d, 65d]);

    SliceSuggestionReview review = SliceSuggestionReviewService.Review(
        current,
        risky,
        sourceWidth: 1000d,
        sourceHeight: 1000d,
        targetWidth: 200d,
        targetHeight: 200d);

    Assert.True(!review.SafeToApply, "Risky suggestions should not be marked safe to apply.");
    Assert.True(review.Score < 0.7d, "Risky suggestions should receive a lower confidence score.");
    Assert.True(review.Risks.Any(risk => risk.Contains("fixed", StringComparison.OrdinalIgnoreCase)), "Risk review should explain fixed-region problems.");
}

static void SliceSuggestionReviewRewardsWarningReductions()
{
    var current = new TwentyFiveSliceData([35d, 45d, 55d, 65d], [35d, 45d, 55d, 65d]);
    var proposed = new TwentyFiveSliceData([8d, 40d, 60d, 92d], [8d, 40d, 60d, 92d]);

    SliceSuggestionReview review = SliceSuggestionReviewService.Review(
        current,
        proposed,
        sourceWidth: 1000d,
        sourceHeight: 1000d,
        targetWidth: 400d,
        targetHeight: 400d);

    Assert.True(review.SafeToApply, "Suggestions that remove target warnings should be safe to apply.");
    Assert.True(review.Score >= 0.8d, "Suggestions that reduce warnings should receive a strong confidence score.");
    Assert.True(review.Improvements.Any(improvement => improvement.Contains("reduced", StringComparison.OrdinalIgnoreCase)), "Review should explain warning reductions.");
    Assert.Equal(4, review.VerticalPixelDeltas.Count, "Review should expose per-guide vertical pixel deltas.");
    Assert.Equal(4, review.HorizontalPixelDeltas.Count, "Review should expose per-guide horizontal pixel deltas.");
}

static void SliceSuggestionReviewRewardsImagePaddingAlignment()
{
    RunOnStaThread(() =>
    {
        BitmapSource image = CreateTransparentPaddingBitmap(100, 100, 12, 12, 8, 8);
        var current = TwentyFiveSliceData.CreateDefault();
        var proposed = new TwentyFiveSliceData([12d, 40d, 60d, 88d], [8d, 40d, 60d, 92d]);

        SliceSuggestionReview review = SliceSuggestionReviewService.Review(
            current,
            proposed,
            image,
            targetWidth: 220d,
            targetHeight: 220d);

        Assert.True(review.SafeToApply, "Suggestions aligned with detected transparent padding should be safe.");
        Assert.True(review.Improvements.Any(improvement => improvement.Contains("opaque content bounds", StringComparison.OrdinalIgnoreCase)), "Review should explain image-bound alignment.");
    });
}

static void SliceSuggestionReviewBlocksImagePaddingMismatch()
{
    RunOnStaThread(() =>
    {
        BitmapSource image = CreateTransparentPaddingBitmap(100, 100, 12, 12, 8, 8);
        var current = TwentyFiveSliceData.CreateDefault();
        var proposed = new TwentyFiveSliceData([2d, 40d, 60d, 98d], [2d, 40d, 60d, 98d]);

        SliceSuggestionReview review = SliceSuggestionReviewService.Review(
            current,
            proposed,
            image,
            targetWidth: 220d,
            targetHeight: 220d);

        Assert.True(!review.SafeToApply, "Suggestions far from detected transparent padding should not auto-apply.");
        Assert.True(review.Risks.Any(risk => risk.Contains("opaque content bounds", StringComparison.OrdinalIgnoreCase)), "Review should explain image-bound mismatch.");
        Assert.True(review.Score < 0.78d, "Image-bound mismatch should lower the score below the safe threshold.");
    });
}

static void SliceChatServiceCreatesReviewedSuggestion()
{
    var chat = new SliceChatService();
    var context = new SliceChatContext(
        TwentyFiveSliceData.CreateDefault(),
        SourceWidth: 1000d,
        SourceHeight: 1000d,
        TargetWidth: 400d,
        TargetHeight: 400d);

    SliceChatResponse response = chat.Send("make this a button", context);

    Assert.True(response.ProposedSliceData is not null, "Known edit prompts should return a proposed slice.");
    Assert.True(response.Review is not null, "Proposed slices should include deterministic review.");
    Assert.True(response.AssistantMessage.Contains("review", StringComparison.OrdinalIgnoreCase), "Chat response should mention review status.");
}

static void SliceChatServiceCreatesReviewedCloudSuggestion()
{
    var chat = new SliceChatService();
    var context = new SliceChatContext(
        TwentyFiveSliceData.CreateDefault(),
        SourceWidth: 1000d,
        SourceHeight: 1000d,
        TargetWidth: 400d,
        TargetHeight: 400d);

    SliceChatResponse response = chat.FromCloudAdvice(
        "Use this exact slice: {\"verticalBorders\":[8,40,60,92],\"horizontalBorders\":[8,40,60,92]}",
        context,
        "OpenAI / ChatGPT");

    Assert.True(response.ProposedSliceData is not null, "Cloud advice with border JSON should return a proposed slice.");
    Assert.True(response.Review?.SafeToApply == true, "Cloud proposals should include deterministic review.");
    Assert.True(response.AssistantMessage.Contains("OpenAI", StringComparison.OrdinalIgnoreCase), "Cloud chat response should name the provider.");
}

static void SliceChatServiceHandlesConversationalEditRequest()
{
    var chat = new SliceChatService();
    var context = new SliceChatContext(
        new TwentyFiveSliceData([12d, 32d, 68d, 88d], [12d, 32d, 68d, 88d]),
        SourceWidth: 1000d,
        SourceHeight: 1000d,
        TargetWidth: 400d,
        TargetHeight: 400d);

    SliceChatResponse response = chat.Send("the middle feels too stretchy and the end caps are getting weird", context);

    Assert.True(response.ProposedSliceData is not null, "Conversational chat edits should create a proposal.");
    Assert.True(response.Review is not null, "Conversational chat proposals should be reviewed.");
    Assert.True(response.AssistantMessage.Contains("proposal", StringComparison.OrdinalIgnoreCase), "Conversational chat should talk in proposal terms.");
}

static void SliceChatServiceRecognizesProposalCommands()
{
    Assert.Equal(SliceChatCommand.PreviewProposal, SliceChatService.ParseCommand("preview it"), "Chat should recognize preview command.");
    Assert.Equal(SliceChatCommand.ApplyProposal, SliceChatService.ParseCommand("apply that suggestion"), "Chat should recognize apply command.");
    Assert.Equal(SliceChatCommand.RejectProposal, SliceChatService.ParseCommand("reject it"), "Chat should recognize reject command.");
    Assert.Equal(SliceChatCommand.ExplainRisk, SliceChatService.ParseCommand("explain the risk"), "Chat should recognize risk explanation command.");
    Assert.Equal(SliceChatCommand.TrySaferProposal, SliceChatService.ParseCommand("try a safer version"), "Chat should recognize safer retry command.");
    Assert.Equal(SliceChatCommand.None, SliceChatService.ParseCommand("make this a button"), "Normal prompts should not be treated as proposal commands.");
}

static void SliceChatServiceAnswersAnalysisWithoutProposal()
{
    var chat = new SliceChatService();
    var context = new SliceChatContext(
        TwentyFiveSliceData.CreateDefault(),
        SourceWidth: 1000d,
        SourceHeight: 500d,
        TargetWidth: 640d,
        TargetHeight: 160d);

    SliceChatResponse response = chat.Send("what do you think about this?", context);

    Assert.True(response.ProposedSliceData is null, "Analysis prompts should not propose a slice edit.");
    Assert.True(response.AssistantMessage.Contains("looks like", StringComparison.OrdinalIgnoreCase), "Analysis response should summarize the current slice.");
}

static BitmapSource CreateTransparentPaddingBitmap(int width, int height, int left, int right, int top, int bottom)
{
    var pixels = new byte[width * height * 4];
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int index = ((y * width) + x) * 4;
            bool opaque = x >= left && x < width - right && y >= top && y < height - bottom;
            pixels[index] = 255;
            pixels[index + 1] = 255;
            pixels[index + 2] = 255;
            pixels[index + 3] = opaque ? (byte)255 : (byte)0;
        }
    }

    return BitmapSource.Create(width, height, 96d, 96d, PixelFormats.Bgra32, null, pixels, width * 4);
}

static CloudAiImageInput CreateCloudImage()
{
    return new CloudAiImageInput("image/png", "abc123");
}

static IReadOnlyList<GoldenSample> LoadGoldenSamples()
{
    string root = Path.Combine(TestProjectRoot(), "GoldenSamples");
    return
    [
        LoadGoldenSample("button", Path.Combine(root, "button-100x40-padding.png"), Path.Combine(root, "button-100x40-padding.25slice.json")),
        LoadGoldenSample("panel", Path.Combine(root, "panel-120x80-padding.png"), Path.Combine(root, "panel-120x80-padding.25slice.json"))
    ];
}

static GoldenSample LoadGoldenSample(string name, string imagePath, string slicePath)
{
    return new GoldenSample(name, imagePath, slicePath, LoadBitmap(imagePath), ReadSliceData(slicePath));
}

static BitmapSource LoadBitmap(string imagePath)
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

static TwentyFiveSliceData ReadSliceData(string slicePath)
{
    return JsonSerializer.Deserialize<TwentyFiveSliceData>(File.ReadAllText(slicePath)) ??
        throw new InvalidDataException($"Unable to read slice data from {slicePath}.");
}

static void AssertAxisNear(IReadOnlyList<double> expected, IReadOnlyList<double> actual, double tolerance, string message)
{
    Assert.Equal(expected.Count, actual.Count, message);
    for (int index = 0; index < expected.Count; index++)
    {
        double delta = Math.Abs(expected[index] - actual[index]);
        if (delta > tolerance)
        {
            throw new InvalidOperationException($"{message} Index {index} expected {expected[index]:0.###}, got {actual[index]:0.###}, delta {delta:0.###}.");
        }
    }
}

static string TestProjectRoot()
{
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
}

static string RepositoryRoot()
{
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}

static string DesktopProjectRoot()
{
    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TwentyFiveSlicer.Desktop"));
}

static bool BitmapHasVisiblePixels(BitmapSource bitmap)
{
    var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
    bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

    for (int index = 3; index < pixels.Length; index += 4)
    {
        if (pixels[index] > 0)
        {
            return true;
        }
    }

    return false;
}

static byte[] EncodePng(BitmapSource bitmap)
{
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return stream.ToArray();
}

static bool IsPng(byte[] bytes)
{
    byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    return bytes.Length >= signature.Length &&
        signature.SequenceEqual(bytes.Take(signature.Length));
}

static int CountOccurrences(string value, string pattern)
{
    int count = 0;
    int index = 0;
    while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
    {
        count++;
        index += pattern.Length;
    }

    return count;
}

static void RunOnStaThread(Action action)
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw failure;
    }
}

static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
        }
    }

    public static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException($"Expected {expected.Count} items, got {actual.Count}.");
        }

        for (int index = 0; index < expected.Count; index++)
        {
            Equal(expected[index], actual[index], $"Item {index} should match.");
        }
    }

    public static void Contains<T>(IReadOnlyList<T> items, Func<T, bool> predicate)
    {
        if (!items.Any(predicate))
        {
            throw new InvalidOperationException("Expected collection to contain a matching item.");
        }
    }

    public static void Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"{message} Expected {typeof(TException).Name}, got {exception.GetType().Name}.", exception);
        }

        throw new InvalidOperationException($"{message} Expected {typeof(TException).Name}.");
    }
}

sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly string _responseBody;
    private readonly System.Net.HttpStatusCode _statusCode;

    public FakeHttpHandler(string responseBody, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody)
        });
    }
}

sealed class TestSecretProtector : ICloudAiSecretProtector
{
    public byte[] Protect(byte[] plaintext, string entropy)
    {
        return Transform(plaintext, entropy);
    }

    public byte[] Unprotect(byte[] protectedData, string entropy)
    {
        return Transform(protectedData, entropy);
    }

    private static byte[] Transform(byte[] input, string entropy)
    {
        byte mask = (byte)(entropy.Sum(character => character) % 251);
        byte[] output = new byte[input.Length];
        for (int index = 0; index < input.Length; index++)
        {
            output[index] = (byte)(input[input.Length - index - 1] ^ mask);
        }

        return output;
    }
}

sealed record GoldenSample(string Name, string ImagePath, string SlicePath, BitmapSource Image, TwentyFiveSliceData Expected);
