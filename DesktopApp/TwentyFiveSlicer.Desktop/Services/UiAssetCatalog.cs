using System.IO;
using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class UiAssetCatalog
{
    private readonly Dictionary<string, UiAssetDescriptor> _assets;

    private UiAssetCatalog(IEnumerable<UiAssetDescriptor> assets)
    {
        _assets = assets.ToDictionary(asset => asset.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<UiAssetDescriptor> Assets => _assets.Values;

    public static UiAssetCatalog LoadFromFile(string manifestPath)
    {
        string json = File.ReadAllText(manifestPath);
        UiAssetManifest? manifest = JsonSerializer.Deserialize<UiAssetManifest>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (manifest is null)
        {
            throw new InvalidDataException("The UI asset manifest could not be read.");
        }

        if (manifest.Version != 1)
        {
            throw new InvalidDataException($"Unsupported UI asset manifest version {manifest.Version}.");
        }

        if (manifest.Assets.Count == 0)
        {
            throw new InvalidDataException("The UI asset manifest does not contain any assets.");
        }

        string? duplicateId = manifest.Assets
            .GroupBy(asset => asset.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (!string.IsNullOrWhiteSpace(duplicateId))
        {
            throw new InvalidDataException($"Duplicate UI asset id '{duplicateId}'.");
        }

        foreach (UiAssetDescriptor asset in manifest.Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Id) ||
                string.IsNullOrWhiteSpace(asset.Path) ||
                string.IsNullOrWhiteSpace(asset.Source) ||
                string.IsNullOrWhiteSpace(asset.License) ||
                string.IsNullOrWhiteSpace(asset.Kind))
            {
                throw new InvalidDataException("Every UI asset must declare id, path, source, license, and kind.");
            }
        }

        return new UiAssetCatalog(manifest.Assets);
    }

    public UiAssetDescriptor GetRequired(string id)
    {
        if (_assets.TryGetValue(id, out UiAssetDescriptor? descriptor))
        {
            return descriptor;
        }

        throw new KeyNotFoundException($"UI asset '{id}' is not registered.");
    }

    public bool TryGet(string id, out UiAssetDescriptor descriptor)
    {
        return _assets.TryGetValue(id, out descriptor!);
    }
}
