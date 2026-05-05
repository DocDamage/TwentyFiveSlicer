using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class PreviewTargetCatalog
{
    public static PreviewTarget[] GetCommonTargets()
    {
        return
        [
            new("128 square", 128d, 128d),
            new("256 wide", 256d, 96d),
            new("512 square", 512d, 512d),
            new("1080p panel", 1920d, 1080d)
        ];
    }
}
