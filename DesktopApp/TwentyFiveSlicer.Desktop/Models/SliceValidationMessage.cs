namespace TwentyFiveSlicer.Desktop.Models;

public enum SliceValidationSeverity
{
    Info,
    Warning
}

public sealed record SliceValidationMessage(SliceValidationSeverity Severity, string Text);
