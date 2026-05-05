using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class CloudAiRequestBuilder
{
    private const string SystemPrompt = "You are helping analyze 25-slice UI artwork. Return concise, practical slice-border advice.";

    public static HttpRequestMessage BuildAnalysisRequest(CloudAiProviderDescriptor provider, CloudAiSettings settings, string prompt)
    {
        string apiKey = ResolveApiKey(provider, settings);
        string model = string.IsNullOrWhiteSpace(settings.ModelId) ? provider.DefaultModel : settings.ModelId!;

        return provider.ApiKind switch
        {
            CloudAiApiKind.AnthropicMessages => BuildAnthropicRequest(provider, settings, model, apiKey, prompt),
            CloudAiApiKind.GeminiGenerateContent => BuildGeminiRequest(provider, settings, model, apiKey, prompt),
            _ => BuildOpenAiCompatibleRequest(provider, settings, model, apiKey, prompt)
        };
    }

    private static HttpRequestMessage BuildOpenAiCompatibleRequest(CloudAiProviderDescriptor provider, CloudAiSettings settings, string model, string apiKey, string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GetEndpoint(provider, settings, model));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent(new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = prompt }
            }
        });
        return request;
    }

    private static HttpRequestMessage BuildAnthropicRequest(CloudAiProviderDescriptor provider, CloudAiSettings settings, string model, string apiKey, string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GetEndpoint(provider, settings, model));
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent(new
        {
            model,
            max_tokens = 1024,
            system = SystemPrompt,
            messages = new object[]
            {
                new { role = "user", content = prompt }
            }
        });
        return request;
    }

    private static HttpRequestMessage BuildGeminiRequest(CloudAiProviderDescriptor provider, CloudAiSettings settings, string model, string apiKey, string prompt)
    {
        string endpoint = GetEndpoint(provider, settings, model);
        string separator = endpoint.Contains('?') ? "&" : "?";
        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}{separator}key={Uri.EscapeDataString(apiKey)}");
        request.Content = JsonContent(new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = SystemPrompt } }
            },
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            }
        });
        return request;
    }

    private static string GetEndpoint(CloudAiProviderDescriptor provider, CloudAiSettings settings, string model)
    {
        string endpoint = string.IsNullOrWhiteSpace(settings.EndpointOverride) ? provider.Endpoint : settings.EndpointOverride!;
        return endpoint.Replace("{model}", Uri.EscapeDataString(model), StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveApiKey(CloudAiProviderDescriptor provider, CloudAiSettings settings)
    {
        string envVar = string.IsNullOrWhiteSpace(settings.ApiKeyEnvironmentVariable)
            ? provider.ApiKeyEnvironmentVariable
            : settings.ApiKeyEnvironmentVariable!;
        string? apiKey = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"Environment variable {envVar} is not set.");
        }

        return apiKey;
    }

    private static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }
}
