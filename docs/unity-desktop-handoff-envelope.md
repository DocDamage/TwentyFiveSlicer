# Unity Desktop Handoff Envelope

The Unity package and the standalone desktop app can now exchange a combined handoff payload.

## Exporter

Use:

- `Tools/Twenty Five Slicer Tools/Export Desktop Handoff Envelope For Selected Target`
- `Tools/Twenty Five Slicer Tools/Import Desktop Handoff Envelope For Selected Target`

The exporter resolves the same selection types as the other bridge commands:

- Sprite asset
- `TwentyFiveSliceImage`
- `TwentyFiveSliceSpriteRenderer`
- `SpriteRenderer`

The output is a versioned envelope that combines:

- desktop-style slice data
- sprite/atlas context
- runtime metadata
- intended Unity target kind

By default it saves next to the selected sprite texture and suggests a file name like `Button.25slice.handoff.json`.

## JSON Shape

```json
{
  "schemaVersion": 1,
  "targetKind": "twentyFiveSliceImage",
  "targetName": "PlayButton",
  "sliceData": {
    "schemaVersion": 2,
    "xGuidesPercent": [12, 42, 58, 88],
    "yGuidesPercent": [12, 42, 58, 88],
    "xSegments": [
      { "mode": "fixed" },
      { "mode": "stretch" },
      { "mode": "fixed" },
      { "mode": "stretch" },
      { "mode": "fixed" }
    ],
    "ySegments": [
      { "mode": "fixed" },
      { "mode": "stretch" },
      { "mode": "fixed" },
      { "mode": "stretch" },
      { "mode": "fixed" }
    ]
  },
  "spriteContext": {
    "schemaVersion": 1,
    "spriteName": "ButtonGreen",
    "texturePath": "Assets/UI/atlas.png",
    "textureWidth": 256,
    "textureHeight": 128,
    "coordinateOrigin": "bottom-left",
    "pixelsPerUnit": 64,
    "spriteRect": {
      "x": 32,
      "y": 16,
      "width": 96,
      "height": 48
    },
    "pivotPixels": {
      "x": 24,
      "y": 12
    }
  },
  "runtimeSettings": {
    "tintHex": "#FFFFFFFF",
    "pixelsPerUnit": 64,
    "useSpritePivot": true,
    "customPivotX": 0,
    "customPivotY": 0,
    "sortingLayerName": "Default",
    "sortingOrder": 0,
    "materialName": "",
    "raycastTarget": true,
    "raycastPaddingLeft": 0,
    "raycastPaddingBottom": 0,
    "raycastPaddingRight": 0,
    "raycastPaddingTop": 0
  }
}
```

## Target Kinds

- `spriteAsset`
- `twentyFiveSliceImage`
- `twentyFiveSliceSpriteRenderer`
- `spriteRenderer`

## Field Notes

- Root `schemaVersion` is the handoff-envelope version.
- `sliceData.schemaVersion` is the desktop slice schema version and currently exports as `2`.
- Unity's fixed 25-slice data is exported into desktop variable-grid format using the default five-segment pattern:
  - `fixed`
  - `stretch`
  - `fixed`
  - `stretch`
  - `fixed`
- `spriteContext` matches the standalone sprite sidecar contract.
- `runtimeSettings` is component-aware:
  - `TwentyFiveSliceImage` exports tint, material, raycast target, and raycast padding
  - `TwentyFiveSliceSpriteRenderer` exports tint, effective PPU, pivot mode/custom pivot, sorting layer/order, and material
  - `SpriteRenderer` exports tint, effective PPU, sorting layer/order, and material
  - Sprite-asset selection exports defaults derived from the sprite itself

## Current Scope

The standalone desktop app can import this combined envelope through its existing slice-load flow. Open the `.json` file from the desktop app's load-slice action or drop it onto the window, and the app will restore:

- `sliceData`
- `spriteContext`
- `runtimeSettings`

When the referenced texture can be resolved locally, the desktop app also loads and crops the source image automatically. When it cannot be resolved locally, the desktop app still restores the imported sprite and runtime metadata and reports that the referenced texture is unavailable.

The desktop app can now export the reviewed state back to a combined handoff envelope through the main toolbar's `Export Handoff` action. That export preserves imported `targetKind` and `targetName` metadata when present and otherwise defaults to a `spriteAsset` target derived from the current source image or sprite context.

Before writing the file, the desktop export runs the same fixed-layout compatibility check that Unity uses during import. If the current variable-grid data would be rejected by Unity, the desktop app warns the user and explains why before allowing the export to continue.

Unity can now import the reviewed handoff envelope back onto the currently selected target as well. The Unity importer:

- updates `SliceDataMap` for the selected sprite
- applies runtime settings directly to the selected component when the envelope target kind matches
- supports `TwentyFiveSliceImage`, `TwentyFiveSliceSpriteRenderer`, and `SpriteRenderer`

## Unity Import Constraints

The Unity package still renders only the fixed 25-slice layout. Because of that, Unity import currently accepts only the compatible desktop subset:

- exactly four `xGuidesPercent` values
- exactly four `yGuidesPercent` values
- the default five-segment pattern on each axis:
  - `fixed`
  - `stretch`
  - `fixed`
  - `stretch`
  - `fixed`

If a desktop envelope contains arbitrary variable-grid guides or non-default segment modes, Unity import rejects it instead of silently misapplying it.

The desktop app mirrors these same constraints as an export-time preflight warning so unsupported envelopes are called out before they are sent back to Unity.

## Unity Apply Notes

- `TwentyFiveSliceImage` import applies tint, material, raycast target, and raycast padding.
- `TwentyFiveSliceSpriteRenderer` import applies tint, effective pixels per unit, pivot mode, custom pivot, sorting layer/order, and material.
- `SpriteRenderer` import applies tint, sorting layer/order, and material.
- `spriteAsset` envelopes update `SliceDataMap` only; no component runtime settings are applied.
- Material import is best-effort by material name. If the project has no unique matching material asset, Unity leaves the current material unchanged and reports a warning.