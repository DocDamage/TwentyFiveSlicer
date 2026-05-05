namespace TwentyFiveSlicer.Desktop.Models;

public sealed class DesktopAppState
{
    public List<string> RecentFiles { get; set; } = [];

    public Dictionary<string, TwentyFiveSliceData> UserPresets { get; set; } = [];

    public DesktopSessionState? LastSession { get; set; }
}
