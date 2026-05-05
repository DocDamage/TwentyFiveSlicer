# 🚀 Announcing N-Slicer: The Evolution of TwentyFiveSlicer

📢 **New Release**: Beyond the open-source spirit of TwentyFiveSlicer, this enhanced solution is now available on the **Unity Asset Store**!
 
- [Purchase on Unity Asset Store](https://assetstore.unity.com/packages/slug/319511)
- [Visit the Official Website](https://www.nbeyond.dev/docs/nslicer)

**[N-Slicer](https://assetstore.unity.com/packages/slug/319511) is the next-generation sprite slicing solution, representing the technological advancement of TwentyFiveSlicer!**
 
<img src="Documentation~/Images/nslicer_cover.png" alt="n-slicer" width="700" />

<img src="Documentation~/Images/nslicer.gif" alt="n-slicer" width="700" />

While TwentyFiveSlicer evolved from 9-slice to 25-slice, N-Slicer takes it to the next level with **unlimited customizable slices**. Each slice can be individually configured as Fixed or Stretchable, enabling more sophisticated UI implementations.

---

# Twenty Five Slicer

[![openupm](https://img.shields.io/npm/v/com.kwanjoong.twentyfiveslicer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.kwanjoong.twentyfiveslicer/)

**Twenty Five Slicer** is a Unity package designed for more advanced sprite slicing, enabling a "25-slice" approach. It divides a sprite into a 5x5 grid, allowing precise scaling and manipulation of individual regions while preserving key areas.

---

## 9-slice vs 25-slice

<p align="center">
  <img src="Documentation~/Images/9slice_VS_25slice_3.gif" alt="9-slice vs 25-slice" width="700" />
</p>

---

## Key Concept

<p align="center">
  <img src="Documentation~/Images/25slice_debugging_view.gif" alt="25-slice Debugging View" width="700" />
</p>

- **9 slices**: Non-stretchable areas.
- **6 slices**: Stretch horizontally only.
- **6 slices**: Stretch vertically only.
- **4 slices**: Stretch in both directions.

This allows for far more detailed slicing. Where traditional 9-slice images often require stacking multiple image layers to achieve complex UI shapes (e.g., speech bubbles, boxes with icons or separators at the center), a 25-slice configuration can often handle these scenarios with just a single image.

---

## Installing the Package

### 1. Install via OpenUPM

#### 1.1. Install via Package Manager
Please follow the instructions:
1. Open **Edit → Project Settings → Package Manager**
2. Add a new Scoped Registry (or edit the existing OpenUPM entry)
  - **Name**: `package.openupm.com`
  - **URL**: `https://package.openupm.com`
3. Click **Save** or **Apply**
4. Open **Window → Package Manager**
5. Click the `+` button
6. Select **Add package by name...** (or **Add package from git URL...**)
7. Paste `com.kwanjoong.twentyfiveslicer` into **Name**
8. Paste a version (e.g., `1.1.2`) into **Version**
9. Click **Add**

---

#### 1.2. Alternatively, merge the snippet into `Packages/manifest.json`
```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": []
    }
  ],
  "dependencies": {
    "com.kwanjoong.twentyfiveslicer": "1.1.2"
  }
}
```

---

#### 1.3. Install via command-line interface
```sh
openupm add com.kwanjoong.twentyfiveslicer
```

---

### 2. Install via Git URL

1. Open the Unity **Package Manager**.
2. Select **Add package from Git URL**.
3. Enter: `https://github.com/kwan3854/twentyfiveslicer.git`
4. To install a specific version, append a version tag, for example:  
   `https://github.com/kwan3854/twentyfiveslicer.git#v1.0.0`

---

## How to Use

### Create Slice Data Map (First-time Setup)

1. Navigate to the `Assets/Resources` folder. (Create it if it doesn't exist.)
2. Right-click → **Create → TwentyFiveSlicer → SliceDataMap**

<p align="center">
  <img src="Documentation~/Images/how_to_add_25slice_datamap.png" alt="How to Add 25-slice DataMap" width="550" style="display:inline-block; margin-right:20px;" />
  <img src="Documentation~/Images/sliceDataMap.png" alt="SliceDataMap Example" width="200" style="display:inline-block;" />
</p>

---

### Editing a Sprite

1. **Open the 25-Slice Editor**
  - **Window → 2D → 25-Slice Editor**

   <p align="center">
     <img src="Documentation~/Images/how_to_open_editor.png" alt="How to Open 25-Slice Editor" width="700" />
   </p>

2. **Load Your Sprite**
  - Drag and drop your sprite into the editor or select it via the provided field.

3. **Adjust the Slices**
  - Use the sliders to define horizontal and vertical cut lines, dividing the sprite into 25 sections.
  - Borders are displayed visually for accurate adjustments.

   <p align="center">
     <img src="Documentation~/Images/editor.png" alt="25-Slice Editor" width="700" />
   </p>

4. **Save the Configuration**
  - Click **Save Borders** to store the 25-slice settings.

---

### Using the 25-Sliced Sprite

#### 1. Using with **UI (TwentyFiveSliceImage)**

This is the **UI** approach, similar to `UnityEngine.UI.Image`:
1. **Create a TwentyFiveSliceImage GameObject** or add `TwentyFiveSliceImage` to an existing **UI** element in a Canvas.
2. Assign your 25-sliced sprite to the `TwentyFiveSliceImage`.
3. Adjust the RectTransform size to see how each slice region scales or remains fixed.

<p align="center">
  <img src="Documentation~/Images/how_to_add_25slice_gameobject.png" alt="How to Add 25-Slice GameObject" width="700" />
</p>
<p align="center">
  <img src="Documentation~/Images/image_component.png" alt="How to Add 25-Slice GameObject" width="700" />
</p>

#### 2. Using with **2D Scenes (TwentyFiveSliceSpriteRenderer)**

This is the **MeshRenderer**-based approach, similar to `SpriteRenderer`:
1. You can create a **25-Sliced Sprite** in the **Hierarchy**:
  - **Right-click → 2D Object → Sprites → 25-Sliced**  
    *This will instantiate a GameObject named `25-Sliced Sprite` with `TwentyFiveSliceSpriteRenderer` attached.*
2. In the Inspector, assign your 25-sliced sprite to its `Sprite` field.
3. Adjust the **Size** property in the Inspector (instead of using `transform.localScale`) to properly stretch or preserve corners/edges as needed.
4. **Sorting Layer** and **Order in Layer** are also available, just like a normal SpriteRenderer.

<p align="center">
  <img src="Documentation~/Images/sprite_renderer_menu.png" alt="How to Add 25-Slice GameObject" width="700" />
</p>
<p align="center">
  <img src="Documentation~/Images/sprite_renderer_component.png" alt="How to Add 25-Slice GameObject" width="700" />
</p>

---

## Key Features

- Divide sprites into a 5x5 grid for highly detailed control.
- Seamlessly scale and stretch specific sprite regions.
- **UI approach** (`TwentyFiveSliceImage`) for usage in UGUI-based canvases.
- **2D Mesh approach** (`TwentyFiveSliceSpriteRenderer`) for usage in 2D scenes without UI.
- Compatible with Unity's 2D workflow, supports Sorting Layers.
- Intuitive editor window with clear visual guidance for precise adjustments.

---

## Delete Unused Data

You can remove slice data that is no longer needed:
**Tools → Twenty Five Slicer Tools → Slice Data Cleaner**

---

## Standalone Windows Desktop App

A standalone WPF desktop app lives in `DesktopApp/TwentyFiveSlicer.Desktop`. It is intended for users who want the original 25-slice authoring workflow without opening Unity.

The desktop app preserves the important behavior from the Unity package:

- 4 vertical borders and 4 horizontal borders stored as percentages.
- Unity-compatible JSON using `verticalBorders` and `horizontalBorders`.
- The same 5x5 fixed/stretch layout rules as `TwentyFiveSliceImage` and `TwentyFiveSliceSpriteRenderer`.
- Fixed-region scaling when the output target is smaller than the fixed edge total.
- Flip X/Y, debug overlay, source guide comparison, and PNG preview export.
- Direct guide dragging, including intersection handles that move vertical and horizontal guides together.
- Preview zoom from 50% to 200%, including slider, mouse wheel, quick zoom buttons, zoomed-preview panning, and 100% reset.

### Desktop Authoring Features

- Open PNG, JPG, BMP, GIF, TIFF, or JSON files by button or drag and drop.
- Save and copy Unity-compatible slice JSON.
- Export rendered PNG previews, with optional debug overlay.
- Edit borders with sliders, numeric fields, or direct preview guide dragging.
- Undo and redo border/target-size changes.
- Recent file restore and last-session persistence.
- Built-in presets, user presets, candidate preview/apply workflow, and batch checks across common target sizes.
- Batch export common preview sizes to a folder.

### Desktop Visual Assets

The standalone desktop app keeps third-party visual assets in a small curated layer:

- Candy Pixel Art GUI for reviewed, slice-safe skin PNGs.
- Tabler Icons for generic MIT-licensed app/tool SVG icons.
- Simple Icons only for provider brand badges when brand use is appropriate.
- Devicon only for IDE/developer-tool badges when brand use is appropriate.
- TwentyFiveSlicer-specific slice diagrams are drawn natively in WPF so they stay accurate to the 25-slice model.

Third-party notices live in `DesktopApp/TwentyFiveSlicer.Desktop/Assets/THIRD_PARTY_NOTICES.md`. New assets must be registered in `DesktopApp/TwentyFiveSlicer.Desktop/Assets/manifest.json` and covered by tests. Raw vendor drops such as `candy_pixel_art_gui/` are intentionally ignored; only curated imports should be committed.

### Unity JSON Bridge

The Unity package can import and export the same JSON files as the standalone desktop app:

- **Tools → Twenty Five Slicer Tools → Import Slice JSON For Selected Sprite**
- **Tools → Twenty Five Slicer Tools → Export Slice JSON For Selected Sprite**

Select a Sprite asset, `TwentyFiveSliceImage`, `TwentyFiveSliceSpriteRenderer`, or normal `SpriteRenderer` before using these menu items. Import writes the selected sprite's data into `SliceDataMap`; export writes the saved `SliceDataMap` entry to desktop-compatible JSON.

### Local And Cloud AI Assistance

The desktop app includes local assistant features and optional cloud AI.

Local assistant features:

- Chat-style assistant panel with message history, reviewed proposals, preview, apply, and reject controls.
- Chat uses local deterministic commands by default and can use the selected cloud AI provider when Cloud AI is enabled.
- Local chat understands conversational requests such as "the corners are getting warped", "the middle feels too stretched", or "make it behave like a wide CTA button".
- Chat understands proposal commands such as "preview it", "apply it", "reject it", "explain the risk", and "try a safer version".
- Detect transparent padding and suggest borders.
- Analyze the current slice setup for warnings.
- Apply natural-language slice edits such as "make this a button", "make this a panel", "symmetrize", or "make corners thicker".
- Rank candidate slice presets for one target size or across multiple target sizes.

Cloud AI features:

- Optional provider selection and endpoint override.
- API keys are read from environment variables only; raw keys are not saved in app state.
- Vision-capable providers receive the loaded image as PNG context.
- If a cloud model returns exact `verticalBorders` and `horizontalBorders`, the app parses and applies them automatically.
- Supported provider presets include OpenAI / ChatGPT, Anthropic Claude, Google Gemini, Moonshot Kimi, DeepSeek, Zhipu GLM, MiniMax, Mistral, Cohere, Groq, xAI Grok, Perplexity, and custom OpenAI-compatible endpoints.

### IDE / Codex AI CLI

IDE agents can use the same AI helpers without the WPF UI through `DesktopApp/TwentyFiveSlicer.AiCli`. It writes JSON to stdout for easy parsing.

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -- providers
dotnet run --project .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -- analyze --slice .\button.25slice.json --source-width 256 --source-height 128 --target-width 640 --target-height 160
dotnet run --project .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -- apply --slice .\button.25slice.json --prompt "make this a button"
dotnet run --project .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -- prompt --slice .\button.25slice.json --prompt "show the exact cloud prompt"
dotnet run --project .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -- review --slice .\button.25slice.json --proposed .\suggested.25slice.json --source-width 256 --source-height 128 --target-width 640 --target-height 160 --image .\button.png
dotnet run --project .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -- cloud --slice .\button.25slice.json --provider openai --prompt "recommend safer borders"
```

The `review` command is the deterministic accuracy gate for AI-suggested cuts. Add `--image` to check proposed outer guides against detected opaque content bounds. The `cloud` command also includes this review when exact border JSON is parsed from a model response. The `cloud` command supports `--model`, `--endpoint`, `--api-key-env`, and `--image`. Use `--slice -` to read slice JSON from stdin. API keys still come from environment variables. Errors are returned as JSON with the `twenty-five-slicer.ai.error.v1` schema.

Common API key environment variables:

```powershell
$env:OPENAI_API_KEY="..."
$env:ANTHROPIC_API_KEY="..."
$env:GEMINI_API_KEY="..."
$env:MOONSHOT_API_KEY="..."
$env:DEEPSEEK_API_KEY="..."
$env:ZHIPU_API_KEY="..."
$env:MINIMAX_API_KEY="..."
$env:MISTRAL_API_KEY="..."
$env:COHERE_API_KEY="..."
$env:GROQ_API_KEY="..."
$env:XAI_API_KEY="..."
$env:PERPLEXITY_API_KEY="..."
```

### Build And Test

Run the desktop test harness:

```powershell
dotnet run --project .\DesktopApp\TwentyFiveSlicer.Desktop.Tests\TwentyFiveSlicer.Desktop.Tests.csproj
```

Build the desktop app:

```powershell
dotnet build .\DesktopApp\TwentyFiveSlicer.Desktop\TwentyFiveSlicer.Desktop.csproj
dotnet build .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj
```

### Publish A Standalone Exe

```powershell
dotnet publish .\DesktopApp\TwentyFiveSlicer.Desktop\TwentyFiveSlicer.Desktop.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
dotnet publish .\DesktopApp\TwentyFiveSlicer.AiCli\TwentyFiveSlicer.AiCli.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

Published output:

- `DesktopApp/TwentyFiveSlicer.Desktop/bin/Release/net8.0-windows/win-x64/publish/TwentyFiveSlicer.Desktop.exe`
- `DesktopApp/TwentyFiveSlicer.AiCli/bin/Release/net8.0-windows/win-x64/publish/TwentyFiveSlicer.AiCli.exe`

The published executables are self-contained for Windows x64 and do not require a Unity project.

---

For more information or contributions, visit the [repository](https://github.com/kwan3854/TwentyFiveSlicer).
