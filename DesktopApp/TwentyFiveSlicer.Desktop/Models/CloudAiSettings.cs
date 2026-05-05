namespace TwentyFiveSlicer.Desktop.Models;

public sealed class CloudAiSettings
{
    public bool Enabled { get; set; }

    public string ProviderId { get; set; } = "local";

    public string? ModelId { get; set; }

    public string? EndpointOverride { get; set; }

    public string? ApiKeyEnvironmentVariable { get; set; }

    public bool UseSecureApiKeyStore { get; set; } = true;
}
