using System.IO;
using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public AppStateStore(string filePath)
    {
        _filePath = filePath;
    }

    public DesktopAppState Load()
    {
        if (!File.Exists(_filePath))
        {
            return new DesktopAppState();
        }

        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<DesktopAppState>(json, JsonOptions) ?? new DesktopAppState();
    }

    public void Save(DesktopAppState state)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(state, JsonOptions));
    }
}
