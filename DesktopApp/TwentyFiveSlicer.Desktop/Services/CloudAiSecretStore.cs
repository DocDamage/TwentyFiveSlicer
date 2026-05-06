using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class CloudAiSecretStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly ICloudAiSecretProtector _protector;

    public CloudAiSecretStore(string filePath, ICloudAiSecretProtector? protector = null)
    {
        _filePath = filePath;
        _protector = protector ?? new WindowsCurrentUserSecretProtector();
    }

    public static CloudAiSecretStore CreateDefault()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TwentyFiveSlicer",
            "cloud-ai-secrets.v1.json");
        return new CloudAiSecretStore(path);
    }

    public bool HasSecret(CloudAiProviderDescriptor provider, CloudAiSettings settings)
    {
        string keyId = BuildKeyId(provider, settings);
        CloudAiSecretFile file = LoadFile();
        return file.Secrets.ContainsKey(keyId);
    }

    public void SaveSecret(CloudAiProviderDescriptor provider, CloudAiSettings settings, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key cannot be empty.");
        }

        string keyId = BuildKeyId(provider, settings);
        string entropy = BuildEntropy(provider, settings);
        byte[] encrypted = _protector.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), entropy);
        CloudAiSecretFile file = LoadFile();
        file.Secrets[keyId] = new CloudAiSecretRecord(
            provider.Id,
            ResolveEnvironmentVariable(provider, settings),
            Convert.ToBase64String(encrypted),
            DateTimeOffset.UtcNow);
        SaveFile(file);
    }

    public bool TryGetSecret(CloudAiProviderDescriptor provider, CloudAiSettings settings, out string apiKey)
    {
        string keyId = BuildKeyId(provider, settings);
        CloudAiSecretFile file = LoadFile();
        if (!file.Secrets.TryGetValue(keyId, out CloudAiSecretRecord? record) || string.IsNullOrWhiteSpace(record.ProtectedApiKey))
        {
            apiKey = string.Empty;
            return false;
        }

        try
        {
            byte[] decrypted = _protector.Unprotect(Convert.FromBase64String(record.ProtectedApiKey), BuildEntropy(provider, settings));
            apiKey = Encoding.UTF8.GetString(decrypted);
            return !string.IsNullOrWhiteSpace(apiKey);
        }
        catch (CryptographicException)
        {
            apiKey = string.Empty;
            return false;
        }
        catch (FormatException)
        {
            apiKey = string.Empty;
            return false;
        }
    }

    public bool DeleteSecret(CloudAiProviderDescriptor provider, CloudAiSettings settings)
    {
        string keyId = BuildKeyId(provider, settings);
        CloudAiSecretFile file = LoadFile();
        bool removed = file.Secrets.Remove(keyId);
        if (removed)
        {
            SaveFile(file);
        }

        return removed;
    }

    public string DescribeStatus(CloudAiProviderDescriptor provider, CloudAiSettings settings)
    {
        string envVar = ResolveEnvironmentVariable(provider, settings);
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(envVar)))
        {
            return $"Environment variable {envVar} is set.";
        }

        return HasSecret(provider, settings)
            ? $"Secure key saved for {CloudAiProviderLabelFormatter.Format(provider.DisplayName)}."
            : $"No key found. Set {envVar} or save a secure key.";
    }

    private CloudAiSecretFile LoadFile()
    {
        if (!File.Exists(_filePath))
        {
            return new CloudAiSecretFile();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<CloudAiSecretFile>(json, JsonOptions) ?? new CloudAiSecretFile();
        }
        catch (IOException)
        {
            return new CloudAiSecretFile();
        }
        catch (JsonException)
        {
            return new CloudAiSecretFile();
        }
    }

    private void SaveFile(CloudAiSecretFile file)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(file, JsonOptions));
    }

    private static string BuildKeyId(CloudAiProviderDescriptor provider, CloudAiSettings settings)
    {
        return $"{provider.Id}:{ResolveEnvironmentVariable(provider, settings)}".ToLowerInvariant();
    }

    private static string BuildEntropy(CloudAiProviderDescriptor provider, CloudAiSettings settings)
    {
        return $"TwentyFiveSlicer.CloudAI.v1:{BuildKeyId(provider, settings)}";
    }

    public static string ResolveEnvironmentVariable(CloudAiProviderDescriptor provider, CloudAiSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.ApiKeyEnvironmentVariable)
            ? provider.ApiKeyEnvironmentVariable
            : settings.ApiKeyEnvironmentVariable!.Trim();
    }
}

public interface ICloudAiSecretProtector
{
    byte[] Protect(byte[] plaintext, string entropy);

    byte[] Unprotect(byte[] protectedData, string entropy);
}

public sealed class WindowsCurrentUserSecretProtector : ICloudAiSecretProtector
{
    public byte[] Protect(byte[] plaintext, string entropy)
    {
        return ProtectedData.Protect(plaintext, Encoding.UTF8.GetBytes(entropy), DataProtectionScope.CurrentUser);
    }

    public byte[] Unprotect(byte[] protectedData, string entropy)
    {
        return ProtectedData.Unprotect(protectedData, Encoding.UTF8.GetBytes(entropy), DataProtectionScope.CurrentUser);
    }
}

public sealed class CloudAiSecretFile
{
    public int Version { get; set; } = 1;

    public Dictionary<string, CloudAiSecretRecord> Secrets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record CloudAiSecretRecord(
    string ProviderId,
    string ApiKeyEnvironmentVariable,
    string ProtectedApiKey,
    DateTimeOffset UpdatedUtc);
