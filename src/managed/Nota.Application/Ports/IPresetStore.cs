// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.

namespace Nota.Application;

/// <summary>Save/apply of a single device or instrument preset. Listing presets for
/// the browser is <see cref="IPresetLibrary"/>.</summary>
public interface IPresetStore
{
    /// <summary>Loads a preset file and applies it to a track. Returns a warning
    /// string ("" = clean).</summary>
    string ApplyFromFile(IAudioEngine engine, string path, int targetTrackId);

    /// <summary>Loads an instrument preset file into a track's instrument in place, swapping
    /// the instrument first when the preset is for another one (<paramref name="swapped"/>).
    /// Null — nothing done — when the file isn't an instrument preset; else a warning ("" = clean).</summary>
    string? ApplyInstrumentInPlace(IAudioEngine engine, string path, int trackId, out bool swapped);

    /// <summary>Captures a track's device/instrument as a preset and saves it to
    /// <paramref name="folder"/>. Returns false if there was nothing to capture
    /// (e.g. the built-in synth has no state).</summary>
    bool Save(IAudioEngine engine, int trackId, int deviceIndex, string displayName, string folder);

    /// <summary>Captures a rack chain's instrument as a preset (hosted plugins only).
    /// Returns false if there was nothing to capture (built-in chain instrument).</summary>
    bool SaveRackChainInstrument(IAudioEngine engine, int trackId, int chain, string displayName, string folder);
}
