// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Empty plugin catalog / preset library, so a BrowserViewModel can be built over the
// factory catalog alone (no scanned plug-ins, no presets folder).

using Nota.Application;

namespace Nota.SmokeTest;

internal sealed class EmptyPluginCatalog : IPluginCatalog
{
    public int Scan(string workerPath) => 0;
    public int Count => 0;
    public string? Description(int index) => null;
    public string? Id(int index) => null;
    public int IndexOfId(string identifier) => -1;
    public void AddScanPath(string dir) { }
    public void RemoveScanPath(int index) { }
    public int ScanPathCount => 0;
    public string? ScanPath(int index) => null;
}

internal sealed class EmptyPresetLibrary : IPresetLibrary
{
    public IReadOnlyList<PresetInfo> List(string folder) => Array.Empty<PresetInfo>();
}

/// <summary>A fixed list of Mosaic pack presets, for the browser's Nota Mosaic → Packs tree.</summary>
internal sealed class FakeMosaicPacks(params MosaicPackPreset[] presets) : IMosaicPacks
{
    public string Root => "";
    public IReadOnlyList<MosaicPackPreset> Presets() => presets;
    public event Action? Changed { add { } remove { } }
    public Task<int> ScanAsync(CancellationToken ct = default) => Task.FromResult(0);
    public string Save(Nota.Application.Mosaic.MosaicProgram program, string name, string folder, IReadOnlyDictionary<string, float>? param = null) => "";
    public string ApplyInPlace(IAudioEngine engine, string path, int trackId) => "";
    public Nota.Application.Mosaic.MosaicProgram? Read(string path) => null;
    public string ToRef(string path) => path;
    public string? Resolve(string reference) => null;
    public double? DetectNote(string path) => null;
}
