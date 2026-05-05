using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Controls;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

var tests = new (string Name, Action Test)[]
{
    ("SliceHistory restores undo and redo states", SliceHistoryRestoresUndoAndRedoStates),
    ("Layout calculator matches Unity fixed stretch distribution", LayoutCalculatorMatchesUnityFixedStretchDistribution),
    ("Layout calculator scales fixed regions when target is too small", LayoutCalculatorScalesFixedRegionsWhenTargetIsTooSmall),
    ("Layout calculator flips source regions without moving destinations", LayoutCalculatorFlipsSourceRegionsWithoutMovingDestinations),
    ("Preview control renders export bitmap", PreviewControlRendersExportBitmap),
    ("Preview control export bitmap encodes as PNG", PreviewControlExportBitmapEncodesAsPng),
    ("Slice data JSON stays Unity compatible", SliceDataJsonStaysUnityCompatible),
    ("SliceValidation warns when fixed columns exceed target width", SliceValidationWarnsWhenFixedColumnsExceedTargetWidth),
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
    ("CloudAiAdviceParser extracts JSON border suggestions", CloudAiAdviceParserExtractsJsonBorderSuggestions),
    ("CloudAiAdviceParser extracts inline border suggestions", CloudAiAdviceParserExtractsInlineBorderSuggestions),
    ("CloudAiAdviceParser extracts fenced snake case JSON suggestions", CloudAiAdviceParserExtractsFencedSnakeCaseJsonSuggestions),
    ("CloudAiAdviceParser extracts spaced percentage labels", CloudAiAdviceParserExtractsSpacedPercentageLabels)
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

static void SliceDataJsonStaysUnityCompatible()
{
    var data = new TwentyFiveSliceData([12d, 42d, 58d, 88d], [10d, 40d, 60d, 90d]);

    string json = JsonSerializer.Serialize(data);
    TwentyFiveSliceData? loaded = JsonSerializer.Deserialize<TwentyFiveSliceData>(json);

    Assert.True(json.Contains("\"verticalBorders\""), "Standalone JSON should use the Unity runtime verticalBorders field name.");
    Assert.True(json.Contains("\"horizontalBorders\""), "Standalone JSON should use the Unity runtime horizontalBorders field name.");
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
            KeepAspect = true,
            DebugOverlay = true,
            ExportDebug = true,
            SourceGuides = true,
            FlipX = true,
            FlipY = false,
            AssistantOutput = "Best fit: Button"
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
        Assert.Equal(true, session.KeepAspect, "Toggle state should round trip.");
        Assert.Equal(88d, session.SliceData.VerticalBorders[3], "Slice data should round trip.");
        Assert.Equal("Best fit: Button", session.AssistantOutput, "Assistant output should round trip.");
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
