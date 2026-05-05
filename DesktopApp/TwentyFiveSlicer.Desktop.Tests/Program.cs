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
    ("SliceAssistant applies natural language edits", SliceAssistantAppliesNaturalLanguageEdits)
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
