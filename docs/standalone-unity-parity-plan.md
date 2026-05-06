# Standalone Unity Parity Plan

## Goal
Bring the standalone desktop app to practical parity with the Unity package for the features that matter during authoring, validation, and handoff, without pretending the desktop app can directly replace a live Unity scene.

## Current Gap
- The desktop app is stronger at authoring workflows, AI, batch checks, variable-grid editing, and desktop-only per-cell override workflows.
- The Unity package is still stronger at Unity-native component configuration, sprite asset context, hierarchy creation, and project-side data ownership.

## Scope
- Parity target: the standalone should understand, persist, preview, validate, and hand off Unity-relevant settings.
- Non-goal: the standalone should not try to reimplement the Unity editor or fabricate scene objects without a bridge back into a Unity project.

## Progress Snapshot
- Implemented:
  - desktop Unity runtime settings with session persistence, preview tinting, and runtime summaries
  - sprite sidecar ingestion with atlas rect, pivot, and pixels-per-unit metadata
  - no-sidecar source loading that can auto-detect sprite regions from transparent or flat-background textures, filter out tiny noise regions, and let the desktop app switch between multiple detected candidates through labels or thumbnails
  - combined handoff-envelope import/export between Unity and the desktop app
  - Unity-side apply flow back into `SliceDataMap` and compatible selected components
  - desktop export-time preflight warnings that mirror Unity's fixed 25-slice importer constraints
  - desktop per-cell source/target rect overrides with direct preview cell picking
  - desktop target profiles for `spriteAsset`, `TwentyFiveSliceImage`, `TwentyFiveSliceSpriteRenderer`, and `SpriteRenderer`
  - profile-aware runtime defaults, runtime summaries, and target-specific validation warnings in the desktop app
- Remaining:
  - richer project-aware bridge workflows such as enumerating known Unity targets
  - project-aware validation parity such as resolving real Unity targets, sorting layers, and component availability from exported project manifests

## Feature Areas

### 1. Unity Runtime Settings
- Add a desktop-side runtime settings model that covers the Unity-only surface users expect when moving between tools.
- Include:
  - tint/color
  - pixels per unit
  - sprite pivot vs custom pivot
  - sorting layer and order
  - material name placeholder/metadata
  - raycast target
  - raycast padding
- Persist these settings in desktop session state.
- Make the visually meaningful subset affect desktop preview immediately:
  - tint multiplies the rendered preview
  - pixels per unit and pivot affect runtime summaries and bounds calculations
- Validation:
  - focused desktop tests for session round-trip and preview tinting

### 2. Sprite Asset Context
- Add optional sprite-source metadata so the standalone can model real Unity Sprite assets instead of only raw image files.
- Include:
  - sprite rect inside a texture/atlas
  - sprite name
  - imported pivot in pixels
  - imported pixels per unit
  - optional atlas/texture path metadata
- Support ingest from a Unity-produced sidecar payload first.
- Later option: read a dedicated export manifest produced by the Unity package.
- Validation:
  - tests for sidecar parsing and atlas-rect preview correctness

### 3. SliceDataMap And Project Bridge
- Add a bridge format that lets the desktop target a Unity project deliberately instead of only copying JSON by hand.
- Add desktop commands for:
  - choosing a Unity project root
  - listing known slice targets from exported Unity metadata
  - exporting slice updates back to a bridge folder or CLI contract
- Add Unity-side commands that can:
  - export selected sprite/component context for the desktop app
  - import approved desktop output into SliceDataMap
  - resolve sorting layers and target components
- Keep direct project mutation behind explicit user action.
- Validation:
  - desktop and Unity integration tests around stable bridge payloads

### 4. Component-Aware Authoring
- Make the desktop app understand the difference between:
  - TwentyFiveSliceImage
  - TwentyFiveSliceSpriteRenderer
  - SpriteRenderer
- Show component-specific summaries and warnings.
- Add profile presets for common Unity targets:
  - UI image authoring profile
  - 25-slice sprite renderer authoring profile
  - plain sprite renderer bridge profile
- Carry component metadata through the bridge so the Unity side can apply it safely.
- Status:
  - implemented in the desktop app through the runtime-panel profile selector, recommended-default presets, and profile-aware summary/validation surfaces
- Validation:
  - tests for component profile round-trip and expected defaults

### 5. Validation Parity
- Extend desktop validation beyond slice math.
- Add warnings for:
  - invalid or suspicious pixels per unit
  - custom pivot values far outside source bounds
  - unusual sorting metadata
  - raycast padding that is large relative to output size
  - unsupported handoff combinations
- Surface both preview-impacting issues and Unity-handoff issues separately.
- Status:
  - desktop handoff export now warns when Unity will reject the envelope because the layout no longer matches the fixed 25-slice subset
  - desktop validation now warns about suspicious runtime metadata and target-specific fields that Unity will ignore for the selected authoring profile
  - raw-texture sprite auto-detection now has a minimum connected-pixel filter to suppress tiny fragments before authoring begins
- Validation:
  - focused service tests for each warning path

### 6. Handoff UX
- Add a clear handoff flow in the desktop app:
  - author
  - validate
  - package for Unity
  - apply in Unity
- Avoid mixing slice JSON with Unity-only component metadata unless the schema is explicitly versioned.
- Prefer a bridge envelope that contains:
  - slice data
  - runtime settings
  - sprite context
  - intended Unity target kind
- Status:
  - the desktop app can now load and export the combined handoff envelope directly
  - imported `targetKind` and `targetName` metadata are preserved on the desktop side for round-trip review/edit/apply workflows
- Validation:
  - tests that envelope payloads remain stable and backwards compatible

## Recommended Implementation Order

### Phase 1
- Add the runtime settings foundation to the desktop app.
- Persist it in session state.
- Make tint affect preview.
- Show runtime bounds/metadata summary.
- Status: implemented.

### Phase 2
- Add sprite asset context sidecar support.
- Use it to improve pivot, PPU, and atlas correctness.
- Status: implemented.

### Phase 3
- Add an explicit desktop-to-Unity bridge contract.
- Let Unity export/import context instead of relying on manual copy/paste.
- Status: implemented, including desktop-side handoff export and Unity compatibility preflight warnings.

### Phase 4
- Add component-aware presets, validation, and apply workflows.
- Status: implemented for desktop-side target profiles, recommended defaults, runtime summaries, and validation warnings.

## Next Recommended Slice
- Extend the bridge into project-aware target discovery:
  - export Unity project manifests that enumerate known compatible targets and sorting layers
  - let the desktop app pick from real Unity targets instead of only generic target kinds
  - validate handoff envelopes against project-specific availability before export/apply