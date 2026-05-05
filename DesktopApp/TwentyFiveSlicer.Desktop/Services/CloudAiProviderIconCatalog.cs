namespace TwentyFiveSlicer.Desktop.Services;

public static class CloudAiProviderIconCatalog
{
    private static readonly Dictionary<string, string> IconPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["openai"] = "Assets/Icons/Tabler/message-chatbot.svg",
        ["anthropic"] = "Assets/Icons/SimpleIcons/anthropic.svg",
        ["gemini"] = "Assets/Icons/SimpleIcons/google.svg",
        ["kimi"] = "Assets/Icons/Tabler/sparkles.svg",
        ["deepseek"] = "Assets/Icons/Tabler/search.svg",
        ["glm"] = "Assets/Icons/Tabler/brain.svg",
        ["minimax"] = "Assets/Icons/Tabler/arrows-minimize.svg",
        ["mistral"] = "Assets/Icons/Tabler/wind.svg",
        ["cohere"] = "Assets/Icons/Tabler/circles-relation.svg",
        ["groq"] = "Assets/Icons/Tabler/bolt.svg",
        ["xai"] = "Assets/Icons/Tabler/x.svg",
        ["perplexity"] = "Assets/Icons/Tabler/question-mark.svg",
        ["openai-compatible"] = "Assets/Icons/Tabler/api.svg"
    };

    public static string GetIconPath(string providerId)
    {
        return IconPaths.TryGetValue(providerId, out string? path)
            ? path
            : "Assets/Icons/Tabler/api.svg";
    }
}
