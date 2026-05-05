using System.Text.Json;
using System.Windows.Media.Imaging;
using TwentyFiveSlicer.Desktop.Models;
using TwentyFiveSlicer.Desktop.Services;

if (args.Length == 0 || IsHelp(args[0]))
{
    PrintHelp();
    return 0;
}

try
{
    string command = args[0].ToLowerInvariant();
    Dictionary<string, string> options = ParseOptions(args.Skip(1).ToArray());
    string output = command switch
    {
        "providers" => IdeAssistantBridge.ListProvidersJson(),
        "analyze" => IdeAssistantBridge.AnalyzeLocalJson(ReadInput(options)),
        "apply" => IdeAssistantBridge.ApplyPromptJson(ReadInput(options), GetOption(options, "prompt", "make practical improvements")),
        "prompt" => IdeAssistantBridge.BuildPromptJson(ReadInput(options), GetOption(options, "prompt", "analyze and recommend improvements")),
        "review" => RunReview(options),
        "cloud" => await RunCloudAsync(options),
        "key-status" => RunKeyStatus(options),
        "key-set" => RunKeySet(options),
        "key-delete" => RunKeyDelete(options),
        _ => throw new InvalidOperationException($"Unknown command '{args[0]}'.")
    };

    Console.WriteLine(output);
    return 0;
}
catch (Exception exception)
{
    Console.WriteLine(IdeAssistantBridge.ErrorJson(exception.Message, exception.GetType().Name));
    return 1;
}

static async Task<string> RunCloudAsync(Dictionary<string, string> options)
{
    IdeAssistantInput input = ReadInput(options);
    string provider = GetRequiredOption(options, "provider");
    string prompt = GetOption(options, "prompt", "analyze and recommend improvements");
    CloudAiImageInput? image = options.TryGetValue("image", out string? imagePath)
        ? ReadImage(imagePath)
        : null;

    return await IdeAssistantBridge.AskCloudJsonAsync(
        input,
        provider,
        prompt,
        GetNullableOption(options, "model"),
        GetNullableOption(options, "endpoint"),
        GetNullableOption(options, "api-key-env"),
        image);
}

static string RunReview(Dictionary<string, string> options)
{
    IdeAssistantInput input = ReadInput(options);
    TwentyFiveSliceData proposed = ReadSliceData(GetRequiredOption(options, "proposed"));
    return options.TryGetValue("image", out string? imagePath)
        ? IdeAssistantBridge.ReviewSuggestionJson(input, proposed, ReadBitmap(imagePath))
        : IdeAssistantBridge.ReviewSuggestionJson(input, proposed);
}

static string RunKeyStatus(Dictionary<string, string> options)
{
    CloudAiProviderDescriptor provider = ReadProvider(options);
    CloudAiSettings settings = ReadCloudKeySettings(provider, options);
    var store = CloudAiSecretStore.CreateDefault();
    return JsonSerializer.Serialize(new
    {
        schema = "twenty-five-slicer.ai.key-status.v1",
        provider = provider.Id,
        apiKeyEnvironmentVariable = CloudAiSecretStore.ResolveEnvironmentVariable(provider, settings),
        environmentVariableSet = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CloudAiSecretStore.ResolveEnvironmentVariable(provider, settings))),
        secureKeyStored = store.HasSecret(provider, settings),
        status = store.DescribeStatus(provider, settings)
    });
}

static string RunKeySet(Dictionary<string, string> options)
{
    CloudAiProviderDescriptor provider = ReadProvider(options);
    CloudAiSettings settings = ReadCloudKeySettings(provider, options);
    string apiKey = Console.In.ReadToEnd().Trim();
    CloudAiSecretStore.CreateDefault().SaveSecret(provider, settings, apiKey);
    return JsonSerializer.Serialize(new
    {
        schema = "twenty-five-slicer.ai.key-set.v1",
        provider = provider.Id,
        apiKeyEnvironmentVariable = CloudAiSecretStore.ResolveEnvironmentVariable(provider, settings),
        saved = true
    });
}

static string RunKeyDelete(Dictionary<string, string> options)
{
    CloudAiProviderDescriptor provider = ReadProvider(options);
    CloudAiSettings settings = ReadCloudKeySettings(provider, options);
    bool deleted = CloudAiSecretStore.CreateDefault().DeleteSecret(provider, settings);
    return JsonSerializer.Serialize(new
    {
        schema = "twenty-five-slicer.ai.key-delete.v1",
        provider = provider.Id,
        apiKeyEnvironmentVariable = CloudAiSecretStore.ResolveEnvironmentVariable(provider, settings),
        deleted
    });
}

static CloudAiProviderDescriptor ReadProvider(Dictionary<string, string> options)
{
    string providerId = GetRequiredOption(options, "provider");
    return CloudAiProviderCatalog.Find(providerId)
        ?? throw new InvalidOperationException($"Unknown cloud AI provider '{providerId}'. Run providers to list supported IDs and aliases.");
}

static CloudAiSettings ReadCloudKeySettings(CloudAiProviderDescriptor provider, Dictionary<string, string> options)
{
    return new CloudAiSettings
    {
        Enabled = true,
        ProviderId = provider.Id,
        ApiKeyEnvironmentVariable = GetNullableOption(options, "api-key-env"),
        UseSecureApiKeyStore = true
    };
}

static IdeAssistantInput ReadInput(Dictionary<string, string> options)
{
    string slicePath = GetRequiredOption(options, "slice");
    TwentyFiveSliceData sliceData = ReadSliceData(slicePath);

    return new IdeAssistantInput(
        sliceData,
        GetDoubleOption(options, "source-width", 100d),
        GetDoubleOption(options, "source-height", 100d),
        GetDoubleOption(options, "target-width", 640d),
        GetDoubleOption(options, "target-height", 360d));
}

static TwentyFiveSliceData ReadSliceData(string slicePath)
{
    string json = slicePath == "-"
        ? Console.In.ReadToEnd()
        : File.ReadAllText(slicePath);
    return JsonSerializer.Deserialize<TwentyFiveSliceData>(json)
        ?? throw new InvalidDataException("Slice JSON did not contain valid 25-slice data.");
}

static CloudAiImageInput ReadImage(string imagePath)
{
    byte[] bytes = File.ReadAllBytes(imagePath);
    string mimeType = Path.GetExtension(imagePath).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "image/png"
    };

    return new CloudAiImageInput(mimeType, Convert.ToBase64String(bytes));
}

static BitmapSource ReadBitmap(string imagePath)
{
    var image = new BitmapImage();
    image.BeginInit();
    image.CacheOption = BitmapCacheOption.OnLoad;
    image.UriSource = new Uri(Path.GetFullPath(imagePath));
    image.EndInit();
    image.Freeze();
    return image;
}

static Dictionary<string, string> ParseOptions(string[] tokens)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int index = 0; index < tokens.Length; index++)
    {
        string token = tokens[index];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected argument '{token}'. Options must use --name value.");
        }

        string name = token[2..];
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Empty option name.");
        }

        if (index + 1 >= tokens.Length || tokens[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Missing value for --{name}.");
        }

        options[name] = tokens[++index];
    }

    return options;
}

static string GetRequiredOption(Dictionary<string, string> options, string name)
{
    if (!options.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required option --{name}.");
    }

    return value;
}

static string GetOption(Dictionary<string, string> options, string name, string fallback)
{
    return options.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : fallback;
}

static string? GetNullableOption(Dictionary<string, string> options, string name)
{
    return options.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : null;
}

static double GetDoubleOption(Dictionary<string, string> options, string name, double fallback)
{
    if (!options.TryGetValue(name, out string? value))
    {
        return fallback;
    }

    return double.TryParse(value, out double parsed)
        ? parsed
        : throw new InvalidOperationException($"Option --{name} must be a number.");
}

static bool IsHelp(string value)
{
    return value is "-h" or "--help" or "help";
}

static void PrintHelp()
{
    Console.WriteLine("""
    TwentyFiveSlicer AI CLI

    Commands:
      providers
      analyze --slice slice.json --source-width 100 --source-height 100 --target-width 640 --target-height 360
      apply --slice slice.json --prompt "make this a button"
      prompt --slice slice.json --prompt "recommend borders"
      review --slice current.json --proposed proposed.json --source-width 100 --source-height 100 --target-width 640 --target-height 360 [--image source.png]
      cloud --slice slice.json --provider openai --prompt "recommend borders" [--model gpt-5.1] [--api-key-env OPENAI_API_KEY] [--endpoint URL] [--image image.png]
      key-status --provider openai [--api-key-env OPENAI_API_KEY]
      key-set --provider openai [--api-key-env OPENAI_API_KEY] < api-key.txt
      key-delete --provider openai [--api-key-env OPENAI_API_KEY]

    Use --slice - to read Unity-compatible slice JSON from stdin.
    Output is JSON so IDE agents such as Codex can parse it directly, including errors.
    key-set reads the API key from stdin so the secret does not appear in command history.
    """);
}
