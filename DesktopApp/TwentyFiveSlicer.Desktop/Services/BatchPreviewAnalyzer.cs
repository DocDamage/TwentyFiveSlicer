using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class BatchPreviewAnalyzer
{
    public static IReadOnlyList<BatchPreviewResult> Analyze(
        double sourceWidth,
        double sourceHeight,
        TwentyFiveSliceData sliceData,
        IEnumerable<PreviewTarget> targets)
    {
        return targets
            .Select(target => new BatchPreviewResult(
                target,
                SliceValidationService.Validate(sourceWidth, sourceHeight, target.Width, target.Height, sliceData)))
            .ToArray();
    }
}
