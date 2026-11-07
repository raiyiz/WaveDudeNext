# Linux / Avalonia port

Run from the repository root (the folder containing `WaveDudeNext.sln`):

```sh
DOTNET_SYSTEM_NET_DISABLEIPV6=1 dotnet run --project WaveDudeNext.Avalonia
```

The workspace now starts with an editable project. Project tree is on the left, field and intensity are side by side in the middle, properties are on the right, and the frame timeline is below. The splitters resize the panes.

## Available

- New/open/save/save as; bundled examples under File > Examples; unsaved-change confirmation.
- Add sources, every optical-part kind, probes and colour schemes from Project. Duplicate/delete and move up/down from Edit. Tree names follow edits.
- WPF-derived editors for Project, Scene, Simulation, Display, Source, Lens/Mirror, Polygon, Rectangle, Ellipse, Triangle, Outline, Fibre (including nodes and handles), Bragg reflector, GRIN lens, Zone plate, XOR/AND/OR gates, Probe and Colour scheme.
- The shared WPF form layout, labels, tooltips, enum names, numeric limits and dependent-control rules, using Avalonia controls. RGB swatches open an RGB picker; colour stops can be added/removed or copied from another scheme.
- Select objects on either preview, drag to move, drag a selected part's circular rotation handle to rotate, or use arrow keys (Shift for 10 pixels). Geometry dimensions and path handles can also be edited numerically.
- Auto rerun debounces edits; turn it off to apply changes explicitly. Undo/redo uses the shared project history.
- Simulation run/pause/reset, frame capture in the shared RAM/disk frame store, scrubbing, playback, looping and playback speed. Display changes recolour stored frames through the shared colour renderer.
- Point/line probe recording and graph overlays using the shared probe sampler and graph layout, including logarithmic axes; selected-probe CSV export.
- Current field/intensity PNG export, scene-image embedding/extraction/removal, image composition with optical parts, external image-file refresh.
- Ctrl+N/O/S, Ctrl+Shift+S, Ctrl+Z/Y/D outside text editors, Delete outside text editors, F5/F6/F7.

## Remaining Windows parity

This is not yet a complete replacement for the WPF application. Still missing:

- Docking, floating/tabbed panes, workspace persistence, UI zoom, theme/language switching and full accessibility parity.
- WPF's full canvas manipulation system: shape-specific resize handles, path-handle dragging, multi-selection, pan/zoom and drag/drop.
- System clipboard cut/copy/paste, recent files, classic project import UI, integrated manual and diagnostics/about panes.
- Background run orchestration and cancellation, preview-quality settings/automatic tuning, memory-warning UI, independent probe-only reruns and all playback in/out controls.
- Video export, image sequences, OpenEXR export and rendering graph/marker decorations into exports. PNG export currently writes the displayed bitmap and its geometry edge overlay; canvas labels/graphs are separate visual overlays.
- WPF/WIC image-format coverage: the Linux image picker currently supports PNG/JPEG/BMP/GIF/WebP through Skia.

The Avalonia edits remain in `WaveDudeNext.Avalonia`; the WPF frontend and shared numerical library are unchanged.

## Verification

`_tools/avalonia_check` exercises compiled view bindings, all optical-part editors, conditional controls, image channels, history and serialization without a desktop. Its optional `--workspace` mode runs the actual window and checks object commands, timeline, probe recording and rendered editor templates in a display (or Xvfb). See its README for commands.
