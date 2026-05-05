namespace TwentyFiveSlicer.Desktop.Models;

public sealed record BatchPreviewResult(PreviewTarget Target, IReadOnlyList<SliceValidationMessage> Messages)
{
    public bool HasWarnings => Messages.Any(message => message.Severity == SliceValidationSeverity.Warning);
}
