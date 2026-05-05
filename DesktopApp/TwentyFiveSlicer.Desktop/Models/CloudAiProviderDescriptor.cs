namespace TwentyFiveSlicer.Desktop.Models;

public sealed record CloudAiProviderDescriptor(
    string Id,
    string DisplayName,
    CloudAiApiKind ApiKind,
    string Endpoint,
    string DefaultModel,
    IReadOnlyList<string> Models,
    string ApiKeyEnvironmentVariable,
    bool SupportsVision,
    IReadOnlyList<string> Aliases);
