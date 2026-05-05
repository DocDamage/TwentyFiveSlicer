namespace TwentyFiveSlicer.Desktop.Services;

public static class CloudAiProviderLabelFormatter
{
    public static string Format(string displayName)
    {
        return displayName switch
        {
            "OpenAI / ChatGPT" => "ChatGPT",
            "Anthropic Claude" => "Claude",
            "Google Gemini" => "Gemini",
            _ => displayName
        };
    }
}
