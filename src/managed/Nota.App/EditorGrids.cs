// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// The live snap-grid settings of the two editors plus the piano roll's Quantize step
// (specs/003-adaptive-snap-grid). App-wide rather than per view: the piano roll is rebuilt
// for every clip and can float in the detail window, and its grid must not reset when that
// happens. Loads from and saves to Settings (settings.json), never to a project; every
// change raises Changed so views redraw and snap chips relabel.

using System;
using Microsoft.Extensions.DependencyInjection;
using Nota.Application;
using Nota.Application.Editing;

namespace Nota.App;

public enum EditorKind { Arrangement, PianoRoll }

public sealed class EditorGrids
{
    private static EditorGrids? s_shared;

    /// <summary>The app's one instance, on the registered settings service.</summary>
    public static EditorGrids Shared => s_shared ??= new EditorGrids(App.Services.GetRequiredService<ISettingsService>());

    private readonly ISettingsService _settings;

    public EditorGrids(ISettingsService settings) => _settings = settings;

    public event Action<EditorKind>? Changed;

    public GridSetting Get(EditorKind kind) => (kind == EditorKind.Arrangement
        ? _settings.Current.ArrangementGrid : _settings.Current.PianoRollGrid) ?? Default(kind);

    public void Set(EditorKind kind, GridSetting value)
    {
        if (value == Get(kind)) return;
        if (kind == EditorKind.Arrangement) _settings.Current.ArrangementGrid = value;
        else _settings.Current.PianoRollGrid = value;
        _settings.Save();
        Changed?.Invoke(kind);
    }

    /// <summary>The piano roll's Quantize step (also the clip tools' grid).</summary>
    public GridStep Quantize => _settings.Current.QuantizeStep.Normalize();

    public void SetQuantize(GridStep step)
    {
        step = step.Normalize();
        if (step == Quantize) return;
        _settings.Current.QuantizeStep = step;
        _settings.Save();
        Changed?.Invoke(EditorKind.PianoRoll);
    }

    private static GridSetting Default(EditorKind kind)
        => kind == EditorKind.Arrangement ? GridSetting.DefaultArrangement : GridSetting.DefaultPianoRoll;
}
