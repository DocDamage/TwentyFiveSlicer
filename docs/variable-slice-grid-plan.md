# Save Then Implement Variable X/Y Slice Grid

## Summary
Before coding starts, save the approved plan to `docs/variable-slice-grid-plan.md`, then implement the variable-grid slicer in focused stages. The feature replaces fixed 25-slice editing with arbitrary X/Y guides, per-segment fixed/stretch/hidden behavior, and backward-compatible loading of old `.25slice.json` files.

## Key Changes
- Save the plan first:
  - create `docs/variable-slice-grid-plan.md`
  - include the approved variable-grid plan, schema summary, UI goals, and test checklist
- Add v2 `.25slice.json` support:
  - `schemaVersion: 2`
  - `xGuidesPercent`
  - `yGuidesPercent`
  - `xSegments`
  - `ySegments`
  - segment mode: `fixed`, `stretch`, `hidden`
- Preserve old file loading:
  - old `verticalBorders`/`horizontalBorders` files load as v2
  - saving writes v2 using the same `.25slice.json` extension
- Replace fixed 5x5 layout:
  - arbitrary rows/columns from guide arrays
  - max practical grid: `100 x 100`
  - hidden segments render nothing and consume no target space
  - fixed/stretch distribution works per axis

## UI Implementation
- Replace hardcoded four-guide controls with dynamic X/Y guide lists.
- Add preview-driven editing:
  - drag existing guides
  - add X guide at cursor
  - add Y guide at cursor
  - remove selected guide
  - selected guide has stronger highlight
- Add selected guide/segment panel:
  - position in percent and pixels
  - segment mode picker: fixed/stretch/hidden
  - grid size display, for example `12 x 8 cells`
- Keep current 25-slice presets by converting them into v2 guide sets.

## AI and Validation
- Keep AI as review/suggest first, not automatic mutation.
- Update prompts/review to understand variable guide arrays and segment modes.
- Validate:
  - guide order
  - duplicate/near-duplicate guides
  - tiny segments
  - hidden segments
  - grid cap
  - fixed segments exceeding target size

## Test Plan
- Plan file exists at `docs/variable-slice-grid-plan.md`.
- Old 25-slice JSON upgrades to v2 correctly.
- V2 files round-trip guide arrays and segment modes.
- Existing 4x4 guides reproduce current 25-region output.
- Extra guides produce expected region counts.
- Fixed/stretch/hidden modes behave correctly.
- UI can construct with dynamic guide controls.
- AI parser/review accepts variable-grid suggestions and rejects unsafe ones.
- Debug and release EXEs build and launch.

## Assumptions
- Save location: `docs/variable-slice-grid-plan.md`.
- “Infinite” means user-facing unlimited guides with a hard safety cap of `100 x 100` cells.
- Per-cell freeform overrides are deferred; variable X/Y guides ship first.
- The file extension remains `.25slice.json`.
