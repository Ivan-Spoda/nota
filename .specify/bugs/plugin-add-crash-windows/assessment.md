# Bug Assessment: Windows — adding a VST3 plugin (DecentSampler) crashes Nota 0.51.x

- **Slug**: plugin-add-crash-windows
- **Created**: 2026-10-13
- **Source**: pasted text (user comment + screenshot of the Visual Studio JIT "Unhandled exception" dialog)
- **Verdict**: likely valid, needs reproduction
- **Severity**: high

## Report (verbatim or summarized)

> братан, убери дебаг символы из релиз сборки, ну позор же какой-то... Размер только раздувается от этого.
> Похоже нужен хот-фикс. Каждый раз, когда добавляю плаг - получаю обращение к чужой памяти, версия новейшая (0.51.2):
> Вызвано исключение по адресу 0x00007FFB55F42C20 (DecentSampler.vst3) в Nota.App.exe: 0xC0000005: нарушение прав доступа при чтении по адресу 0x0000000000000008.
> Вызвано исключение по адресу 0x00007FFC13B1E6B0 (vcruntime140.dll) в Nota.App.exe: 0xC0000005: нарушение прав доступа при записи по адресу 0x0000000000000020.
>
> P.S скачал 0.51.1 - там так же. Работает только худо-бедно 0.50.1

Screenshot: VS JIT debugger dialog "Исключение не обработано", the `vcruntime140.dll` write AV at 0x20.

## Symptom

On Windows, adding a hosted plugin (DecentSampler.vst3 named; the reporter says "every time I add a plugin") crashes
Nota 0.51.1 / 0.51.2 with a native access violation: a first-chance AV inside DecentSampler (null+0x08 read), then an
unhandled AV in `vcruntime140.dll` (null+0x20 write — memcpy/memset into a null-based object). 0.50.1 "works more or less".
Expected: the plugin loads as an instrument/effect.

Secondary request: release builds should not ship debug symbols.

## Reproduction

1. Windows x64, install Nota 0.51.2 (installer from GitHub release). [NEEDS CLARIFICATION: Windows version, x64 vs arm64]
2. Install DecentSampler (VST3) [NEEDS CLARIFICATION: DecentSampler version; a sample library loaded or empty?], scan plug-ins.
3. Add DecentSampler — [NEEDS CLARIFICATION: how: browser drag to arrangement / double-click / command palette / drop onto existing track (replace) / into a Rack?]
4. Crash. [NEEDS CLARIFICATION: does it crash with *any* plugin, or only DecentSampler? at insert, or when the editor opens / first note plays?]

## Suspected Code Paths

Diff `v0.50.1..v0.51.2` touching the plugin path (native side changed little; the managed insert path was rewritten):

- `src/managed/Nota.Presentation/DeviceInsertService.cs` — **new in 0.51.1**; all browser/palette/drop inserts were moved
  here from `MainWindow.Dnd.cs`. Plugin creation (`AddPluginInstrumentTrack`, `SetTrackInstrumentPlugin`,
  `AddTrackEffectPlugin`) is now wrapped in `Group()` → `BeginUndoGroup/EndUndoGroup`, and the replace-in-place /
  placement (`Place`: Move/Remove device) logic changed. Same engine calls otherwise — needs runtime confirmation.
- `src/native/nota.engine/pluginhost/PluginHost.cpp` `PluginInstrument::noteExpression` — **new in 0.51.1** (MPE):
  pitch wheel / channel pressure / CC74 / CC64 / poly aftertouch now reach hosted plugins from the audio thread
  (`Engine_Render.cpp:87,505,726`). A plugin that mishandles these is plausible but would crash on note input, not on insert.
- `src/native/nota.engine/pluginhost/PluginHost.cpp` `PluginInstrument::prepare` — dereferences `plugin_` without a null
  check (the constructor guards `plugin_` elsewhere). Not a regression, but brittle.
- `src/native/nota.engine/pluginhost/PluginHost.cpp:273` `nota_pluginhost_plugin_desc` — gained a 5th field (category);
  parsed by `PluginCatalog.cs` / `PluginDescriptors.cs` (new palette). A catalog-index mismatch between the browser/palette
  rows and the native catalog would instantiate the wrong description — worth checking.
- `scripts/package-win.ps1` / `scripts/nota.iss:42` — the whole `dotnet publish` dir (`{#PubDir}\*`) goes into the
  installer, including the managed `*.pdb` files. Native `nota_engine.dll` is copied alone (no native PDB shipped).

Unchanged between versions: Windows CI toolchain (windows-2022, VS 2022, dynamic CRT), the JUCE instantiate path
(`instantiate()` / `createPluginInstrument` / `createPluginEffect`). 0.51.2 added FLAC/LAME, but 0.51.1 already crashes,
so the codecs are ruled out.

## Root Cause Hypothesis

**Low confidence.** The faulting frames are inside DecentSampler and the CRT, so Nota is most likely driving the plugin
into an invalid state rather than corrupting memory itself. The regression window (0.50.1 → 0.51.1) contains two
plugin-facing changes: (a) the rewritten insert flow in `DeviceInsertService` (undo grouping, placement/replace), and
(b) MPE expression forwarded as MIDI to hosted plugins. Neither is an obvious bug on read-through; the JIT dialog shows
an unhandled native SEH exception, which Nota cannot catch from managed code. A crash dump / call stack is needed to
tell whether the AV happens during instantiation (`createPluginInstance`/`prepareToPlay`), in `processBlock`, or in
the editor.

The "debug symbols" complaint is a misreading: the dialog is Visual Studio's JIT debugger, shown because VS is installed
on the reporter's machine — it would appear without any PDBs. Still, the installer *does* ship the managed `.pdb` files
(small, a few MB); excluding them is a cheap, legitimate cleanup.

## Proposed Remediation

**Preferred**:
1. Reproduce on a Windows x64 box with DecentSampler 1.x: add it via browser drag, double-click, palette, and as a
   replace-in-place, on 0.50.1 and 0.51.2; capture a full dump (`procdump -e -ma Nota.App.exe` or WER LocalDumps) and
   open it with JUCE + nota_engine symbols from a local RelWithDebInfo build to get the real stack.
2. Fix at the point the stack shows. If it is the insert flow, align `DeviceInsertService` with the 0.50.1 call order;
   if it is MPE forwarding, send expression to plugins only when the clip/input actually carries it (and never CC64 /
   wheel resets on idle); if it is instantiate/prepare, harden `prepare()` (null check, call `prepareToPlay` on the
   message thread only, set the bus layout before `setPlayConfigDetails`).
3. Packaging: stop shipping managed PDBs in Release — `-p:DebugType=none -p:DebugSymbols=false` on `dotnet publish` in
   `scripts/package-win.ps1` (and mac/linux siblings), or exclude `*.pdb` in `scripts/nota.iss`. Keep producing symbols
   as a separate CI artifact so field crashes stay debuggable.

**Alternatives**:
- Out-of-process plugin hosting (sandbox) so a plugin crash doesn't take Nota down — the right long-term answer, but a
  large architectural change, not a hotfix.
- Wrap instantiate/prepare in SEH (`__try/__except`) on Windows and report "plugin failed to load" — masks some crashes
  at instantiation only; leaves the process in an unknown state; acceptable only as a stopgap.

**Files likely to change**:
- `src/native/nota.engine/pluginhost/PluginHost.cpp`
- `src/managed/Nota.Presentation/DeviceInsertService.cs` (if the stack points there)
- `scripts/package-win.ps1`, `scripts/package-linux.sh`, `scripts/bundle-mac.sh` (PDB exclusion)
- `CHANGELOG.md`

**Tests to add or update**:
- Smoke test (`scripts/build.sh`): instantiate a hosted test plugin, add it via the same engine call sequence
  `DeviceInsertService` uses (undo group + clip), render blocks with MPE expression events, then remove/undo.
- Managed test on `DeviceInsertService` asserting the engine call order for a plugin instrument insert.
- Packaging check: the publish dir contains no `*.pdb`.

## Risks & Considerations

- Without a Windows repro the fix is speculative; a blind hotfix risks shipping nothing useful.
- Dropping PDBs everywhere makes user crash reports harder to symbolize — keep them as CI artifacts.
- Changing MPE forwarding affects plugins that *do* use wheel/aftertouch (behaviour change for MPE users).

## Open Questions

- [NEEDS CLARIFICATION: any plugin, or only DecentSampler? Which DecentSampler version?]
- [NEEDS CLARIFICATION: how the plugin is added (drag / double-click / palette / replace / Rack)?]
- [NEEDS CLARIFICATION: crash at the moment of adding, or on first note / editor open?]
- [NEEDS CLARIFICATION: a MIDI controller (MPE or not) connected?]
- [NEEDS CLARIFICATION: can the reporter share a dump or the VS "Show call stack" output (button on the same dialog)?]
