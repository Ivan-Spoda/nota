# UI Contract: Return / Master footer row

The arrangement's pinned footer rows (`ArrangementView`, `FooterRowH = 40`,
`HeaderW = 228`). Ember tokens per [`DESIGN.md`](../../../DESIGN.md).

## Layout (header column, one row)

```text
┌─┬──────────────────────────────────────────────┬──┐
│ │ Reverb                                0.0    │▮▮│  ← name (11 SemiBold, TextPrimary) · dB (Mono 9, TextTertiary, 30 px, right-aligned)
│s│ ━━━━━━━━━━━━━━━━━━━━━━━●──────────────        │▮▮│  ← MiniFader, full width of the content column
└─┴──────────────────────────────────────────────┴──┘
 3px spine (track colour; Accent for master / selected)    MeterBar, vertical, Width 8, margin 0,6,3,6
```

- Content padding 8,4 so two lines fit in 40 px; the meter is 28 px tall.
- The name trims with an ellipsis before the dB read-out or meter shrink.
- Background: `Brush.SurfaceCard` (return), `Brush.SurfaceRaised` (master), selected →
  `SelHeaderBg` — unchanged.

## Behaviour

| Interaction | Return row | Master row |
|---|---|---|
| Click on the row (not the fader) | selects the return (device chain below) — unchanged | selects the master chain — unchanged |
| Right-click | Rename / Colour menu — unchanged | unchanged |
| Drag fader | `SetTrackVolume(returnId)`, dB follows | `Transport.MasterVolume`, dB follows; MST + Mixer follow |
| Double-click fader | reset to 0 dB | reset to 0 dB |
| Click meter | clears peak hold (MeterBar default) | same |
| Browser drop on the row | effect lands on that bus — unchanged | unchanged |
| Playback with return volume automation | fader + dB follow the live value | n/a (shows set volume) |
| Automation write | none | none |

## Callbacks added to views

```csharp
// ArrangementView and MixerView (same shape as SessionView already has)
public Func<float>?  GetMasterVolume { get; set; }
public Action<float>? SetMasterVolume { get; set; }
```

Wired in `MainWindow` to `vm.Transport.MasterVolume`.
