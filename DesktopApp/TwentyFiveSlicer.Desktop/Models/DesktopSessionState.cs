namespace TwentyFiveSlicer.Desktop.Models;

public sealed class DesktopSessionState
{
    public string? ImagePath { get; set; }

    public TwentyFiveSliceData SliceData { get; set; } = TwentyFiveSliceData.CreateDefault();

    public double TargetWidth { get; set; } = 640d;

    public double TargetHeight { get; set; } = 360d;

    public bool KeepAspect { get; set; }

    public bool DebugOverlay { get; set; }

    public bool ExportDebug { get; set; }

    public bool SourceGuides { get; set; }

    public bool FlipX { get; set; }

    public bool FlipY { get; set; }

    public string? AssistantOutput { get; set; }
}
