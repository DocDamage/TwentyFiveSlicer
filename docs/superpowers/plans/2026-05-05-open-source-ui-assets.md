# Open Source UI Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reliable visual asset layer for TwentyFiveSlicer using Candy Pixel Art GUI for slice-safe controls plus GitHub-hosted open-source icon sets for app, AI, brand, and IDE visuals.

**Architecture:** Keep third-party assets isolated under `DesktopApp/TwentyFiveSlicer.Desktop/Assets`, with a manifest-driven `UiAssetCatalog` service exposing stable IDs to XAML and code-behind. App-specific slice diagrams remain generated vector/WPF drawings so generic asset packs are not forced to explain TwentyFiveSlicer concepts.

**Tech Stack:** WPF on .NET 8, embedded WPF `Resource` assets, JSON manifests, Tabler Icons SVG, Simple Icons SVG, Devicon SVG, Candy Pixel Art GUI PNGs, existing desktop test harness.

---

## Sources

- Candy Pixel Art GUI: https://untiedgames.itch.io/candy-pixel-art-gui
- Tabler Icons: https://github.com/tabler/tabler-icons, MIT License
- Simple Icons: https://github.com/simple-icons/simple-icons, CC0-1.0 plus brand/trademark caution
- Devicon: https://github.com/devicons/devicon, MIT License plus brand/trademark caution

## File Structure

- Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md`: license/source notices for every imported asset source.
- Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/manifest.json`: stable app asset IDs mapped to embedded resource paths and source packs.
- Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/Tabler/*.svg`: selected generic app icons only.
- Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/SimpleIcons/*.svg`: selected provider icons only when brand guidelines permit use.
- Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/Devicon/*.svg`: selected IDE/dev icons only.
- Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Skin/Candy/*.png`: selected slice-safe Candy Pixel Art GUI PNGs.
- Create `DesktopApp/TwentyFiveSlicer.Desktop/Models/UiAssetDescriptor.cs`: typed manifest row.
- Create `DesktopApp/TwentyFiveSlicer.Desktop/Services/UiAssetCatalog.cs`: loads and validates `manifest.json`.
- Modify `DesktopApp/TwentyFiveSlicer.Desktop/TwentyFiveSlicer.Desktop.csproj`: embed assets as WPF resources and copy notices.
- Modify `DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml`: replace text-only high-frequency commands with icon plus accessible text where useful.
- Modify `DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml.cs`: bind asset-derived provider labels and keep plain text fallback.
- Modify `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`: add catalog, resource existence, notice, and provider fallback tests.
- Modify `README.md`: document asset sources, licensing responsibilities, and import/update process.

---

### Task 1: Asset Intake Boundaries

**Files:**
- Create: `DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md`
- Create: `DesktopApp/TwentyFiveSlicer.Desktop/Assets/manifest.json`
- Modify: `DesktopApp/TwentyFiveSlicer.Desktop/TwentyFiveSlicer.Desktop.csproj`
- Test: `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`

- [ ] **Step 1: Create the third-party notices file**

Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md`:

```markdown
# Third-Party Asset Notices

TwentyFiveSlicer keeps third-party visual assets isolated in this folder. Do not import a new pack without adding a source URL, license summary, and usage note here.

## Candy Pixel Art GUI

- Source: https://untiedgames.itch.io/candy-pixel-art-gui
- Intended use: resizable window, panel, button, area, scrollbar, progress, checkbox, and slider skin assets.
- License summary: commercial and non-commercial use allowed after purchase; attribution required by the listed license; raw asset resale/redistribution is not allowed.
- Import rule: only import individual PNGs that pass TwentyFiveSlicer review. Do not import full pack archives.

## Tabler Icons

- Source: https://github.com/tabler/tabler-icons
- License: MIT
- Intended use: generic app/tool icons such as open, save, export, undo, redo, warning, info, chat, terminal, code, settings, upload, and download.
- Import rule: import only selected SVGs used by the app.

## Simple Icons

- Source: https://github.com/simple-icons/simple-icons
- License: CC0-1.0
- Intended use: optional provider brand badges.
- Trademark caution: Simple Icons license does not override brand or trademark guidelines. Prefer text labels when brand use is uncertain.

## Devicon

- Source: https://github.com/devicons/devicon
- License: MIT
- Intended use: IDE/dev-tool visual indicators.
- Trademark caution: Devicon license does not override brand or trademark guidelines. Prefer generic Tabler icons when brand use is uncertain.
```

- [ ] **Step 2: Create the initial manifest**

Create `DesktopApp/TwentyFiveSlicer.Desktop/Assets/manifest.json`:

```json
{
  "version": 1,
  "assets": [
    { "id": "action.openImage", "path": "Assets/Icons/Tabler/photo-plus.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "action.loadJson", "path": "Assets/Icons/Tabler/file-import.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "action.saveJson", "path": "Assets/Icons/Tabler/device-floppy.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "action.copyJson", "path": "Assets/Icons/Tabler/copy.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "action.exportPng", "path": "Assets/Icons/Tabler/download.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "action.undo", "path": "Assets/Icons/Tabler/arrow-back-up.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "action.redo", "path": "Assets/Icons/Tabler/arrow-forward-up.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "ai.chat", "path": "Assets/Icons/Tabler/message-chatbot.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "ai.review.safe", "path": "Assets/Icons/Tabler/shield-check.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "ai.review.warning", "path": "Assets/Icons/Tabler/alert-triangle.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "ide.bridge", "path": "Assets/Icons/Tabler/terminal-2.svg", "source": "Tabler Icons", "license": "MIT", "kind": "icon" },
    { "id": "provider.openai", "path": "Assets/Icons/SimpleIcons/openai.svg", "source": "Simple Icons", "license": "CC0-1.0", "kind": "brand", "fallbackText": "ChatGPT" },
    { "id": "provider.anthropic", "path": "Assets/Icons/SimpleIcons/anthropic.svg", "source": "Simple Icons", "license": "CC0-1.0", "kind": "brand", "fallbackText": "Claude" },
    { "id": "provider.google", "path": "Assets/Icons/SimpleIcons/google.svg", "source": "Simple Icons", "license": "CC0-1.0", "kind": "brand", "fallbackText": "Gemini" },
    { "id": "dev.github", "path": "Assets/Icons/Devicon/github-original.svg", "source": "Devicon", "license": "MIT", "kind": "brand", "fallbackText": "GitHub" }
  ]
}
```

- [ ] **Step 3: Embed assets as WPF resources**

Modify `DesktopApp/TwentyFiveSlicer.Desktop/TwentyFiveSlicer.Desktop.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <Resource Include="Assets\**\*.svg" />
    <Resource Include="Assets\**\*.png" />
    <Resource Include="Assets\manifest.json" />
    <None Include="Assets\THIRD_PARTY_NOTICES.md" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Add a failing notice test**

In `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`, add the test name to the `tests` array:

```csharp
("Third-party asset notices mention required sources", ThirdPartyAssetNoticesMentionRequiredSources),
```

Add this test method:

```csharp
static void ThirdPartyAssetNoticesMentionRequiredSources()
{
    string noticesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TwentyFiveSlicer.Desktop", "Assets", "THIRD_PARTY_NOTICES.md"));
    string notices = File.ReadAllText(noticesPath);

    Assert.True(notices.Contains("Candy Pixel Art GUI", StringComparison.Ordinal), "Candy Pixel Art GUI notice should be present.");
    Assert.True(notices.Contains("Tabler Icons", StringComparison.Ordinal), "Tabler Icons notice should be present.");
    Assert.True(notices.Contains("Simple Icons", StringComparison.Ordinal), "Simple Icons notice should be present.");
    Assert.True(notices.Contains("Devicon", StringComparison.Ordinal), "Devicon notice should be present.");
}
```

- [ ] **Step 5: Run the test**

Run:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
```

Expected: PASS for the new notice test after the file exists.

- [ ] **Step 6: Commit**

```powershell
git add DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md DesktopApp/TwentyFiveSlicer.Desktop/Assets/manifest.json DesktopApp/TwentyFiveSlicer.Desktop/TwentyFiveSlicer.Desktop.csproj DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs
git commit -m "chore: define desktop asset intake manifest"
```

---

### Task 2: Asset Catalog Service

**Files:**
- Create: `DesktopApp/TwentyFiveSlicer.Desktop/Models/UiAssetDescriptor.cs`
- Create: `DesktopApp/TwentyFiveSlicer.Desktop/Models/UiAssetManifest.cs`
- Create: `DesktopApp/TwentyFiveSlicer.Desktop/Services/UiAssetCatalog.cs`
- Test: `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`

- [ ] **Step 1: Add descriptor models**

Create `DesktopApp/TwentyFiveSlicer.Desktop/Models/UiAssetDescriptor.cs`:

```csharp
namespace TwentyFiveSlicer.Desktop.Models;

public sealed record UiAssetDescriptor(
    string Id,
    string Path,
    string Source,
    string License,
    string Kind,
    string? FallbackText = null);
```

Create `DesktopApp/TwentyFiveSlicer.Desktop/Models/UiAssetManifest.cs`:

```csharp
namespace TwentyFiveSlicer.Desktop.Models;

public sealed record UiAssetManifest(int Version, IReadOnlyList<UiAssetDescriptor> Assets);
```

- [ ] **Step 2: Add the catalog service**

Create `DesktopApp/TwentyFiveSlicer.Desktop/Services/UiAssetCatalog.cs`:

```csharp
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
```

- [ ] **Step 3: Add catalog tests**

Add these test names:

```csharp
("UiAssetCatalog loads manifest assets", UiAssetCatalogLoadsManifestAssets),
("UiAssetCatalog rejects duplicate ids", UiAssetCatalogRejectsDuplicateIds),
```

Add these test methods:

```csharp
static void UiAssetCatalogLoadsManifestAssets()
{
    string manifestPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TwentyFiveSlicer.Desktop", "Assets", "manifest.json"));
    UiAssetCatalog catalog = UiAssetCatalog.LoadFromFile(manifestPath);

    UiAssetDescriptor saveIcon = catalog.GetRequired("action.saveJson");

    Assert.Equal("Assets/Icons/Tabler/device-floppy.svg", saveIcon.Path, "The Save JSON icon should resolve from the manifest.");
    Assert.Equal("MIT", saveIcon.License, "Tabler icons should be recorded as MIT licensed.");
}

static void UiAssetCatalogRejectsDuplicateIds()
{
    string tempFile = Path.Combine(Path.GetTempPath(), $"twenty-five-slicer-assets-{Guid.NewGuid():N}.json");
    File.WriteAllText(tempFile, """
        {
          "version": 1,
          "assets": [
            { "id": "action.saveJson", "path": "one.svg", "source": "Test", "license": "MIT", "kind": "icon" },
            { "id": "action.saveJson", "path": "two.svg", "source": "Test", "license": "MIT", "kind": "icon" }
          ]
        }
        """);

    try
    {
        Assert.Throws<InvalidDataException>(() => UiAssetCatalog.LoadFromFile(tempFile), "Duplicate ids should be rejected.");
    }
    finally
    {
        File.Delete(tempFile);
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add DesktopApp/TwentyFiveSlicer.Desktop/Models/UiAssetDescriptor.cs DesktopApp/TwentyFiveSlicer.Desktop/Models/UiAssetManifest.cs DesktopApp/TwentyFiveSlicer.Desktop/Services/UiAssetCatalog.cs DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs
git commit -m "feat: add desktop UI asset catalog"
```

---

### Task 3: Import Selected Open-Source Icons

**Files:**
- Create: selected SVGs under `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/Tabler`
- Create: selected SVGs under `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/SimpleIcons`
- Create: selected SVGs under `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/Devicon`
- Test: `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`

- [ ] **Step 1: Download selected Tabler icons**

Import only these SVGs from `https://github.com/tabler/tabler-icons/tree/main/icons`:

```text
photo-plus.svg
file-import.svg
device-floppy.svg
copy.svg
download.svg
arrow-back-up.svg
arrow-forward-up.svg
message-chatbot.svg
shield-check.svg
alert-triangle.svg
terminal-2.svg
settings.svg
sparkles.svg
zoom-in.svg
zoom-out.svg
refresh.svg
wand.svg
```

Place them in:

```text
DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/Tabler/
```

- [ ] **Step 2: Download selected brand icons when available**

Import only available SVGs from Simple Icons and Devicon:

```text
DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/SimpleIcons/openai.svg
DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/SimpleIcons/anthropic.svg
DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/SimpleIcons/google.svg
DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons/Devicon/github-original.svg
```

If a brand icon is missing upstream or brand use is uncertain, do not invent a replacement. Keep the manifest entry and use `fallbackText`.

- [ ] **Step 3: Add resource existence test**

Add this test name:

```csharp
("UiAssetCatalog registered files exist", UiAssetCatalogRegisteredFilesExist),
```

Add this test method:

```csharp
static void UiAssetCatalogRegisteredFilesExist()
{
    string desktopRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TwentyFiveSlicer.Desktop"));
    string manifestPath = Path.Combine(desktopRoot, "Assets", "manifest.json");
    UiAssetCatalog catalog = UiAssetCatalog.LoadFromFile(manifestPath);

    foreach (UiAssetDescriptor asset in catalog.Assets)
    {
        string assetPath = Path.Combine(desktopRoot, asset.Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(assetPath) && string.IsNullOrWhiteSpace(asset.FallbackText))
        {
            throw new FileNotFoundException($"Registered UI asset '{asset.Id}' is missing and has no fallback text.", assetPath);
        }
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
```

Expected: all catalog file references either exist or have `fallbackText`.

- [ ] **Step 5: Commit**

```powershell
git add DesktopApp/TwentyFiveSlicer.Desktop/Assets/Icons DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs
git commit -m "feat: import selected open-source UI icons"
```

---

### Task 4: Import Candy Pixel Art GUI Skin Assets

**Files:**
- Create: `DesktopApp/TwentyFiveSlicer.Desktop/Assets/Skin/Candy/*.png`
- Modify: `DesktopApp/TwentyFiveSlicer.Desktop/Assets/manifest.json`
- Test: `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`

- [ ] **Step 1: Buy/download Candy Pixel Art GUI outside the repo**

Download the purchased pack from:

```text
https://untiedgames.itch.io/candy-pixel-art-gui
```

Extract it outside the repository first. Do not commit zip files, PSDs, generator tools, or unused themes.

- [ ] **Step 2: Select one default theme**

Copy only the chosen default theme PNGs needed for these app surfaces:

```text
button normal
button hover
button pressed
button disabled
window/panel
subpanel/card
input/text area
progress meter
scrollbar track
scrollbar thumb
checkbox unchecked
checkbox checked
```

Place them in:

```text
DesktopApp/TwentyFiveSlicer.Desktop/Assets/Skin/Candy/
```

- [ ] **Step 3: Add skin entries to the manifest**

Append entries like this, using actual copied filenames:

```json
{ "id": "skin.button.normal", "path": "Assets/Skin/Candy/button-normal.png", "source": "Candy Pixel Art GUI", "license": "Commercial purchase license", "kind": "skin" },
{ "id": "skin.button.hover", "path": "Assets/Skin/Candy/button-hover.png", "source": "Candy Pixel Art GUI", "license": "Commercial purchase license", "kind": "skin" },
{ "id": "skin.panel.default", "path": "Assets/Skin/Candy/panel-default.png", "source": "Candy Pixel Art GUI", "license": "Commercial purchase license", "kind": "skin" }
```

- [ ] **Step 4: Generate `.25slice.json` for skin candidates**

For each Candy PNG that is intended to stretch, create adjacent metadata using TwentyFiveSlicer after manual/AI review:

```text
button-normal.png
button-normal.25slice.json
panel-default.png
panel-default.25slice.json
```

The generated JSON must keep corners fixed and stretch only safe center/edge regions.

- [ ] **Step 5: Add skin metadata test**

Add this test name:

```csharp
("Candy skin stretch assets include slice metadata", CandySkinStretchAssetsIncludeSliceMetadata),
```

Add this test method:

```csharp
static void CandySkinStretchAssetsIncludeSliceMetadata()
{
    string desktopRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TwentyFiveSlicer.Desktop"));
    string manifestPath = Path.Combine(desktopRoot, "Assets", "manifest.json");
    UiAssetCatalog catalog = UiAssetCatalog.LoadFromFile(manifestPath);

    foreach (UiAssetDescriptor asset in catalog.Assets.Where(asset => asset.Kind.Equals("skin", StringComparison.OrdinalIgnoreCase)))
    {
        string pngPath = Path.Combine(desktopRoot, asset.Path.Replace('/', Path.DirectorySeparatorChar));
        string slicePath = Path.ChangeExtension(pngPath, ".25slice.json");

        Assert.True(File.Exists(pngPath), $"Skin PNG should exist for {asset.Id}.");
        Assert.True(File.Exists(slicePath), $"Stretch skin asset {asset.Id} should have adjacent .25slice.json metadata.");
    }
}
```

- [ ] **Step 6: Run deterministic and image-aware review**

For each generated metadata file, run the CLI review with the matching image:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -- review --slice .\DesktopApp\TwentyFiveSlicer.Desktop\Assets\Skin\Candy\button-normal.25slice.json --proposed .\DesktopApp\TwentyFiveSlicer.Desktop\Assets\Skin\Candy\button-normal.25slice.json --source-width 64 --source-height 32 --target-width 256 --target-height 64 --image .\DesktopApp\TwentyFiveSlicer.Desktop\Assets\Skin\Candy\button-normal.png
```

Expected: JSON review reports `safeToApply: true` or lists risks that are fixed before committing.

- [ ] **Step 7: Run tests**

Run:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 8: Commit**

```powershell
git add DesktopApp/TwentyFiveSlicer.Desktop/Assets/Skin/Candy DesktopApp/TwentyFiveSlicer.Desktop/Assets/manifest.json DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs
git commit -m "feat: import reviewed Candy UI skin assets"
```

---

### Task 5: Wire Icons Into The Desktop UI

**Files:**
- Modify: `DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml`
- Modify: `DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml.cs`
- Test: `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`

- [ ] **Step 1: Add reusable icon button style in XAML**

In `MainWindow.xaml`, add a compact icon button style next to `ActionButtonStyle`:

```xml
<Style x:Key="IconActionButtonStyle" TargetType="Button" BasedOn="{StaticResource ActionButtonStyle}">
    <Setter Property="MinWidth" Value="36" />
    <Setter Property="Padding" Value="8,6" />
    <Setter Property="ToolTipService.ShowDuration" Value="30000" />
</Style>
```

- [ ] **Step 2: Replace high-frequency command labels with icon plus text**

Update the main command row so the labels stay visible while icons improve scan speed:

```xml
<Button Style="{StaticResource ActionButtonStyle}" Click="OpenImage_Click" ToolTip="Open image">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="Open Image" />
    </StackPanel>
</Button>
```

Use the same pattern for Load JSON, Save JSON, Copy JSON, Export PNG, Undo, Redo, Zoom Out, Zoom In, Send Chat, Preview Proposal, Apply Proposal, Reject Proposal, Ask Cloud AI, Batch Check, and Batch Export. Add the actual SVG rendering only after choosing a WPF-safe SVG path strategy; keep labels so the UI works without icon rendering.

- [ ] **Step 3: Add provider fallback mapping**

In `MainWindow.xaml.cs`, add a helper:

```csharp
private static string FormatProviderLabel(CloudAiProvider provider)
{
    return provider.DisplayName switch
    {
        "OpenAI / ChatGPT" => "ChatGPT",
        "Anthropic Claude" => "Claude",
        "Google Gemini" => "Gemini",
        _ => provider.DisplayName
    };
}
```

Use this helper anywhere provider labels are rendered in UI text.

- [ ] **Step 4: Add a provider label test**

If `FormatProviderLabel` remains private, test it indirectly through the provider catalog display flow. If it is moved to a service, add:

```csharp
static void ProviderLabelsPreferFriendlyNames()
{
    Assert.Equal("ChatGPT", CloudAiProviderLabelFormatter.Format("OpenAI / ChatGPT"));
    Assert.Equal("Claude", CloudAiProviderLabelFormatter.Format("Anthropic Claude"));
    Assert.Equal("Gemini", CloudAiProviderLabelFormatter.Format("Google Gemini"));
}
```

- [ ] **Step 5: Build the app**

Run:

```powershell
dotnet build .\DesktopApp\TwentyFiveSlicer.Desktop\TwentyFiveSlicer.Desktop.csproj
```

Expected: build succeeds with no XAML errors.

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml.cs DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs
git commit -m "feat: add asset-backed desktop UI affordances"
```

---

### Task 6: App-Specific Vector Diagrams

**Files:**
- Create: `DesktopApp/TwentyFiveSlicer.Desktop/Controls/SliceConceptDiagramControl.cs`
- Modify: `DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml`
- Test: `DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs`

- [ ] **Step 1: Create a WPF-drawn slice diagram control**

Create `DesktopApp/TwentyFiveSlicer.Desktop/Controls/SliceConceptDiagramControl.cs`:

```csharp
using System.Windows;
using System.Windows.Media;

namespace TwentyFiveSlicer.Desktop.Controls;

public sealed class SliceConceptDiagramControl : FrameworkElement
{
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(220d, 140d);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        Rect bounds = new Rect(0d, 0d, ActualWidth, ActualHeight);
        if (bounds.Width <= 0d || bounds.Height <= 0d)
        {
            return;
        }

        Pen borderPen = new Pen(new SolidColorBrush(Color.FromRgb(54, 71, 91)), 1.5d);
        Pen guidePen = new Pen(new SolidColorBrush(Color.FromRgb(0, 184, 148)), 1d);
        Brush fixedBrush = new SolidColorBrush(Color.FromRgb(242, 201, 76));
        Brush stretchBrush = new SolidColorBrush(Color.FromRgb(86, 204, 242));

        Rect art = new Rect(12d, 12d, Math.Max(20d, bounds.Width - 24d), Math.Max(20d, bounds.Height - 24d));
        drawingContext.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(18, 26, 36)), borderPen, art, 6d, 6d);

        double[] xs = [art.Left, art.Left + art.Width * 0.18d, art.Left + art.Width * 0.36d, art.Left + art.Width * 0.64d, art.Left + art.Width * 0.82d, art.Right];
        double[] ys = [art.Top, art.Top + art.Height * 0.18d, art.Top + art.Height * 0.36d, art.Top + art.Height * 0.64d, art.Top + art.Height * 0.82d, art.Bottom];

        for (int row = 0; row < 5; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                Rect cell = new Rect(xs[column], ys[row], xs[column + 1] - xs[column], ys[row + 1] - ys[row]);
                bool centerStretch = row is 1 or 2 or 3 && column is 1 or 2 or 3;
                drawingContext.DrawRectangle(centerStretch ? stretchBrush : fixedBrush, null, cell);
            }
        }

        for (int i = 1; i < 5; i++)
        {
            drawingContext.DrawLine(guidePen, new Point(xs[i], art.Top), new Point(xs[i], art.Bottom));
            drawingContext.DrawLine(guidePen, new Point(art.Left, ys[i]), new Point(art.Right, ys[i]));
        }

        drawingContext.DrawRoundedRectangle(null, borderPen, art, 6d, 6d);
    }
}
```

- [ ] **Step 2: Add the diagram to the help/validation area**

In `MainWindow.xaml`, place the diagram near validation or assistant guidance:

```xml
<controls:SliceConceptDiagramControl Width="220"
                                     Height="140"
                                     HorizontalAlignment="Left"
                                     Margin="0,8,0,0" />
```

- [ ] **Step 3: Add render smoke test**

Add a test that instantiates and renders the control:

```csharp
static void SliceConceptDiagramRenders()
{
    var control = new SliceConceptDiagramControl
    {
        Width = 220d,
        Height = 140d
    };

    control.Measure(new Size(220d, 140d));
    control.Arrange(new Rect(0d, 0d, 220d, 140d));
    control.UpdateLayout();

    var bitmap = new RenderTargetBitmap(220, 140, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(control);

    Assert.Equal(220, bitmap.PixelWidth, "Diagram render should preserve requested width.");
    Assert.Equal(140, bitmap.PixelHeight, "Diagram render should preserve requested height.");
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add DesktopApp/TwentyFiveSlicer.Desktop/Controls/SliceConceptDiagramControl.cs DesktopApp/TwentyFiveSlicer.Desktop/MainWindow.xaml DesktopApp/TwentyFiveSlicer.Desktop.Tests/Program.cs
git commit -m "feat: add native slice concept diagram"
```

---

### Task 7: Documentation And Release Verification

**Files:**
- Modify: `README.md`
- Modify: `DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md`

- [ ] **Step 1: Document asset strategy in README**

Add a subsection under the standalone desktop app section:

```markdown
### Desktop Visual Assets

The standalone desktop app uses a small curated visual asset layer:

- Candy Pixel Art GUI for purchased, slice-safe skin PNGs.
- Tabler Icons for generic MIT-licensed app/tool SVG icons.
- Simple Icons only for provider brand badges when brand use is appropriate.
- Devicon only for IDE/developer-tool badges when brand use is appropriate.
- TwentyFiveSlicer-specific slice diagrams are drawn natively in WPF so they stay accurate to the 25-slice model.

Third-party asset notices live in `DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md`. New assets must be registered in `Assets/manifest.json` and covered by tests.
```

- [ ] **Step 2: Run the full desktop verification**

Run:

```powershell
dotnet build .\DesktopApp\TwentyFiveSlicer.Desktop\TwentyFiveSlicer.Desktop.csproj
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
dotnet publish .\DesktopApp\TwentyFiveSlicer.Desktop\TwentyFiveSlicer.Desktop.csproj -c Release -r win-x64 --self-contained true
```

Expected:

```text
Build succeeded.
All tests pass.
Publish succeeds and includes Assets/ resources.
```

- [ ] **Step 3: Inspect publish output**

Run:

```powershell
Get-ChildItem -LiteralPath .\DesktopApp\TwentyFiveSlicer.Desktop\bin\Release\net8.0-windows\win-x64\publish -Recurse | Where-Object { $_.Name -match 'THIRD_PARTY_NOTICES|TwentyFiveSlicer.Desktop.exe' } | Select-Object FullName
```

Expected: the standalone executable and third-party notices are present.

- [ ] **Step 4: Commit**

```powershell
git add README.md DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md
git commit -m "docs: document desktop visual asset sources"
```

---

## Final Verification Checklist

- [ ] `dotnet build .\DesktopApp\TwentyFiveSlicer.Desktop\TwentyFiveSlicer.Desktop.csproj`
- [ ] `dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj`
- [ ] `dotnet publish .\DesktopApp\TwentyFiveSlicer.Desktop\TwentyFiveSlicer.Desktop.csproj -c Release -r win-x64 --self-contained true`
- [ ] Manual launch published `.exe`
- [ ] Confirm text fallbacks appear for provider badges if brand SVGs are unavailable
- [ ] Confirm Candy skin PNGs have adjacent `.25slice.json`
- [ ] Confirm every imported third-party source appears in `THIRD_PARTY_NOTICES.md`

## Risks

- Candy Pixel Art GUI requires purchase and should not be committed as an entire raw pack.
- Simple Icons and Devicon licenses do not override provider/developer-tool trademark rules.
- WPF does not render arbitrary SVG as `Image.Source` without extra support, so the first UI wiring pass keeps labels and can defer actual SVG rendering to a controlled renderer or conversion step.
- Asset imports can bloat the repo if full packs are copied. Import only selected files.

