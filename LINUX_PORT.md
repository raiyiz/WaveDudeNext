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

## Desktop and export parity

- Vector icons on menus, toolbars, playback controls, the project tree and editor buttons. Optical-part drawings are copied from WPF; command icons use portable vectors rather than Windows-only icon fonts.
- System clipboard cut/copy/paste with Ctrl+X/C/V, multiple object selection, group moves, duplication and deletion. Text editors retain their native keyboard shortcuts. Ctrl-click canvas objects to extend/toggle selection; the tree also supports multiple selection.
- Drag project objects from the tree onto a preview to place them, or onto another tree row to reorder them. Hold Ctrl to copy. Drop project files, classic configuration JSON, scene images or copied object XML onto the workspace.
- Recent-project menu, classic project import with conversion notes, integrated manual (F1), and About/diagnostics.
- View > Panes floats/docks individual panes and switches previews between tabs and side-by-side views. Window dimensions, pane widths, floating positions, preview layout, UI scale, theme and language are saved in the user's local application-data directory under `WaveDudeNext/desktop.json`.
- Dark/light/system themes, 80–150% UI scale, and the WPF English/German/French/Dutch editor resources. Some newly introduced command/dialog labels remain English.
- Wheel zoom, middle-button pan and View > Fit previews. Selected parts have dimension handles; polygons have a radius handle, and outlines/fibres have draggable points/control handles. The properties pane retains numeric access to every geometry setting.
- Simulation batches run on a worker; pause cancels the active batch, and reset/rebuild/close wait for engine ownership before replacing resources. Preview presets offer full 16-bit, balanced 8-bit, or half-resolution/alternate-frame 8-bit capture. Automatic selects a preset from available physical memory. The timeline reports RAM/disk use and warns when disk caching is required.
- Playback in/out points and range reset live under Simulation. Simulation > Re-record probes generates fresh full-quality recordings while retaining preview frames.
- File > Export recording writes the playback range to PNG/JPEG/OpenEXR sequences, AVI, MPEG-1 or MOV (Animation/PNG), with progress and cancellation. OpenEXR can store intensity as I or √I. Reduced previews automatically rerun at full quality for export. Graphs/markers follow the project's export settings.
- Linux MP4 export uses an optional system `ffmpeg` with H.264 encoding. Other video/sequence encoders are built into the shared library.
- PNG/JPEG/BMP/GIF/WebP scene decoding uses Skia. When Skia cannot decode an image, an optional system ImageMagick (`magick` or `convert`) converts it to straight-alpha PNG; TIFF is covered by the verification harness when ImageMagick is installed. Other codecs, including JPEG XR, depend on that installation's delegates.

## Remaining differences from Windows

- Panes float and dock through commands; WPF's arbitrary drag-to-dock layout system is not reproduced.
- Dimension and path handles cover common edits; specialised WPF handles such as lens curvature and source-array manipulation still use the numeric editors.
- Reduced-quality previews show stored-frame numbering, rather than WPF's full timeline with repeated stand-in frames. Current-frame PNG export uses the displayed preview resolution; recording exports rerun at full quality.
- Full screen-reader/accessibility parity, translation of all new shell/dialog text, and every Windows-installed WIC codec are not established. Native file pickers, pointer dragging and assistive technologies require manual testing.

Changes are confined to the Avalonia frontend, its verification harness and documentation; the WPF frontend and shared numerical library are unchanged.

## Verification

`_tools/avalonia_check` exercises compiled view bindings, all optical-part editors, conditional controls, image channels, history and serialization without a desktop. Its optional `--workspace` mode runs the actual window and checks object commands, timeline, probe recording and rendered editor templates in a display (or Xvfb). See its README for commands.
