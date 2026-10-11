# Quickstart: validate MP3/FLAC export end-to-end

**Feature**: specs/001-mp3-flac-export | **Prereqs**: macOS build host (Linux/Windows siblings exist), .NET 10 SDK, CMake ≥ libFLAC 1.5's minimum, Ninja.

## 1. Build with the new codecs

```sh
git submodule update --init --recursive   # picks up vendor/flac
scripts/build.sh                          # native (now with libFLAC + LAME) + managed + smoke test
```

The smoke run itself already proves most of the feature (see step 3). Expected:
green, including the new export block.

## 2. Manual UI check (dialog matrix)

```sh
dotnet run --project src/managed/Nota.App
```

With any project that plays audio: `⌘⇧E`, then walk the matrix from
`contracts/ui-export-dialog.md`:

1. Format = FLAC → depth list is 16/24; export → file lands as `*.flac`,
   footer shows `≈` estimate; plays in the OS player.
2. Format = MP3 → quality combo 320/256/192/128; 96 kHz greyed; dither greyed;
   export at 320 → `*.mp3`, size ≈ 40 MB per hour × … (≈ 2.4 MB/min).
3. Normalize on + FLAC → result quieter, peaks ≈ −1 dBTP.
4. Stems + MP3 → folder of per-track `*.mp3`.
5. WAV regression: export WAV with old defaults → byte-comparable behaviour to
   before the change (same dialog path, same file).

Screenshots per the UI verify recipe (env-guarded hook + `screencapture`,
`NOTA_DATA_DIR` isolation) for the three format states of the dialog.

## 3. Smoke test gates (already run by `scripts/build.sh`)

`tests/Nota.SmokeTest` default suite; the new block checks, in order
(definitions in `research.md` D7):

- FLAC 24-bit round-trip: exported file decodes (dr_flac via
  `NotaEngine.DecodeMono`) back to the rendered reference within quantization.
- MP3 320 CBR: decodes audible; duration within ±100 ms; size within ±10 %.
- FLAC stems: one decodable file per track.
- Normalize → FLAC peak ≤ 0.8913.
- MCP `export_master(format: "flac")` writes a decodable file.
- Encoder failure path: open on an invalid path → null; discard + delete ok.

## 4. Cross-platform confidence

- `scripts/build-linux.sh` on a Linux box/CI (gcc, ALSA toolchain as usual).
- `scripts/build-win.ps1` in a VS 2022 developer shell; CI package job
  (`package-win.ps1`, VS generator x64/arm64) must produce `nota_engine.dll`
  with the encoders linked.

## Expected outcome

All four user stories demonstrably work: FLAC archive (lossless re-import),
MP3 share (small, plays everywhere), stems in both formats, MCP/scripted
export — with the WAV path unchanged.
