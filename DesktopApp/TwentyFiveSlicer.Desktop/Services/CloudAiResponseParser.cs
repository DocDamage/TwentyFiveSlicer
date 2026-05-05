using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class CloudAiResponseParser
{
    public static string Parse(CloudAiProviderDescriptor provider, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return provider.ApiKind switch
        {
            CloudAiApiKind.AnthropicMessages => ParseAnthropic(document.RootElement),
            CloudAiApiKind.GeminiGenerateContent => ParseGemini(document.RootElement),
            _ => ParseOpenAiCompatible(document.RootElement)
        };
    }

    private static string ParseOpenAiCompatible(JsonElement root)
    {
        if (root.TryGetProperty("choices", out JsonElement choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            JsonElement choice = choices[0];
            if (choice.TryGetProperty("message", out JsonElement message) &&
                message.TryGetProperty("content", out JsonElement content))
            {
                return content.GetString() ?? string.Empty;
            }

            if (choice.TryGetProperty("text", out JsonElement text))
            {
                return text.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string ParseAnthropic(JsonElement root)
    {
        if (!root.TryGetProperty("content", out JsonElement content) ||
            content.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (JsonElement part in content.EnumerateArray())
        {
            if (part.TryGetProperty("text", out JsonElement text))
            {
                return text.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string ParseGemini(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out JsonElement candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        JsonElement candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out JsonElement content) ||
            !content.TryGetProperty("parts", out JsonElement parts) ||
            parts.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (JsonElement part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out JsonElement text))
            {
                return text.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
