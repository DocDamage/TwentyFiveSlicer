namespace TwentyFiveSlicer.Desktop.Models;

public sealed record CloudAiResult(bool Success, string Advice, string ErrorMessage)
{
    public static CloudAiResult Succeeded(string advice)
    {
        return new CloudAiResult(true, advice, string.Empty);
    }

    public static CloudAiResult Failed(string errorMessage)
    {
        return new CloudAiResult(false, string.Empty, errorMessage);
    }
}
