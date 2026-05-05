using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class IdeAssistantBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ListProvidersJson()
    {
        var payload = new
        {
            schema = "twenty-five-slicer.ai.providers.v1",
            providers = CloudAiProviderCatalog.GetAll().Select(provider => new
            {
                provider.Id,
                provider.DisplayName,
                apiKind = provider.ApiKind.ToString(),
                provider.Endpoint,
                provider.DefaultModel,
                provider.Models,
                provider.ApiKeyEnvironmentVariable,
                provider.SupportsVision,
                provider.Aliases
            })
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string AnalyzeLocalJson(IdeAssistantInput input)
    {
        var assistant = new SliceAssistantService();
        SliceAssistantAnalysis analysis = assistant.AnalyzeDetailed(
            input.SourceWidth,
            input.SourceHeight,
            input.TargetWidth,
            input.TargetHeight,
            input.SliceData);

        var payload = new
        {
            schema = "twenty-five-slicer.ai.analysis.v1",
            summary = analysis.Summary,
            assetKind = analysis.AssetKind.ToString(),
            confidence = analysis.Confidence,
            sliceData = input.SliceData,
            observations = analysis.Observations,
            validationMessages = analysis.ValidationMessages.Select(message => new
            {
                severity = message.Severity.ToString(),
                message.Text
            }),
            recommendedActions = analysis.RecommendedActions
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string ApplyPromptJson(IdeAssistantInput input, string prompt)
    {
        var assistant = new SliceAssistantService();
        SliceAssistantResult result = assistant.Apply(prompt, input.SliceData);

        var payload = new
        {
            schema = "twenty-five-slicer.ai.apply.v1",
            result.Applied,
            result.Message,
            sliceData = result.SliceData
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static async Task<string> AskCloudJsonAsync(
        IdeAssistantInput input,
        string providerId,
        string prompt,
        string? modelId = null,
        string? endpointOverride = null,
        string? apiKeyEnvironmentVariable = null,
        CloudAiImageInput? image = null,
        CancellationToken cancellationToken = default)
    {
        CloudAiProviderDescriptor provider = CloudAiProviderCatalog.Find(providerId)
            ?? throw new InvalidOperationException($"Unknown cloud AI provider '{providerId}'. Run providers to list supported IDs and aliases.");

        string fullPrompt = BuildPrompt(input, prompt);
        var settings = new CloudAiSettings
        {
            Enabled = true,
            ProviderId = provider.Id,
            ModelId = string.IsNullOrWhiteSpace(modelId) ? provider.DefaultModel : modelId,
            EndpointOverride = endpointOverride,
            ApiKeyEnvironmentVariable = apiKeyEnvironmentVariable
        };

        using var client = new CloudAiClient();
        CloudAiResult result = await client.AskAsync(provider, settings, fullPrompt, image, cancellationToken).ConfigureAwait(false);
        TwentyFiveSliceData? parsedSliceData = null;
        if (result.Success)
        {
            CloudAiAdviceParser.TryParseSliceData(result.Advice, out parsedSliceData);
        }

        var payload = new
        {
            schema = "twenty-five-slicer.ai.cloud.v1",
            provider = provider.Id,
            model = settings.ModelId,
            result.Success,
            advice = result.Advice,
            result.ErrorMessage,
            parsedSliceData
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildPrompt(IdeAssistantInput input, string userPrompt)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "Analyze this 25-slice UI asset configuration and recommend border edits.",
            $"Source image: {input.SourceWidth:0.#} x {input.SourceHeight:0.#}px",
            $"Target preview: {input.TargetWidth:0.#} x {input.TargetHeight:0.#}px",
            $"Vertical borders: {string.Join(", ", input.SliceData.VerticalBorders.Select(value => $"{value:0.#}%"))}",
            $"Horizontal borders: {string.Join(", ", input.SliceData.HorizontalBorders.Select(value => $"{value:0.#}%"))}",
            "Return concise advice. If border edits are recommended, include exact JSON with verticalBorders and horizontalBorders arrays of four percentages each.",
            string.IsNullOrWhiteSpace(userPrompt) ? "User request: analyze and recommend improvements." : $"User request: {userPrompt.Trim()}"
        });
    }
}
