# Sprite Sidecar Format

The desktop app now auto-discovers optional sprite sidecar files when you open an image.

If you need a single Unity export that also carries slice data and runtime metadata, use the combined handoff envelope documented in `docs/unity-desktop-handoff-envelope.md`.

## Unity Exporter

Unity can now export this payload from:

- `Tools/Twenty Five Slicer Tools/Export Sprite Sidecar For Selected Sprite`

The exporter resolves the same selection types as the existing JSON bridge:

- Sprite asset
- `TwentyFiveSliceImage`
- `TwentyFiveSliceSpriteRenderer`
- `SpriteRenderer`

By default it saves next to the selected sprite's texture and suggests a file name like `atlas.png.sprite.json`.

If multiple sprites share the same texture, each export still represents the currently selected sprite only, so rename the file if you want to keep multiple sidecars side-by-side.

## Supported File Names

Given an image like `atlas.png`, the desktop app will look for the first existing match in this order:

1. `atlas.png.sprite.json`
2. `atlas.sprite.json`
3. `atlas.png.sprite-context.json`
4. `atlas.sprite-context.json`

## Purpose

Use a sidecar when the selected image is a texture or atlas but the desktop app should author against one specific Unity sprite region.

When a sidecar is found, the desktop app will:

- crop the loaded image to the sprite rect
- use the imported sprite pivot in the Unity runtime summary when `Use sprite pivot` is enabled
- copy the imported pixels-per-unit value into the desktop Unity runtime settings on fresh load
- persist the resolved sprite context in the desktop session state

## JSON Shape

```json
{
  "schemaVersion": 1,
  "spriteName": "ButtonGreen",
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
}
```

## Field Notes

- `coordinateOrigin` supports `bottom-left` and `top-left`.
- Omitted or unrecognized `coordinateOrigin` values default to `bottom-left` to match Unity-style sprite coordinates.
- `spriteRect` is normalized and clamped to the actual loaded image size.
- `pivotPixels` is interpreted in sprite-local pixels, not atlas-global pixels.
- `pixelsPerUnit` defaults to `100` when omitted or invalid.

## Recommended Unity Export Payload

For future Unity-side export support, emit the sprite rect in Unity texture coordinates and set:

- `coordinateOrigin: "bottom-left"`
- `spriteName`
- `pixelsPerUnit`
- `spriteRect`
- `pivotPixels`

That payload is already accepted by the desktop app.