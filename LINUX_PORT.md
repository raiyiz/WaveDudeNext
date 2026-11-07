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
- Full-resolution current field/intensity PNG export, scene-image embedding/extraction/removal, image composition with optical parts, external image-file refresh.
- Ctrl+N/O/S, Ctrl+Shift+S, Ctrl+Z/Y/D outside text editors, Delete outside text editors, F5/F6/F7.

## Desktop and export parity

- WPF-style Project pane with Source/Probe add buttons, a complete Part menu, folder plus buttons and per-object up/down/remove controls. Boundary arrows disable automatically; row commands support undo. Checkboxes precede the matching object icons and control source/part enablement or probe recording. The default layout gives the tree more room; existing saved layouts retain their widths.
- Vector icons on menus, toolbars, playback controls, the project tree and editor buttons. Optical-part drawings are copied from WPF; command icons use portable vectors rather than Windows-only icon fonts.
- System clipboard cut/copy/paste with Ctrl+X/C/V, multiple object selection, group moves, duplication and deletion. Text editors retain their native keyboard shortcuts. Ctrl-click canvas objects to extend/toggle selection; the tree also supports multiple selection.
- Drag project objects from the tree onto a preview to place them, or onto another tree row to reorder them. Hold Ctrl to copy. Drop project files, classic configuration JSON, scene images or copied object XML onto the workspace.
- Recent-project menu, classic project import with conversion notes, integrated manual (F1), and About/diagnostics.
- Drag a pane tab to any other pane: the centre adds a tab; an edge creates a horizontal/vertical split with a splitter. Release outside the targets to float it; drag a floating tab back to dock. Double-click a tab to float, right-click for keyboard-accessible docking commands, or press Escape during a drag to cancel. View > Panes can show hidden panes, dock all panes or reset the layout. Split ratios, active tabs, floating positions, UI scale, theme and language persist in `WaveDudeNext/desktop.json` under the user's local application-data directory.
- Dark/light/system themes and 80–150% UI scale apply to docked and floating editors. English/German/French/Dutch resources cover the editor and desktop menus, docking commands and dialog labels. Editor labels are exposed to accessibility tools; previews and probe panes have named automation peers. Tab cycles selected geometry handles, arrows edit them, Shift increases the step, Escape leaves handle editing and Ctrl+Tab leaves the preview.
- Wheel zoom, middle-button pan and View > Fit previews. Selected parts have dimension handles; lens surfaces can be pulled while keeping their rims fixed, linked rectangles/ellipses resize together, and source arrays have rotation/spacing handles. Shift snaps rotation to 15°. Outlines/fibres have draggable points/control handles, with fibre constraints preserved. Escape cancels a geometry drag. Numeric editors remain available for every setting.
- Each preview has a **Drawings** toggle that hides all annotations, including selected-object handles and labels, without hiding the simulation. **Overlay options…** controls sources, part outlines, probe markers, object labels, probe graphs and geometry edge glow, with individual object/graph checkboxes and **Show all drawings** for recovery. Right-click a source/part/probe marker to hide its drawing, or use the × on a canvas graph to hide just that graph. Category choices persist independently for field/intensity; individual hide choices apply to the current project session. These controls do not disable sources/parts/probes, affect recording, change export decoration settings or add project undo steps.
- Drag a probe graph to move its box without moving the measuring point. Drag its bottom-right grip to resize, with preset snapping; move the graph near the probe to attach it. Double-click the graph, use its top-right pop-out control or choose View > Panes > Probe viewports to open a separately dockable graph. New graph panes receive separate layout space below intensity (or a tab with other graph panes), and their canvas copy is hidden automatically. The graph pane’s context menu has **Show graph over simulation**, and the preview’s overlay options can restore individual canvas graphs independently. Probe panes follow scrubbing and playback, can float/dock like other panes, and offer CSV export through their context menu. Arrow keys scrub a focused probe pane; Shift moves ten frames. On a canvas graph, arrows move the box and Ctrl+arrows resize it.
- Simulation batches run on a worker; pause cancels the active batch, and reset/rebuild/close wait for engine ownership before replacing resources. Preview presets offer full 16-bit, balanced 8-bit, or half-resolution/alternate-frame 8-bit capture. Automatic selects a preset from available physical memory. The timeline reports RAM/disk use and warns when disk caching is required.
- The timeline has a numbered full-run ruler, a playhead label, a hover-frame label, blue draggable in/out brackets and shading for frames not computed yet. Frame numbers are zero-based. Enter an exact computed frame beside the transport buttons; the adjacent readouts show the last frame and the simulation step. Run restarts from frame zero (F5); simulation pause/resume (F6) is separate from playback play/pause (Space). Comma/period step back/forward, I/O mark the range, X clears it, L toggles looping and F follows the newest frame. On the ruler, arrows step one frame, Page Up/Down ten, Home/End jump to range boundaries, and the mouse wheel steps frames. Speed can be typed or selected from presets and changes during playback. The right-hand buttons show/hide field and intensity or arrange them side by side/stacked. In/out commands also remain available under Simulation. Simulation > Re-record probes generates fresh full-quality recordings while retaining preview frames.
- File > Export recording writes the playback range to PNG/JPEG/OpenEXR sequences, AVI, MPEG-1 or MOV (Animation/PNG), with progress and cancellation. OpenEXR can store intensity as I or √I. Reduced previews automatically rerun at full quality for export. Graphs/markers follow the project's export settings.
- Linux MP4 export uses an optional system `ffmpeg` with H.264 encoding. Other video/sequence encoders are built into the shared library.
- PNG/JPEG/BMP/GIF/WebP scene decoding uses Skia. When Skia cannot decode an image, an optional system ImageMagick (`magick` or `convert`) converts it to straight-alpha PNG; TIFF is covered by the verification harness when ImageMagick is installed. Other codecs, including JPEG XR, depend on that installation's delegates.

## Timeline, export and platform notes

- Reduced-quality previews keep full timeline numbering. Missing captures repeat the preceding stored frame; playback and in/out points use the same numbering at every quality. The timeline also reports the actual stored-frame count.
- Single-frame PNG and recording exports regenerate full-resolution, half-float frames when the preview is reduced. Export decorations use the project’s export flags. Floating editors are disabled during modal export/run operations to keep the project stable while a worker reads it.
- Installed image codecs remain platform-dependent. Skia handles the common formats and ImageMagick provides its available delegates; Windows-installed WIC codecs are not shipped with the Linux application. MP4 requires an ffmpeg build with H.264 support.
- Automated checks cover accessibility names, form label associations and keyboard geometry editing. Live screen-reader behavior, compositor-specific window placement and native file pickers still need testing on the target desktop.

Changes are confined to the Avalonia frontend, its verification harness and documentation; the WPF frontend and shared numerical library are unchanged.

## Verification

`_tools/avalonia_check` exercises compiled view bindings, all optical-part editors, conditional controls, image channels, history and serialization without a desktop. Its optional `--workspace` mode runs the actual window and checks object commands, timeline, probe recording and rendered editor templates in a display (or Xvfb). See its README for commands.
