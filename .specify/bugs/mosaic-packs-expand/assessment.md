# Bug Assessment: Mosaic → Packs → a pack won't expand in the browser

- **Slug**: mosaic-packs-expand
- **Created**: 2026-10-11
- **Source**: pasted text
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

> Загруженные паки в браузере отображаются в Mosaic -> Packs, но их либо нельзя развернуть,
> или разворачивается, но не отображаются пресеты. И перетащить соответственно тоже нельзя.

## Symptom

In the Instruments tab, Nota Mosaic → Packs expands and lists the installed packs, but clicking a
pack's disclosure triangle does nothing visible: its presets never appear, so none can be dragged
onto a track. Expected: the pack expands to its presets, which drag like any other preset.

## Reproduction

1. Install at least one sample pack (Settings → Downloads) so Mosaic generates pack presets.
2. Browser → Instruments → expand Nota Mosaic → expand Packs: the packs are listed.
3. Click a pack's disclosure triangle (or the row): no presets appear.
4. (Collapsing and re-expanding Packs then shows the pack open — its state did flip, the list
   simply was never rebuilt.)

## Suspected Code Paths

- `src/managed/Nota.Presentation/ViewModels/BrowserViewModel.cs:424` `DeviceTabOf()` — decides
  which device tab to re-flatten after a toggle, but only checks a device's **direct** children
  (`d.Children.Contains(item)`). A pack folder is a grandchild (Mosaic → Packs → pack), so it
  returns -1.
- `src/managed/Nota.Presentation/ViewModels/BrowserViewModel.cs:399` `ToggleExpand()` — with -1 it
  falls into the Files/Presets branch: flips `IsExpanded`, records the path in `_expandedTreeKeys`,
  rebuilds the Files and Presets lists — but never `RebuildVisible(0)`, so the Instruments list
  is stale.
- `src/managed/Nota.Presentation/ViewModels/BrowserViewModel.cs:317-333` — builds the 3-level
  Packs tree (the only device-tab tree deeper than two levels); `AddPresetRows` (`:500`) already
  recurses correctly, so rendering is fine once rebuilt.
- `src/managed/Nota.App/BrowserView.cs:1186` `OnRowPressed` — drag is generic for `Preset` rows;
  nothing pack-specific blocks it. Drag fails only because the rows are never visible.

## Root Cause Hypothesis

`DeviceTabOf` assumes device-tab folders are at most one level deep. The Mosaic Packs tree added
a second folder level, so toggling a pack is misrouted to the Files/Presets refresh and the
Instruments list never re-flattens. Confidence: high.

## Proposed Remediation

**Preferred**: make `DeviceTabOf` search the whole subtree of each device (a recursive
`Contains` over `Children`), so any nested folder on a device tab returns its tab and
`ToggleExpand` calls `RebuildVisible(tab)`. Small, local, no API change.

**Alternatives**:
- Give `BrowserItem` a tab / owner field set at build time — avoids the tree walk but touches
  every place that constructs device-tab items.

**Files likely to change**:
- `src/managed/Nota.Presentation/ViewModels/BrowserViewModel.cs`
- `tests/Nota.SmokeTest/` (a browser test, via `BrowserTestSupport.cs`)

**Tests to add or update**:
- Build a `BrowserViewModel` with a fake `IMosaicPacks` returning two presets in one folder;
  expand Mosaic → Packs → the pack via `ToggleExpand` and assert both preset rows appear in
  `Instruments`; collapse and assert they're gone.

## Risks & Considerations

- Folder expansion on device tabs is not persisted across `Rebuild()` (they are all collapsed
  there), unlike Files folders; after the fix pack folders also won't land in `_expandedTreeKeys`
  — consistent with the other device-tab folders.
- Side note (not this bug): `MosaicLibrary.Presets()` parses every preset file on each browser
  `Rebuild()`; with many packs this could become slow.

## Open Questions

- None.
