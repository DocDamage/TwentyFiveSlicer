using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class CloudAiProviderCatalog
{
    private static readonly CloudAiProviderDescriptor[] Providers =
    [
        new(
            "openai",
            "OpenAI / ChatGPT",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.openai.com/v1/chat/completions",
            "gpt-5.1",
            ["gpt-5.1", "gpt-5.1-mini", "gpt-4.1", "gpt-4o"],
            "OPENAI_API_KEY",
            true,
            ["chatgpt", "gpt"]),
        new(
            "anthropic",
            "Anthropic Claude",
            CloudAiApiKind.AnthropicMessages,
            "https://api.anthropic.com/v1/messages",
            "claude-sonnet-4-5",
            ["claude-sonnet-4-5", "claude-opus-4-1", "claude-haiku-4-5"],
            "ANTHROPIC_API_KEY",
            true,
            ["claude"]),
        new(
            "gemini",
            "Google Gemini",
            CloudAiApiKind.GeminiGenerateContent,
            "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
            "gemini-3-pro",
            ["gemini-3-pro", "gemini-2.5-pro", "gemini-2.5-flash"],
            "GEMINI_API_KEY",
            true,
            ["google"]),
        new(
            "kimi",
            "Moonshot Kimi",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.moonshot.ai/v1/chat/completions",
            "kimi-k2.5",
            ["kimi-k2.5", "kimi-k2-turbo-preview"],
            "MOONSHOT_API_KEY",
            true,
            ["moonshot"]),
        new(
            "deepseek",
            "DeepSeek",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.deepseek.com/chat/completions",
            "deepseek-chat",
            ["deepseek-chat", "deepseek-reasoner"],
            "DEEPSEEK_API_KEY",
            false,
            ["deepseel"]),
        new(
            "glm",
            "Zhipu GLM",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://open.bigmodel.cn/api/paas/v4/chat/completions",
            "glm-4.7",
            ["glm-4.7", "glm-4.6", "glm-4.5v"],
            "ZHIPU_API_KEY",
            true,
            ["zhipu", "bigmodel"]),
        new(
            "minimax",
            "MiniMax",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.minimax.io/v1/text/chatcompletion_v2",
            "MiniMax-M1",
            ["MiniMax-M1", "abab6.5s-chat"],
            "MINIMAX_API_KEY",
            false,
            []),
        new(
            "mistral",
            "Mistral",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.mistral.ai/v1/chat/completions",
            "mistral-large-latest",
            ["mistral-large-latest", "mistral-medium-latest", "mistral-small-latest"],
            "MISTRAL_API_KEY",
            true,
            []),
        new(
            "cohere",
            "Cohere",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.cohere.com/compatibility/v1/chat/completions",
            "command-a-vision-07-2025",
            ["command-a-vision-07-2025", "command-r-plus", "command-r"],
            "COHERE_API_KEY",
            true,
            []),
        new(
            "groq",
            "Groq",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.groq.com/openai/v1/chat/completions",
            "llama-3.3-70b-versatile",
            ["llama-3.3-70b-versatile", "openai/gpt-oss-120b", "moonshotai/kimi-k2-instruct"],
            "GROQ_API_KEY",
            false,
            []),
        new(
            "xai",
            "xAI Grok",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.x.ai/v1/chat/completions",
            "grok-4",
            ["grok-4", "grok-3", "grok-3-mini"],
            "XAI_API_KEY",
            true,
            ["grok"]),
        new(
            "perplexity",
            "Perplexity",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://api.perplexity.ai/chat/completions",
            "sonar-pro",
            ["sonar-pro", "sonar", "sonar-reasoning-pro"],
            "PERPLEXITY_API_KEY",
            false,
            []),
        new(
            "openai-compatible",
            "Custom OpenAI-Compatible",
            CloudAiApiKind.OpenAiCompatibleChat,
            "https://example.com/v1/chat/completions",
            "custom-model",
            ["custom-model"],
            "CUSTOM_AI_API_KEY",
            true,
            ["custom"])
    ];

    public static IReadOnlyList<CloudAiProviderDescriptor> GetAll()
    {
        return Providers;
    }

    public static CloudAiProviderDescriptor? Find(string providerOrAlias)
    {
        return Providers.FirstOrDefault(provider =>
            string.Equals(provider.Id, providerOrAlias, StringComparison.OrdinalIgnoreCase) ||
            provider.Aliases.Any(alias => string.Equals(alias, providerOrAlias, StringComparison.OrdinalIgnoreCase)));
    }
}
