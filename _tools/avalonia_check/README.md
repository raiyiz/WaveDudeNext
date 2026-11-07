# Avalonia checks

From the repository root:

```sh
dotnet build _tools/avalonia_check/AvaloniaCheck.csproj -c Release -m:1
dotnet _tools/avalonia_check/bin/Release/net10.0/AvaloniaCheck.dll
```

The checks exercise the compiled editors and bindings, every optical-part kind, conditional controls, reseeding, BGRA scene decoding/composition, image removal, history, serialization and source-image refresh without opening a desktop window.

To test the actual workspace, object commands, stored-frame scrubbing, probe recording and rendered editor templates in a virtual Linux display:

```sh
xvfb-run -a dotnet _tools/avalonia_check/bin/Release/net10.0/AvaloniaCheck.dll --workspace
```

It also checks overlay hide/show rendering (including selected graphics), category/individual visibility, unchanged solver data/history, separate probe placement and independent canvas restoration, vector icons, clipboard paste/undo, arbitrary split/tab docking, floating probe viewports and serialized layout restoration, Escape cancellation, themes, actual translated editor labels, accessibility labels and keyboard geometry editing, UI scaling, background stepping, full-numbered reduced-quality timelines, full-resolution still exports, full-quality export reruns, video/image-sequence encoders, and optional ffmpeg MP4 / ImageMagick TIFF support. Settings are isolated in a temporary directory.

This mode also fails on binding warnings/errors and writes `/tmp/wavedude-linux-workspace.png`. When `xdotool` is installed, this mode also sends real mouse drags under Xvfb to test pane tabbing, floating, redocking, probe-box movement/resizing and cancellation, plus numbered-ruler scrubbing and both range brackets. It checks exact-frame entry, playback range/looping, live-follow, speed presets, simulation restart/pause/resume, accessible seeking and picture layout controls. It does not automate native file-picker dialogs or a live screen reader.

Project-pane checks exercise toolbar and folder additions, all Part menu entries, row/toolbar reordering, boundary arrows, row removal and undo, and probe activity checkboxes. A second screenshot is saved to `/tmp/wavedude-linux-project-tree.png`.

Brightness checks cover independent pane gains, synchronization with Display settings, WPF percentile fits for both layers, retained half-precision recordings, and the unclipped fit fallback for indexed previews.
