namespace TwentyFiveSlicer.Desktop.Services;

public sealed class RecentFileList
{
    private readonly int _capacity;
    private readonly List<string> _files = new();

    public RecentFileList(int capacity = 8)
    {
        _capacity = Math.Max(1, capacity);
    }

    public IReadOnlyList<string> Files => _files;

    public void Add(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        _files.RemoveAll(existing => string.Equals(existing, filePath, StringComparison.OrdinalIgnoreCase));
        _files.Insert(0, filePath);

        while (_files.Count > _capacity)
        {
            _files.RemoveAt(_files.Count - 1);
        }
    }

    public void Replace(IEnumerable<string> filePaths)
    {
        _files.Clear();
        foreach (string filePath in filePaths.Reverse())
        {
            Add(filePath);
        }
    }
}
