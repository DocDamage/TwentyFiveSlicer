using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

var tests = new (string Name, Action Test)[]
{
    ("SliceHistory restores undo and redo states", SliceHistoryRestoresUndoAndRedoStates),
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
    ("SliceAssistant aggregate candidates include target coverage", SliceAssistantAggregateCandidatesIncludeTargetCoverage)
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
