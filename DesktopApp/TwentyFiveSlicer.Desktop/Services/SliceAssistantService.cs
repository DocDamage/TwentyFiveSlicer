using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class SliceAssistantService
{
    public SliceAssistantResult Apply(string prompt, TwentyFiveSliceData current)
    {
        string normalizedPrompt = prompt.Trim().ToLowerInvariant();
        double[] vertical = TwentyFiveSliceData.NormalizeAxis(current.VerticalBorders);
        double[] horizontal = TwentyFiveSliceData.NormalizeAxis(current.HorizontalBorders);
        bool applied = false;
        var messages = new List<string>();

        if (normalizedPrompt.Contains("thinner corner") || normalizedPrompt.Contains("thin corner"))
        {
            vertical[0] = 8d;
            vertical[3] = 92d;
            horizontal[0] = 8d;
            horizontal[3] = 92d;
            applied = true;
            messages.Add("Reduced the outer fixed corner bands.");
        }

        if (normalizedPrompt.Contains("top match bottom") || normalizedPrompt.Contains("top border match"))
        {
            double bottomBand = 100d - horizontal[3];
            horizontal[0] = bottomBand;
            applied = true;
            messages.Add("Matched the top fixed band to the bottom fixed band.");
        }

        if (normalizedPrompt.Contains("left match right") || normalizedPrompt.Contains("left border match"))
        {
            double rightBand = 100d - vertical[3];
            vertical[0] = rightBand;
            applied = true;
            messages.Add("Matched the left fixed band to the right fixed band.");
        }

        if (normalizedPrompt.Contains("center stretch") || normalizedPrompt.Contains("center the stretch"))
        {
            vertical[1] = 40d;
            vertical[2] = 60d;
            horizontal[1] = 40d;
            horizontal[2] = 60d;
            applied = true;
            messages.Add("Centered the main stretch regions.");
        }

        var data = new TwentyFiveSliceData(vertical, horizontal);
        string message = applied
            ? string.Join(" ", messages)
            : "I did not recognize a local command. Try thinner corners, top match bottom, left match right, or center stretch.";

        return new SliceAssistantResult(data, applied, message);
    }
}
