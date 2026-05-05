using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class CloudAiRequestBuilder
{
    private const string SystemPrompt = "You are helping analyze 25-slice UI artwork. Return concise, practical slice-border advice.";

    public static HttpRequestMessage BuildAnalysisRequest(
        CloudAiProviderDescriptor provider,
        CloudAiSettings settings,
        string prompt,
        CloudAiImageInput? image = null,
        CloudAiSecretStore? secretStore = null)
    {
        string apiKey = CloudAiApiKeyResolver.Resolve(provider, settings, secretStore);
        string model = string.IsNullOrWhiteSpace(settings.ModelId) ? provider.DefaultModel : settings.ModelId!;
        CloudAiImageInput? supportedImage = provider.SupportsVision ? image : null;

        return provider.ApiKind switch
        {
            CloudAiApiKind.AnthropicMessages => BuildAnthropicRequest(provider, settings, model, apiKey, prompt, supportedImage),
            CloudAiApiKind.GeminiGenerateContent => BuildGeminiRequest(provider, settings, model, apiKey, prompt, supportedImage),
            _ => BuildOpenAiCompatibleRequest(provider, settings, model, apiKey, prompt, supportedImage)
        };
    }

    private static HttpRequestMessage BuildOpenAiCompatibleRequest(CloudAiProviderDescriptor provider, CloudAiSettings settings, string model, string apiKey, string prompt, CloudAiImageInput? image)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GetEndpoint(provider, settings, model));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        object userContent = image is null
            ? prompt
            : new object[]
            {
                new { type = "text", text = prompt },
                new { type = "image_url", image_url = new { url = $"data:{image.MimeType};base64,{image.Base64Data}" } }
            };

        request.Content = JsonContent(new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userContent }
            }
        });
        return request;
    }

    private static HttpRequestMessage BuildAnthropicRequest(CloudAiProviderDescriptor provider, CloudAiSettings settings, string model, string apiKey, string prompt, CloudAiImageInput? image)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GetEndpoint(provider, settings, model));
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        object userContent = image is null
            ? prompt
            : new object[]
            {
                new { type = "text", text = prompt },
                new { type = "image", source = new { type = "base64", media_type = image.MimeType, data = image.Base64Data } }
            };

        request.Content = JsonContent(new
        {
            model,
            max_tokens = 1024,
            system = SystemPrompt,
            messages = new object[]
            {
                new { role = "user", content = userContent }
            }
        });
        return request;
    }

    private static HttpRequestMessage BuildGeminiRequest(CloudAiProviderDescriptor provider, CloudAiSettings settings, string model, string apiKey, string prompt, CloudAiImageInput? image)
    {
        string endpoint = GetEndpoint(provider, settings, model);
        string separator = endpoint.Contains('?') ? "&" : "?";
        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}{separator}key={Uri.EscapeDataString(apiKey)}");
        object[] parts = image is null
            ? [new { text = prompt }]
            : [new { text = prompt }, new { inlineData = new { mimeType = image.MimeType, data = image.Base64Data } }];

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
                    parts
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

    private static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }
}
