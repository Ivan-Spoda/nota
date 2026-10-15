# Data model: Device categories in the browser

**Feature**: [spec.md](spec.md) · **Research**: [research.md](research.md) (R1, R2, R9)

These are in-memory and settings-level entities only. Nothing changes in the project
format, the C ABI or the engine.

## DeviceCategory (static, `Nota.Application/Palette/DeviceCategories.cs`)

| Field | Type | Rule |
|---|---|---|
| `Name` | string | Sentence case, unique within its tab. |
| `Builtins` | string[] | The catalogue names (`"Nota Reverb"`) of its built-in members. Must not be empty. |

Tab order (the chip order):

- **Instrument**: Synths · Keys & physical · Drums · Samplers · Racks
- **AudioEffect**: EQ & filter · Dynamics · Reverb & delay · Modulation · Distortion ·
  Pitch & time · Utility · Racks
- **MidiEffect**: Generate · Shape

`Other` is the fallback for a plug-in (or an unmapped built-in). Its chip comes last and
only exists when it has devices.

**Operations**:

- `Of(type)` and `Order(type)`: the tab's categories in order.
- `Position(type, name)`: the index, or `int.MaxValue`.
- `For(BuiltinDevice)`: the device's category, or `Other`.
- `ForPlugin(instrument, reported)`: the R2 token table, or `Other`.

**Invariants** (checked by `DesignTokenCheck.RunBrowser`, FR-009 / SC-001):

- every built-in device is in exactly one category of its own type;
- no stale names;
- no empty category;
- sentence-case names;
- 18 / 26 / 6.

## BrowserItem (existing)

| Member | Type | Rule |
|---|---|---|
| `DeviceCategory` | string (init) | Set on top-level device rows in `Rebuild()`: built-ins use `DeviceCategories.For`, plug-ins use `ForPlugin(isInstrument, Category)`. |

## Category filter (`BrowserViewModel`)

| Member | Type | Notes |
|---|---|---|
| `_categoryFilter` | `string[3]` | Per device tab (0 Instruments, 1 Audio effects, 2 MIDI effects); `""` means no chip. In memory only. |
| `CategoryFilter(int tab)` | string | The picked category, or `""`. |
| `SetCategoryFilter(int tab, string name)` | method | Sets or clears (`""`) the tab's pick and rebuilds that tab. |
| `CategoriesOn(int tab)` | `IReadOnlyList<string>` | The tab's order, plus `Other` when a device on the tab maps there. |

A device passes when the tab's filter is `""` or its shelf equals the filter. The shelf
is `DeviceCategory`, or `Other` when the tab doesn't list it.

## Settings (`Nota.Application/Settings.cs`)

| Property | Type | Default | Notes |
|---|---|---|---|
| `BrowserCategoryChips` | bool | `true` | Shows the category chip row. A missing key in an older `settings.json` resolves to `true` (FR-003). |

The headers-version keys `BrowserCategoryView` and `BrowserFoldedCategories` are removed.
They were never released.
