using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public static class CloudAiApiKeyResolver
{
    public static string Resolve(CloudAiProviderDescriptor provider, CloudAiSettings settings, CloudAiSecretStore? secretStore = null)
    {
        string envVar = CloudAiSecretStore.ResolveEnvironmentVariable(provider, settings);
        string? apiKey = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey;
        }

        if (settings.UseSecureApiKeyStore && secretStore is not null && secretStore.TryGetSecret(provider, settings, out string storedApiKey))
        {
            return storedApiKey;
        }

        string secureStoreHint = settings.UseSecureApiKeyStore
            ? " or save a secure key in the app"
            : string.Empty;
        throw new InvalidOperationException($"Environment variable {envVar} is not set{secureStoreHint}.");
    }
}
