# WaveDude Next manual

<!-- The figures are drawn by _tools/ui_check/Screenshots.ps1 from the example projects; the numbers in the lists are the badges of the figure above them. -->

WaveDude Next simulates waves on a two-dimensional canvas. You place sources, put lenses, mirrors, fibres and other optical parts in their way, and watch the wave field and the intensity build up frame by frame. Probes measure the wave at a point or along a line, and a finished run can be exported as a video, as an image sequence or as raw data.

For a first impression, open one of the projects under File > Examples and press Play (Space). The pictures follow the run while it is computed; drag the playhead on the timeline to look at any frame, and change a source or a part to see the run start over with your change.

This manual is also in the program: Help > Manual or F1 opens it, the contents on the left take you to a section, Esc closes it.

## The main window

![main-window](Assets/main-window.svg)

The main window with an example project at frame 1200.

1. **Menu bar**: The File, Edit, View, Simulation and Help menus; see [The menus](#the-menus). Press F10 or Alt to use them from the keyboard.
2. **Project pane**: The tree of everything in the project: the scene and the settings at the top, then the sources, the optical parts, the probes and the colour schemes. Click a row to edit it; see [The project tree](#the-project-tree).
3. **Properties pane**: The form of the selected object. Hover over a box for a short explanation of the setting.
4. **Field picture**: The wave field at the frame shown: the displacement of the wave at every pixel, positive and negative in the colours of the field's colour scheme.
5. **Intensity picture**: The intensity: the wave's energy averaged over the last steps, so it shows where the light goes, much as a long exposure would.
6. **Header strip of a picture**: The title, the brightness box and Auto, which fits the brightness to the frame shown; see [The preview](#the-preview).
7. **Timeline**: Your position in the run. Click or drag to scrub; drag a bracket to move the start or end of the export range. Left/Right step a frame, PageUp/PageDown ten, Home/End jump to the start/end of the range. See [The timeline](#the-timeline).
8. **Transport controls**: Run, play and pause, frame steps, the in and out points, loop and live, the position as frame and step, the playback speed, and which pictures are shown and how.
9. **Status bar**: Shows the last entry in the log. Double-click opens the Messages pane. Colour changes for warnings and errors.

The panes can be arranged as you like: drag a pane by its title to another side of the window, or use the controls at the right of its title to move or close it; View > Panes brings a closed pane back. The arrangement is kept for the next start, as are the theme, the language and the preview settings of the View menu.

Everything can be done from the keyboard: Tab moves through the controls of the panes and the handles on the pictures, the arrow keys move within the tree and along the timeline and nudge a handle, and every menu entry shows its shortcut.

### The project tree

![project-tree](Assets/project-tree.svg)

The Project pane with the example project.

1. **Pane title**: You can reposition the pane by dragging it. The controls on the right allow you to move or close the pane.
2. **Add a source**: Add a source to the project. It lands at the centre of the canvas shown; drag it from there or type its position. See [Sources](#sources).
3. **Add a probe**: Add a probe to the project, at the centre of the canvas as well. See [Probes](#probes).
4. **Add a part**: Shows a submenu of possible optical parts to add to the project. See [Optical parts](#optical-parts).
5. **Move the selected object up**: Move up (Alt+Up). Rows can also be dragged to reorder them.
6. **Move the selected object down**: Move down (Alt+Down).
7. **Remove the selected object**: Remove (Del).
8. **Project**: The root of the current project. Its form holds the random seed and a description; see [Project settings](#project-settings).
9. **Scene**: Configure scene properties, such as the size, the edge configuration and damping, the overall refractive index, and scene sources.
10. **Simulation**: Configure simulation properties, such as the length of the simulation to run and the wave speed.
11. **Display**: Configure various properties of the appearance of the preview, the simulation render and export.
12. **Sources**: Sources emit waves of a specified wavelength and amplitude. The group holds every source of the project.
13. **Expand and collapse tree groups**: The chevron opens or closes a group.
14. **Add to this group**: Adds a new item to the group, the same as the buttons above.
15. **Enabled**: Includes the object in runs: a source emits, a part is painted into the scene, a probe measures. Space on the row toggles it; a disabled object stays in the project, greyed.
16. **Move up**: Moves the row up within its group (Alt+Up).
17. **Move down**: Moves the row down (Alt+Down).
18. **Remove**: Removes the object (Del).
19. **Parts group**: The optical parts: lenses and mirrors, shapes, fibres, Bragg reflectors, Fresnel lenses, GRIN lenses, zone plates, diffusers, interference filters, diffraction gratings and logic gates, all painted into the scene.
20. **Probes group**: The probes, which measure the field or the intensity at a point or along a line and draw a graph over the pictures.
21. **Colour schemes group**: The colour schemes kept in this project; the Display settings pick one for each picture. See [Colour schemes](#colour-schemes).

You can select several rows: Ctrl+click adds a row or removes it (Insert or Ctrl+Space does the same for the focused row), Shift+click or Shift with the arrow keys selects a range, Ctrl+A selects everything. Remove, Duplicate and Enabled then act on the whole selection, and dragging one of the selected handles on the pictures moves them all together.

The row's context menu has the same commands as the buttons and more: Add source, Add part and Add probe; Add to or remove from selection and Select all; Enabled, Duplicate (Ctrl+D), Cut, Copy, Paste and Remove; Move up and Move down; and on a probe, Export probe data as CSV.

Objects can be dragged: drag a row within its group to reorder, onto a picture to put a copy where you drop it, or into the tree of another WaveDude Next window to copy it there. Cut, copy and paste (Ctrl+X, Ctrl+C, Ctrl+V) work on objects too, and the clipboard holds them as text, so a source can be pasted into a text editor and back. A pasted object lands behind the selected row and is renamed only when its name is already taken. A project file dropped anywhere on the window opens it.

Every change can be undone (Ctrl+Z) and redone (Ctrl+Y or Ctrl+Shift+Z) without limit, moves on the pictures included; a text box keeps its own undo while you type in it.

### The preview

![preview](Assets/preview.svg)

The preview: the field and intensity pictures of the example project at frame 1200.

1. **Field picture**: The field at the frame shown. The pictures are scaled to fit the pane. A thin line around a picture shows what a wave meets at the edge of the canvas: red for a reflecting wall, blue for a damping rim outside the canvas, nothing for an open edge.
2. **Brightness of the field picture**: How bright the field is drawn: a factor on the values before they are coloured. Type a number, or step it with the keys: Up and Down by two percent, PageUp and PageDown by a fifth.
3. **Auto brightness**: Fits the brightness once to the frame shown: the brightest half percent of it reaches the top of the colour scheme. The brightness then stays as it is.
4. **Intensity picture**: The intensity at the frame shown.
5. **Brightness of the intensity picture**: The same for the intensity.
6. **Auto brightness of the intensity picture**: The same for the intensity.
7. **Splitter between the pictures**: Drag or use the arrow keys to resize; double-click or Enter resets to half.
8. **Source handle**: A source on the pictures, here the one named Vertical source. Drag the handle to move the source; with the keyboard focus the arrow keys move it by a pixel, ten with Shift. A composite source also shows its emitters as dots and a lever to turn the array; Shift snaps it to 15 degrees.
9. **Part handle**: A part, here the mirror named Mirror background. Drag to move it, the arrow keys nudge it. A selected part also shows a lever for its angle and, by its kind, grips to resize it, the two surface handles of a lens, the points of an outline or the nodes and handles of a fibre, all of them draggable and reachable with Tab.
10. **Probe handle**: A probe, here the end A of a line probe. Drag to move it, the arrow keys nudge it. A probe appears on the picture of the layer it measures, a source on both.
11. **Probe graph box**: The graph of a probe over the picture. Drag it to move it, drag its corner to resize it; with the keyboard focus the arrow keys move it by a pixel, ten with Shift. A box left on the probe's point moves with the probe.

Click a handle to select its object; the tree and the Properties pane follow. Click on empty space to deselect, or drag there to select everything inside the rectangle, Shift adding to the selection and Ctrl toggling it; Ctrl+click adds or removes a single object. Del removes the selected objects. Esc during a drag puts everything back where it was. The Display settings have Handles in the preview and Labels in the preview, which hide the handles or the names of everything but the selected object.

A picture changes only when a frame for your position is ready; until then the last picture stays. When you edit the scene, a source or a part, the frame you are on is computed afresh with your change and shown as soon as it is ready, and a new run starts by itself a moment after your last change (Simulation > Re-run automatically after edits). A long wait dims the pictures under a spinner. Moving a probe or changing its graph needs no new run.

Right-click a picture for Brightness, Auto brightness, Export this frame and the exports of the run; see [Export](#export).

### The timeline

![timeline](Assets/timeline.svg)

The timeline with the playhead and the in and out points, and the transport controls.

1. **Playhead**: The position shown. Drag it, or click anywhere on the timeline to jump there.
2. **In point**: The start of the export range; drag the bracket to move it.
3. **Out point**: The end of the export range.
4. **Run the simulation**: Runs the simulation from the start (F5): every frame of the project as it is now. The pictures follow the run while it computes.
5. **Pause the simulation**: Pauses the run, and resumes it (F6).
6. **Play**: Plays the frames at the playback speed (Space). When there is no run yet, or the project has changed since the last one, Play starts a run and follows it.
7. **Pause**: Pauses playback (Space).
8. **One frame back**: Previous frame (,).
9. **One frame forward**: Next frame (.).
10. **Set the in point**: Starts the export range at this frame (I).
11. **Set the out point**: Ends the export range at this frame (O).
12. **Clear the in and out points**: Clears the export range: the whole run is exported (X).
13. **Loop**: Loops playback over the export range (L).
14. **Live**: Follows the newest frame while the run is in progress (F); scrubbing ends it.
15. **Position**: The frame shown and the last frame of the run.
16. **Step of the position**: The simulation step of the frame shown. A frame is kept every so many steps, as set by Visualize every in the Simulation settings.
17. **Playback speed**: Pick a factor or type one.
18. **Show the field**: Shows or hides the field picture.
19. **Show the intensity**: Shows or hides the intensity picture.
20. **Pictures side by side**: Field and intensity side by side.
21. **Pictures stacked**: Field above intensity.

The letter shortcuts work while the preview pane has the focus. The in and out points are the From step and To step of the Display settings, so they are saved with the project; the range is what File > Export writes and what Loop plays.

## The menus

The menu bar holds the File, Edit, View, Simulation and Help menus. Press F10 or Alt to open them from the keyboard; the entries show their shortcuts. The Add part menu of the project tree is described with the [optical parts](#optical-parts).

### The File menu

![menu-file](Assets/menu-file.svg)

The File menu.

1. **New Project**: Starts a new, empty project (Ctrl+N): a blank canvas with open edges and a damping rim around it, no sources yet.
2. **Open Project...**: Opens a project file (Ctrl+O): a WaveDude Next project (.wdproj), or a configuration.json of Wave Dude 2D, which is imported.
3. **Recent Projects**: The projects you opened last.
4. **Examples**: The example projects shipped with the program; see [Examples](#examples).
5. **Save**: Saves the project (Ctrl+S). The scene image is embedded in the file, so a project is one file.
6. **Save As...**: Saves the project under a new name (Ctrl+Shift+S).
7. **Export**: Videos, image sequences and data of the run; see [Export](#export).
8. **Export last**: Repeats the last export of a run in this session (Ctrl+E): the same pictures, format and file, without a dialog; an existing file is replaced.
9. **Close Project**: Closes the project; unsaved changes are asked about first.
10. **Exit**: Quits the program (Alt+F4); unsaved changes are asked about first.

#### Export

![menu-file-export](Assets/menu-file-export.svg)

The File > Export submenu.

1. **Export field...**: Writes the field over the export range (Ctrl+Shift+E) as a video or an image sequence.
2. **Export intensity...**: The same for the intensity (Ctrl+Shift+I).
3. **Export both...**: Both at once (Ctrl+Shift+B): two files, the field's and the intensity's, from one name.
4. **Export field frame...**: Writes the frame shown as one image of the field (Ctrl+Shift+F).
5. **Export intensity frame...**: The same for the intensity (Ctrl+Shift+G).

The file type in the Save dialog picks the format: an MP4 video (H.264; Windows uses Media Foundation, Linux uses FFmpeg with libx264), an MPEG-1 video, an uncompressed AVI, a QuickTime movie with the lossless Animation codec or the lossless PNG codec, a PNG or JPEG image sequence (one file per frame in a folder, numbered by the frame), or an OpenEXR sequence with the raw data: one 16-bit float channel per file, no colours, the intensity either as I or as √I, for NumPy, MATLAB, Octave or a viewer such as DJV. On Linux, install FFmpeg with the libx264 encoder and make sure `ffmpeg` is on PATH. A frame export offers PNG, JPEG and the two OpenEXR kinds. JPEG is written with full colour resolution (4:4:4), so the fringes of a wave stay sharp.

What the pictures contain is set in the Display settings: the colour scheme, brightness and black level of each layer, the outline of the scene image, the probes' graphs and markers, the source markers, the frame rate and the quality of a video; see [Display](#display). An export always uses the full quality: when the preview was computed at a reduced resolution, with skipped frames or with 8-bit samples (View menu), the run is made again at full quality first. The dialog starts in the project's folder under the project's name, and after the first export where the last one went.

A probe's measurements are exported as CSV from the probe's context menu, in the tree or on its graph box; see [Probes](#probes).

#### Examples

![menu-file-examples](Assets/menu-file-examples.svg)

The File > Examples submenu: the example projects shipped with the program, most of them from Wave Dude 2D. Each opens as an unsaved project named after its example; save it under your own name to keep your changes. Hover over an entry for its description.

1. **Source Mirror Probe**: Simple omnidirectional pulsed source with a smoothed square intensity profile, reflecting of simple mirror surface. A probe measures the intensity profile near the surface.
2. **Coherent source gaussian beam**: Featuring a 21 x 7 composite source that emits a continuous gaussian beam
3. **Directional pulsed source**: Featuring a coherent pulsed assembly of sources that emits a directional stream of wave pulses.
4. **3 sources 3 frequencies**: Geometric wave pattern resulting from 3 sources with different frequencies
5. **Small incoherent source**: Simulation features a small (5x5 composite) source using randomized "off"-time and source frequency.
6. **Large incoherent source**: Featuring a large (450+ composite) source that creates both spatially- as well as temporally incoherent wave emission. The probes show that even though the intensity variations are large for an incoherent source, they eventually average out over time. Probe 2 shows the 1/r relationship. where r= distance to the source.
7. **Random emitter lattice**: Collection of 800+ sources distributed over the full animation space and emitting short random wave pulses with a varying frequency.
8. **Resonant wave in cavity**: Wave energy released at the center of a cavity at almost the resonance frequency.
9. **Laplacian dispersion**: Visualization of dispersion (wavelength-dependent propagation speed) when using a third order Laplacian. To remove the dispersion, check the 4th order Laplacian checkbox under "Simulation".
10. **Beam expansion**: A beam expander using a GRIN lens and a large confocal lens.
11. **Zone plate**: One source and a zone plate
12. **Pulse splitter**: Short coherent pulse divided by a beam splitter
13. **Bragg reflector**: Demo of a Bragg reflector. The layer thickness of each layer should be 1/4 lambda divided by the refractive index of the specific layer. However, reflectivity is strongly angle dependent.
14. **Resonant roundabout**: Resonant roundabout with two sources and two exits. If you change the phase of one of the sources by 180 degrees, waves will take the other exit.
15. **Top up the tank**: Filling a tank with radiative energy
16. **Wave in 2 boxes**: Two boxes connected by a small pipe that allows for wave energy to flow from left to right (and back).
17. **Hanbury Brown Twiss**: Switch on the small source and the two telescopes will see highly correlated signals. Switch on the large source and the correlation disappears. This is a demonstration of the area of coherence as can be measured in the HBT experiment to measure the ratio between the diameter and the distance of a star.
18. **XOR AND OR logic**: 3 logical wave gates chained together
19. **Sources with spherical mirror**: Wave pulse emitted from the center of the curvature of a mirror, reflecting and then returning to the point of emission. Featuring timed probes. Notice that this simulation runs extremely fast on the GPU, when no video export is selected.
20. **Test glass**: The simulation shows the interference pattern created between two high-refractive index surfaces in close proximity of each other under a small angle.
21. **Ball lens**: Continuous point source and high refractive index ball lens. Probe 2A-B features 1/r intensity decay from the source with some interference of some reflections superimposed on it.
22. **Coherence and ball lens**: A coherent and directional source directing its waves at a ball lens with a high refractive index. It demonstrates Gaussian beam and Airy disk profiles
23. **Fibres**: Simulation of 3 different fibres: top a single mode fibre, middle a narrow multi-mode fibre, bottom a wide multi-mode fibre. Includes the use of two probes, an A-B (line) field probe and a single point intensity probe.
24. **Fibre cross over**: Two single-mode fibres side by side, their cores a dozen pixels apart: the evanescent field of one reaches into the other, so light launched into fibre A crosses over into fibre B and back along the length, a directional coupler. The probe along A shows the beating, the cross-fibre probe the field across both cores. Switch source A off and B on to launch from the other fibre.
25. **Lens vs Fresnel lens**: The example shows the difference between a Fresnel lens and a standard lens. Both lenses have a short focal distance. The Fresnel lens is much thinner and thus much lighter; however, its beam is not as clean as that of a standard lens.
26. **Three diffusers in a row**: The example shows a beam passing through 3 diffusers, each with a different characteristic spatial wavelength of its roughness (1: shortest, 3: longest). You can see the effects of the individual diffusers by switching them on or off individually.
27. **Interference filter**: A quarter-wavelength stack interference filter reflects most of the energy in a pulse of frequency 100 (wavelength 16 px) but transmits frequency 50 (wavelength 32 px) almost completely.
28. **Blazed grating spectrometer**: Spectrometer containing a blazed (or "echelette") grating, which diffracts the waves from a source that emits 2 wavelengths under 2 slightly different angles. The lens performs a Fourier transform by separating the 2 wavelengths into two spatially separated focal points. You can switch on either source (or rotate the grating) to see the effect of either wavelength.
29. **Diffraction on a crystal lattice**: A high-frequency source irradiates a diffractive lattice. The lattice diffracts the radiation preferentially in specific directions, similar to a crystal structure in an X-ray beam. By rotating the "crystal" lattice, the diffraction angles change. By measuring the angles of the diffracted beams, the lattice structure can in principle be solved.
30. **Meta prism**: A directional source is irradiating a meta surface prism. In the Field image you can clearly see the direction of the wavefront change; however, because the number of diffractive elements is small, the intensity distribution is somewhat messy.
31. **Meta lens with photomultiplier**: The intensity of a weak pulsing source is detected with a photomultiplier, which is probabilistic and dependent on the intensity. In the focus, the intensity is high during a pulse and this leads to a high probability of detection events for Photomultiplier 2. The other one (Photomultiplier 1) is placed in an area with relatively low intensity and hardly triggers.
32. **Multi Pixel Photon Counter**: 2 coherent weak sources create a diffraction pattern that is then detected by a Multi Pixel Photon Counter.
33. **Fibre splitter with 2 PMs**: Pulses travelling through a multi-mode fibre are detected by 2 probabilistic detectors, one at each end of the fibre split.

### The Edit menu

![menu-edit](Assets/menu-edit.svg)

The Edit menu.

1. **Undo**: Takes the last change back (Ctrl+Z), without limit.
2. **Redo**: Does it again (Ctrl+Y or Ctrl+Shift+Z).
3. **Cut**: Cuts the selected objects to the clipboard (Ctrl+X).
4. **Copy**: Copies them (Ctrl+C), as text in the project's own format, so they can go into a text editor or another window as well.
5. **Paste**: Pastes objects behind the selected row (Ctrl+V); a copied Scene, Simulation or Display settings object replaces the project's when pasted on its row or on the project.
6. **Delete**: Removes the selected objects (Del).
7. **Select all**: Selects every source, part and probe of the project (Ctrl+A).

### The View menu

![menu-view](Assets/menu-view.svg)

The View menu. Its settings belong to the program, not to the project, and are kept between sessions.

1. **Panes**: Shows or hides the Project, Properties, Preview and Messages panes. The Messages pane lists the program's log with a search box, a filter per level and Copy for a bug report.
2. **Theme**: System, Light or Dark; System follows Windows.
3. **UI zoom**: 0.5x to 3x: the size of the program's windows, menus and text, for a large or a very sharp screen; 1x is the screen's own scale. It applies at once and is kept. The preview pictures keep the screen's pixels at any zoom.
4. **Language**: System, English, Deutsch, Français or Nederlands; the program switches at once.
5. **Preview resolution**: Full, Half or Quarter: the preview's run computes the canvas at a reduced size, faster and with less memory, for a weaker machine. An export always uses the full size.
6. **Preview frames**: Full, every second or every fourth frame kept by the preview's run; the timeline does not change.
7. **Preview samples**: 16-bit float keeps the full values, so the brightness can be changed after a run and every level of a gradient shows; 8-bit palette index stores each pixel as its colour at the brightness of the run's start, half the memory, and a brightness change then needs a new run, which starts by itself; the preview dithering is not available with 8-bit samples.
8. **Preview dithering**: With 16-bit samples, the pictures can be dithered as if the screen had 10, 12, 14 or 16 bits per channel, which hides the banding of dark gradients; on screen only, never in an export. This option is not available with 8-bit samples, which have nothing finer to dither. Off by default, except on a machine with plenty of memory (see below).

On its first start the program chooses the preview resolution, the preview frames, the preview samples and the dithering for the machine it runs on, once, and the View menu shows the choice. A machine with 32 GB of memory or more starts with 16-bit samples, dithered to 16 bits. A machine with less than 16 GB, with a hard disk under the cache file, without an OpenCL graphics card or with four processor threads or fewer starts at half resolution with every second frame and 8-bit samples. Every other machine starts at full resolution with every frame and 8-bit samples. Each setting can be changed here at any time.

The three choices differ in memory: a Full HD scene of 1920 × 1080 pixels run for 1500 frames, with the field and the intensity both kept, takes 11.6 GB with 16-bit samples at full resolution and every frame, 5.8 GB with 8-bit samples at full resolution and every frame, and 740 MB at half resolution with every second frame and 8-bit samples. The settings combine: half resolution divides a frame by four, quarter resolution by sixteen, every second or fourth frame divides their number the same way, and 8-bit samples halve each frame against 16-bit.

### The Simulation menu

![menu-simulation](Assets/menu-simulation.svg)

The Simulation menu.

1. **Run**: Runs the simulation from the start (F5).
2. **Pause**: Pauses the run, and resumes it (F6).
3. **Stop**: Stops the run (Shift+F5); the frames computed so far stay.
4. **Play**: Plays the frames at the playback speed, and pauses them (F7): the timeline's Play and Pause.
5. **Use the GPU (OpenCL)**: Computes on the graphics card, many times faster than the processor; the results are the same. Off, or without OpenCL, the processor is used. On by default.
6. **Re-run automatically after edits**: Starts a new run by itself a moment after you change the scene, a source or a part; off, you run by hand with F5 or Play. On by default.
7. **Warn when a run does not fit in RAM**: Asks before a run whose frames would not all fit in the memory the RAM cache may take. Tick the RAM cache size to Auto, half the resolution, half the frames or 8-bit samples, each with its effect shown, and Run applies them in the Simulation and View menus for this and later runs; Cancel leaves everything as it is. "Do not warn me again" switches this off. On by default.
8. **RAM cache size**: How much of the memory a run's frames may take: Auto decides from the free memory, or a share from 10 to 100 percent caps it; the frames beyond it go to a cache file on disk.

### The Help menu

![menu-help](Assets/menu-help.svg)

The Help menu.

1. **About WaveDude Next**: The version, the people whose work it is based on, the license, and the system information (processor, memory, OpenCL devices, video encoders) with a button that copies it for a bug report.
2. **Manual**: This manual (F1). F1 brings it to the front again, Esc closes it, and it closes with the main window.

## Project settings

The first rows of the project tree are the settings of the project as a whole: the Project row with the random seed and a description, the Scene (the canvas and what it is made of), the Simulation (how long and how precisely it runs) and the Display (how the pictures are coloured and what an export contains). A project is saved as one .wdproj file, the scene image embedded; the settings of the View and Simulation menus are the program's and are not in it.

### Project

![properties-project](Assets/properties-project.svg)

The project's own form.

1. **Format version**: The version of the project file format; older files are read and saved in the current one.
2. **Random seed**: The seed of every random decision in the project, the emitters' random offsets and the random gaps between bursts; the same seed reproduces the same frames.
3. **Description**: Free-text notes about the project. An example's description is what its menu entry shows.
4. **New seed**: Draws a fresh random seed.

### Scene

![properties-scene](Assets/properties-scene.svg)

The Scene form of a project with a scene image. The canvas is either blank, with its size and a uniform refractive index, or built from an image, in which every pixel's red is the refractive index (red 0 a mirror), green an emitting source and blue absorption; optical parts are painted over either.

1. **Mode**: Blank canvas of the given size, or a canvas built from the scene image.
2. **Scene image**: Image the canvas is built from: red = refractive index (red 0 = a mirror), green = sources, blue = damping. The name of the embedded image, or a path relative to the project folder.
3. **Image file**: Where the embedded image came from. The embedded image is what is simulated, so a missing file changes nothing; while the project is open, the file is watched and re-embedded when it changes, and on opening the project a changed file replaces the embedded image too.
4. **Embedded image**: The size of the embedded scene image; the canvas takes this size.
5. **Width**: Width of a blank canvas in pixels.
6. **Height**: Height of a blank canvas in pixels.
7. **Edges**: Open lets a wave leave the canvas, as if it sat in open space; Reflective is a hard wall, as in Wave Dude 2D.
8. **Damping edge**: Thickness in pixels of the absorbing frame: positive inside the canvas, where it shows as a dark margin; negative outside it, simulated but never shown. 0 turns it off. A new project has open edges with a rim outside, the combination that returns the least.
9. **Refractive index**: Refractive index of a blank canvas, 1.0 to 2.55; the wave speed is 1/n pixels per step.
10. **Image sources**: Lets the green channel of the scene image emit waves; the pixel value is the frequency level.
11. **Image source amplitude**: Peak amplitude of the image sources.
12. **Object sources**: Uses the sources defined in the project.
13. **The image buttons**: The dots pick an image file (PNG, BMP, JPEG, TIFF or GIF), which is embedded in the project; the save glyph writes the embedded image out to a file; the X drops the image and its path, leaving a blank canvas of the image's size.

### Simulation

![properties-simulation](Assets/properties-simulation.svg)

The Simulation form.

1. **Steps**: Total number of simulation steps in a run.
2. **Visualize every**: A frame is kept, exported and the probes are sampled every this many steps.
3. **4th-order Laplacian**: Uses the more accurate 4th-order stencil with sub-stepping instead of the 9-point stencil. Slower; it removes the dispersion of the 9-point stencil, where the speed depends on the wavelength.
4. **Wave speed**: Nominal wave speed in pixels per step; only used to estimate wavelengths in the forms.
5. **Intensity time decay**: How long the intensity remembers: it is averaged over this × 100 / lowest source level steps.
6. **Source frequency scale**: Multiplier of the level-to-frequency mapping: ω = level / 255 × 0.5 × scale, in radians per step.
7. **Intensity scale**: Multiplier applied to the intensity values.

### Display

![properties-display](Assets/properties-display.svg)

The Display form: the Field and Intensity sections colour the two pictures, Preview sets what lies over them, Export what a video or image sequence contains.

1. **Colour scheme**: The colour scheme of the field picture; see [Colour schemes](#colour-schemes).
2. **Brightness**: Gain applied to the values before the colour lookup; the same as the box in the picture's header.
3. **Black level**: Offset applied before the colour lookup; values at or below it map to the first colour.
4. **Preview**: Shows the field in the preview.
5. **Outline in the preview**: Lays the scene image's glowing-edges outline over the field picture, so you see the scene in the wave.
6. **Outline brightness**: Multiplier of the outline: 1 as computed, higher lifts faint edges, 0 hides it.
7. **Outline in the export**: Bakes the outline into exported frames and videos of the field.
8. **Colour scheme**: The same for the intensity picture.
9. **Brightness**: The same for the intensity.
10. **Black level**: The same for the intensity.
11. **Preview**: Shows the intensity in the preview.
12. **Outline in the preview**: The outline over the intensity picture.
13. **Outline brightness**: The same for the intensity.
14. **Outline in the export**: The same for the intensity.
15. **Handles in the preview**: Shows the handles, lines, emitter dots and levers of every source, part and probe over the preview pictures. A selected object shows its own either way.
16. **Labels in the preview**: Shows the names beside the handles over the preview pictures. A selected object shows its own either way.
17. **From step**: First simulation step of the exported range; 0 is the start of the run. The in point of the timeline.
18. **To step**: Last simulation step of the exported range; 0 means the end of the run. The out point of the timeline.
19. **Probe graphs in the export**: Draws the probes' graph boxes over exported frames and videos.
20. **Probe markers in the export**: Draws the probes' positions (crosses, the line of a line probe, the number) over exported frames and videos.
21. **Source markers in the export**: Draws the enabled sources (the marker, a composite source's emitter dots, the name) over exported frames and videos.
22. **Frame rate**: Frames per second of an exported video.
23. **Quality**: Encoder quality, 1 to 100.

## Sources

Sources emit the waves. A point source emits from one pixel; a composite source is an array of emitters around its position, which makes a beam, a directional pulse or an incoherent patch, as the examples show. Add a source with the first button of the project tree or its context menu; it lands at the centre of the canvas, and you move it by its handle on the pictures or by typing X and Y. A source's frequency is a level up to 255 on the scale of the scene image's green channel; the form shows the wavelength it gives in pixels. A source emits continuously or in bursts, and a disabled source is left out of a run.

### Source

![properties-source](Assets/properties-source.svg)

The form of a point source.

1. **Name**: Name shown in the project tree and on the canvas.
2. **Enabled**: Includes the source in simulation runs.
3. **X**: Horizontal position of the emitter, or of the array centre, in canvas pixels.
4. **Y**: Vertical position of the emitter, or of the array centre, in canvas pixels.
5. **Frequency**: Frequency as a level up to 255, decimals allowed, the same scale as the green channel of image sources. Above 255 the grid cannot sample the wave.
6. **Amplitude**: Amplitude in percent, 0 to 100, decimals allowed.
7. **Phase**: Phase offset in degrees, -180 to 180.
8. **Wavelength**: Estimated from the wave speed and the frequency (λ = c / f): emitters one wavelength apart radiate in phase along that direction. Holds for the 4th-order Laplacian; the 9-point stencil's speed varies with the frequency.
9. **Mode**: Emit continuously or in bursts.
10. **Pulse shape**: Envelope of each burst, Gaussian or a smoothed square; pulsed mode only.
11. **Pulse width**: Length of each burst in cycles of the source frequency; pulsed mode only.
12. **Off width**: Length of the gap between bursts in cycles; pulsed mode only.
13. **Detach after step**: Simulation step at which the source fades out; -1 keeps it on for the whole run.
14. **Composite source**: Emits from an array of emitters instead of a single point; the settings below shape the array.
15. **Columns**: Number of emitters across. Odd counts keep the centre emitter on the source position.
16. **Rows**: Number of emitters down.
17. **Horizontal spacing**: Distance between neighbouring columns in canvas pixels.
18. **Vertical spacing**: Distance between neighbouring rows in canvas pixels.
19. **Rotation**: Rotation of the grid about the source position in degrees, counter-clockwise on the screen; 0 keeps the rows horizontal. On the pictures, the lever of a selected composite source turns it.
20. **Frequency**: Random offsets, added per emitter: each emitter's level is offset by up to ± this amount. 0 turns it off.
21. **Phase**: Each emitter's phase is offset by up to ± this many degrees. 0 turns it off.
22. **Off width**: Each gap between bursts is offset by up to ± this many cycles, re-drawn every period. 0 turns it off.
23. **Position**: Each emitter is moved by up to ± this many pixels in X and in Y. 0 turns it off.

The random offsets are drawn from the project's random seed, so a run repeats exactly; New seed on the Project form draws others.

### Composite source

![properties-composite-source](Assets/properties-composite-source.svg)

The same form for a composite source in pulsed mode: the pulse settings 10 to 12 are in use, and the array settings 14 to 23 shape the emitters, which the pictures show as dots. The random offsets make an incoherent source: with random off widths the emitters fire out of step, and with random frequencies and phases the pattern changes all the time, as in the examples of the small and the large incoherent source.

## Optical parts

Optical parts are painted into the scene: a part of glass by its refractive index, a mirror reflecting everything, an absorber taking the wave in. Add one from the Add part menu of the project tree; it lands at the centre of the canvas, and you move it by its handle, turn it by its lever and, by its kind, resize it by grips, curve a lens by its surface handles or shape an outline or a fibre by its points. A part edit shows in the pictures as soon as the frame is computed, and the run starts over by itself. At the top of a part's form, a header shows or hides a description of its kind; it stays open or closed for every kind.

### The kinds of parts

![menu-add-part](Assets/menu-add-part.svg)

The Add part menu of the project tree.

1. **Lens/Mirror**: A lens or a mirror: a strip of the diameter between two spherical surfaces, each bulging outward, flat or hollow by the sign of its radius, the centre thickness apart. As a mirror it reflects everything; as a lens it refracts by its index, with an anti-reflection coating and absorption if wanted.
2. **Fresnel lens**: A Fresnel lens: a lens whose curved surfaces are cut into zones from the axis to the rim, equal in width or in depth, each zone's arc starting again at the vertex plane, so a thin plate bends light as a thick lens would. The settings are the lens's, with the number of zones and their division; a flat surface stays flat.
3. **Fibre**: A fibre: a core of higher index inside a cladding, along a smooth path of Bézier segments through its nodes, with flat or round ends. A wave coupled into the core is guided along the path; the cladding can absorb stray light at its rim.
4. **Bragg reflector**: A Bragg reflector: pairs of layers of two indices stacked along the axis. It reflects strongly around the wavelength the layer thicknesses are tuned to, a quarter wave each, and passes others.
5. **GRIN lens**: A gradient-index lens: a rectangle whose index falls from the axis to the edge in a parabolic profile, so it focuses like a lens with flat faces.
6. **Zone plate**: A zone plate: Fresnel zones cut for a design frequency and a focal length, every second zone an absorbing block, the central zone open or, inverted, a block. It focuses by diffraction; an optional mount stops light passing outside the diameter.
7. **Diffuser**: A diffuser: a plate of glass or mirror whose front face is rough, the back flat. The roughness is a spread of ripples about a centre wavelength, their phases random from the project's seed; it scatters what passes or reflects.
8. **Interference filter**: An interference filter: a plate of glass or mirror with a stack of thin layers on its front, each with its own thickness and index. Waves reflected at the layers' boundaries interfere, so the stack reflects some wavelengths and passes others; a new filter is a quarter-wave stack for a wavelength of 16 pixels.
9. **Diffraction gratings**: The gratings, in a submenu of their own.
10. **Meta surfaces**: The meta surfaces, in a submenu of their own.
11. **Polygon**: A regular polygon of glass or mirror, by its number of sides and the side length.
12. **Rectangle**: A rectangle of glass or mirror, by length and width; linked, a square. A flat mirror is a long, narrow one.
13. **Ellipse**: An ellipse of glass or mirror, by the length and width of its box; linked, a circle.
14. **Triangle**: A triangle of glass or mirror: the base across the axis, the apex along it, the base angles set by a preset or by hand.
15. **Outline**: A free outline of glass or mirror: a closed shape through the points you place and drag; fewer than three paint nothing.
16. **Optical logic**: The gates, in a submenu of their own.

![menu-add-part-gratings](Assets/menu-add-part-gratings.svg)

The Diffraction gratings submenu.

1. **Simple 2D grating**: A simple 2D grating: a lattice of round dots, so many columns along the axis by so many rows across, each direction with its own spacing, nothing between them. The dots are mirror, or glass with an index; a wave passing the lattice is diffracted into the orders the spacings set. One column makes a line grating.
2. **Facetted grating**: A facetted grating: a plate of glass or mirror whose front face carries a periodic relief, a sawtooth, a square wave or a sinusoid repeated every pitch, the back flat. A diffuser of one wavelength: the pitch and the wavelength set the angles the orders leave at.

![menu-add-part-metasurfaces](Assets/menu-add-part-metasurfaces.svg)

The Meta surfaces submenu.

1. **Meta lens**: A meta lens: a carrier plate with a sub-wavelength pattern on its front, a ridge of its own index in every cell, whose fill is computed from the target frequency and the focal length so the cells delay the wave as a lens would, zone by zone. Converging or diverging; the pattern's tops and the plate's back can carry an anti-reflection coating.
2. **Meta prism**: A meta prism: a carrier plate with a sub-wavelength pattern on its front whose cells delay the wave in a line across the face, so a wave arriving along the axis leaves turned by a set angle, as a blazed grating's first order. Cut for a target frequency; the pattern's tops and the plate's back can carry an anti-reflection coating.
3. **Meta axicon**: A meta axicon: a carrier plate with a sub-wavelength pattern on its front whose cells delay the wave in proportion to the distance from the axis, so the two halves of a wave arriving along the axis are turned toward it by a set angle and cross along a line behind the surface rather than at a point. Cut for a target frequency; the pattern's tops and the plate's back can carry an anti-reflection coating.

![menu-add-part-logic](Assets/menu-add-part-logic.svg)

The Optical logic submenu.

1. **XOR (Mach-Zehnder)**: An optical XOR gate: two input waveguides, a phase section putting the lower input a quarter period ahead, and a coupler whose upper output carries their difference. It lights when one input is lit, not both, for coherent inputs at the design frequency.
2. **AND**: An optical AND gate: the same coupler with the lower input a quarter period behind, so the upper output carries the sum. It is brightest with both inputs lit, about four times a single one; a passive linear gate has no truer AND.
3. **OR**: An optical OR gate: the same coupler with the lower input a twelfth of a period ahead, so the upper output carries half the power of either input and of both. It lights with one input or both.

### Settings every part shares

![properties-part-common](Assets/properties-part-common.svg)

Every part's form starts with the same six settings, here at the top of the rectangle's form; they are the badges 1 to 6 on every part's figure below.

1. **Description of the kind**: The header shows or hides what this kind of part is; Space toggles it. Open or closed is one setting for every kind.
2. **Name**: Name shown in the project tree and on the canvas.
3. **Enabled**: Paints the part into the scene; a disabled part has no effect on a run.
4. **X**: Horizontal position of the part's centre in canvas pixels.
5. **Y**: Vertical position of the part's centre in canvas pixels.
6. **Angle**: Rotation about the centre in degrees, counter-clockwise on the screen; 0 points the part's axis to the right, so the front of the part faces a source on the left.

### Glass and mirror settings

![properties-part-material](Assets/properties-part-material.svg)

A lens, a polygon, a rectangle, an ellipse, a triangle, an outline and a diffuser are made of glass or are a mirror, and share the material settings, here the Material section of the rectangle's form: the badges 7 to 15 on their figures below. The interference filter's plate has them too, at 7 to 13 on its form, with the coating on the back alone. The GRIN lens has the coating settings as well, at 9 to 13 on its form, and the fibre has its own at 13 to 17.

7. **Mirror**: Makes the part a mirror instead of glass. The refractive index, the coating and the absorption are ignored then.
8. **Refractive index**: Refractive index of the glass, 1.00 to 2.55 in steps of 0.01.
9. **AR coating**: Thickness in pixels of the anti-reflection band just inside the outline; 0 for none. A quarter-wave layer is λ / (4 √n) pixels thick, λ being the wavelength the source form shows; a graded band is the better the thicker, from a wavelength on.
10. **Coating profile**: How the index runs through the band: a layer at √n, the quarter-wave idea, or a graded rise from the medium outside to the glass. A graded band reflects less than a layer once it is a wavelength thick or more, at any wavelength; a layer only at the wavelength it is cut for.
11. **Coat the front**: Puts the band on the front: the faces turned toward a source on the left, a lens's first surface. Clear it to leave them bare, as the inner faces of a cavity are.
12. **Coat the back**: Puts the band on the back: the faces turned away from a source on the left, a lens's second surface. Clear it to leave them bare.
13. **Coat the sides**: Puts the band on the sides: the faces that are neither front nor back, a lens's rim, a slab's narrow ends. Clear it to leave them bare.
14. **Absorption**: How strongly the part absorbs waves, 0 (not at all) to 255, as the scene image's blue channel. It rises from nothing at the outline over the absorption ramp, so a wave enters the part instead of being reflected. A mirror ignores it.
15. **Absorption ramp**: Depth in pixels over which the absorption rises from nothing at the outline to its full value; the longer, the less of a wave is reflected. A part thinner than twice the ramp reaches its full absorption in its middle.

### Lens

![properties-lens](Assets/properties-lens.svg)

A lens or a mirror: a strip of the diameter between two spherical surfaces, each bulging outward, flat or hollow by the sign of its radius, the centre thickness apart. As a mirror it reflects everything; as a lens it refracts by its index, with an anti-reflection coating and absorption if wanted. Ticking Mirror renames a part called Lens to Mirror, and back. On the pictures, a selected lens shows a handle on each surface: drag it along the axis to curve the surface, outward bulges, inward flattens and then hollows it, while the rim stays. Badges 1 to 15 are the shared and the glass and mirror settings.

16. **Diameter**: Extent across the axis in pixels.
17. **Concave (diverging)**: Whether the lens diverges, its surfaces curving inward. Ticking it turns the lens around by flipping the sign of both radii; a flat surface stays flat.
18. **R1**: Radius of the first surface in pixels: positive bulges outward, negative is concave, 0 is flat. A radius too curved for the diameter and the centre thickness is raised to the smallest that fits.
19. **R2**: Radius of the second surface in pixels: negative bulges outward, positive is concave, 0 is flat. A radius too curved for the diameter and the centre thickness is raised to the smallest that fits.
20. **Centre thickness**: Distance between the two surfaces on the axis in pixels.
21. **Edge thickness**: Thickness at the rim: the centre thickness less what the convex surfaces take, plus what the concave ones add; 0 when the surfaces meet at the rim. A read-out.

### Fresnel lens

![properties-fresnel-lens](Assets/properties-fresnel-lens.svg)

A Fresnel lens: a lens whose curved surfaces are cut into zones from the axis to the rim, each zone's arc starting again at the vertex plane, so a thin plate bends light as a thick lens would. The settings are the lens's, with the number of zones and how they are divided; a flat surface stays flat. A new Fresnel lens is 500 pixels across with radii of 1500 on a plate 20 pixels thick. The deepest zone is the one the centre thickness must hold: a radius too curved for the plate is raised until that zone's sags fit, where a whole lens would need a far flatter one; the edge thickness is what the deepest zone leaves at the rim. There is no Concave box: a negative radius hollows a surface, as on the lens. On the pictures the surface handles and the size grip work as the lens's, the vertex handle setting the deepest zone's depth. Badges 1 to 15 are the shared and the glass and mirror settings.

16. **Diameter**: Extent across the axis in pixels.
17. **R1**: Radius of the first surface in pixels: positive bulges outward, negative is concave, 0 is flat. A radius too curved for the diameter and the centre thickness is raised to the smallest whose zones fit.
18. **R2**: Radius of the second surface in pixels: negative bulges outward, positive is concave, 0 is flat.
19. **Centre thickness**: Distance between the two surfaces on the axis in pixels: the plate.
20. **Zones**: Number of zones each curved surface is cut into, from the axis to the rim. At each zone's inner edge the arc starts again at the vertex plane, so the plate stays thin; 1 is a plain lens.
21. **Zone division**: How the zones are divided: equal in width, the outer zone the deepest; or equal in depth, each zone cut to the same depth and the zones narrowing outward, as the elements of a lighthouse lens are.
22. **Edge thickness**: Thickness at the rim: the centre thickness less the deepest zone's sag on each convex surface, plus what a concave one adds. A read-out.

### Polygon

![properties-polygon](Assets/properties-polygon.svg)

A regular polygon of glass or mirror, by its number of sides and the side length. Badges 1 to 15 are the shared and the glass and mirror settings.

16. **Number of sides**: 3 to 64.
17. **Side length**: Length of each side in pixels.

### Rectangle

![properties-rectangle](Assets/properties-rectangle.svg)

A rectangle of glass or mirror, by length and width; linked, a square. A flat mirror is a long, narrow one. Badges 1 to 15 are the shared and the glass and mirror settings.

16. **Length**: Extent along the axis in pixels.
17. **Width**: Extent across the axis in pixels.
18. **Linked**: The chain between Length and Width keeps the width equal to the length: a square.

### Ellipse

![properties-ellipse](Assets/properties-ellipse.svg)

An ellipse of glass or mirror, by the length and width of its box; linked, a circle. Badges 1 to 15 are the shared and the glass and mirror settings.

16. **Length**: Diameter along the axis in pixels.
17. **Width**: Diameter across the axis in pixels.
18. **Linked**: The chain between Length and Width keeps the width equal to the length: a circle.

### Triangle

![properties-triangle](Assets/properties-triangle.svg)

A triangle of glass or mirror: the base across the axis, the apex along it, the base angles set by a preset or by hand. Badges 1 to 15 are the shared and the glass and mirror settings.

16. **Type**: Equilateral, right isosceles (the 45° prism), 30-60-90, isosceles with one typed angle, or custom with both typed.
17. **Base**: Length of the base in pixels; the base lies across the axis, toward a source on the left.
18. **Angle A**: Base angle at the first end of the base, in degrees. Typed for the isosceles and custom types; the presets fix it.
19. **Angle B**: Base angle at the second end of the base, in degrees. Typed for the custom type; the others fix it.
20. **Apex angle**: The angle at the apex: 180° minus the two base angles. A read-out.

### Outline

![properties-outline](Assets/properties-outline.svg)

A free outline of glass or mirror: a closed shape through the points you place and drag; fewer than three paint nothing. On the pictures, a selected outline shows a handle on each point: drag it, or move it with the arrow keys. Badges 1 to 15 are the shared and the glass and mirror settings.

16. **X**: A point's position relative to the part's centre in canvas pixels, before rotation, with its Y beside it.
17. **A point's row**: X, Y and a button that removes the point.
18. **Add a point**: Appends a point after the last one.

### Fibre

![properties-fibre](Assets/properties-fibre.svg)

A fibre: a core of higher index inside a cladding, along a smooth path of Bézier segments through its nodes, with flat or round ends. A wave coupled into the core is guided along the path; the cladding can absorb stray light at its rim. On the pictures, a selected fibre shows its nodes and their handles: drag a node to move it with its handles, drag a handle to shape the segment arriving at or leaving the node. Badges 1 to 6 are the shared settings.

7. **Core index**: Refractive index of the core, 1.00 to 2.55 in steps of 0.01.
8. **Core diameter**: Diameter of the core in pixels.
9. **Cladding index**: Refractive index of the cladding, 1.00 to 2.55 in steps of 0.01. Below the core's index, the fibre guides.
10. **Cladding thickness**: Thickness of the cladding around the core in pixels; 0 for a bare core.
11. **Cladding absorption**: Peak absorption of the cladding's absorbing band, 0 (not at all) to 255, as the scene image's blue channel: a hump in the outer part of the cladding, rising over the band width from nothing, peaking, and falling to nothing at the outer edge, so the guided wave's tail stays untouched and stray light is absorbed from either side. Nothing on a bare core.
12. **Absorption band**: Half the absorbing band's width in pixels: the absorption rises over this many pixels to its peak and falls over as many to the outer edge. A band wider than the cladding is squeezed into it.
13. **AR coating**: Thickness in pixels of the anti-reflection band just inside the outer boundary and the end faces; 0 for none. A quarter-wave layer is λ / (4 √n) pixels thick, λ being the wavelength the source form shows; a graded band is the better the thicker, from a wavelength on.
14. **Coating profile**: As for the glass parts: a layer at √n, or a graded rise from the medium outside to the glass.
15. **Coat the front**: Coats the faces turned toward a source on the left: the start face of a fibre that runs to the right.
16. **Coat the back**: Coats the faces turned away from it: the end face.
17. **Coat the sides**: Coats the rest: the tube along the path.
18. **Ends**: How the two ends are cut: flat, straight across the path like a cleaved fibre, or rounded with the outer radius. The AR coating follows a flat end face.
19. **Node**: The first node: X and Y relative to the part's centre in canvas pixels, before rotation.
20. **In**: The node's in-handle: X and Y relative to the node, in canvas pixels before rotation. It shapes the segment arriving from the previous node; zero makes that segment straight at this end.
21. **Out**: The node's out-handle, likewise, shaping the segment leaving to the next node.
22. **Node**: The second node.
23. **In**: Its in-handle.
24. **Out**: Its out-handle.
25. **Smooth, remove**: Smooth keeps the two handles opposite each other through the node, so the path runs smoothly through it; switching it on lines them up at once. The button removes the node.
26. **Add a node**: Appends a node beyond the last one, continuing the path.

### Bragg reflector

![properties-bragg-reflector](Assets/properties-bragg-reflector.svg)

A Bragg reflector: pairs of layers of two indices stacked along the axis. It reflects strongly around the wavelength the layer thicknesses are tuned to, a quarter wave each, and passes others. Badges 1 to 6 are the shared settings.

7. **Layer pairs**: Number of layer pairs, 1 to 200.
8. **Thickness 1**: Thickness of the first layer of each pair in pixels. A quarter-wave layer is λ / (4 n) pixels thick.
9. **Index 1**: Refractive index of the first layer of each pair, 1.00 to 2.55 in steps of 0.01.
10. **Thickness 2**: Thickness of the second layer of each pair in pixels. A quarter-wave layer is λ / (4 n) pixels thick.
11. **Index 2**: Refractive index of the second layer of each pair, 1.00 to 2.55 in steps of 0.01.
12. **Width**: Extent of the stack across the axis in pixels.

### GRIN lens

![properties-grin-lens](Assets/properties-grin-lens.svg)

A gradient-index lens: a rectangle whose index falls from the axis to the edge in a parabolic profile, so it focuses like a lens with flat faces. Badges 1 to 6 are the shared settings.

7. **Centre index**: Refractive index on the axis, 1.00 to 2.55 in steps of 0.01.
8. **Edge index**: Refractive index at the edge, 1.00 to 2.55 in steps of 0.01. The profile between the axis and the edge is parabolic.
9. **AR coating**: The anti-reflection band just inside the outline, as for the glass parts; 0 for none.
10. **Coating profile**: A layer at √n, or a graded rise.
11. **Coat the front**: The band on the face turned toward a source on the left.
12. **Coat the back**: The band on the face turned away from it.
13. **Coat the sides**: The band on the long sides.
14. **Length**: Extent along the axis in pixels.
15. **Diameter**: Extent across the axis in pixels.

### Zone plate

![properties-zone-plate](Assets/properties-zone-plate.svg)

A zone plate: Fresnel zones cut for a design frequency and a focal length, every second zone an absorbing block, the central zone open or, inverted, a block. It focuses by diffraction; an optional mount stops light passing outside the diameter. Badges 1 to 6 are the shared settings.

7. **Target frequency**: The source frequency the plate is designed for, on the same scale as a source's frequency. The zones follow from the wavelength it gives, so the plate focuses this frequency at the focal length; other frequencies focus elsewhere.
8. **Wavelength**: The wavelength the design frequency gives in a medium of index 1, with the simulation's source frequency scale. A read-out.
9. **Focal length**: Distance in pixels from the plate to its focus, for a plane wave arriving along the axis.
10. **Inverse**: Exchanges the zones: the central zone becomes a block, the first ring open, and so on. The focus stays where it is.
11. **Diameter**: Extent across the axis in pixels; the zones fill it.
12. **Thickness**: Extent along the axis in pixels: the depth of the blocks. It should leave room for the anti-reflection rise on both faces.
13. **Anti-reflection**: Depth in pixels over which a block's absorption rises from nothing at its faces to its full strength, so the blocks reflect little; at most half the thickness takes effect.
14. **Mount**: Surrounds the plate with an absorbing mount, so no light passes outside its diameter. Without it, that light meets the focused light on the axis and moves the focus further out, the more so the fewer zones the plate has.
15. **Mount width**: Width in pixels of the absorbing mount on either side, beyond the diameter.
16. **Number of zones**: The zones the plate gets within its diameter. A read-out.
17. **Outermost zone**: The width of the outermost whole zone, which the grid must resolve: a few pixels at least. A read-out.

### Diffuser

![properties-diffuser](Assets/properties-diffuser.svg)

A diffuser: a plate of glass or mirror whose front face is rough, the back flat. The roughness is a spread of ripples about a centre wavelength, their phases random from the project's seed; it scatters what passes or reflects. Coat the front puts the anti-reflection band on the rough face, Coat the back on the flat one, at any angle; a new diffuser coats the back alone. The roughness is worked out again only when a roughness setting, the width, the name or the project's seed changes, so the same project always gives the same face. On the pictures, a selected diffuser shows a grip on its back corner that sets the length and the width. Settings 1 to 6 are those every part shares, 7 to 15 the glass and mirror settings.

16. **Length**: Extent along the axis in pixels: the plate's thickness at the mean of its rough face.
17. **Width**: Extent across the axis in pixels: the width of the rough face.
18. **Centre wavelength**: The spatial wavelength in pixels at the middle of the roughness's spectrum: the ripple along the face that has the full amplitude. Two pixels at least.
19. **Amplitude**: The amplitude in pixels of the ripple at the centre wavelength. The other wavelengths of the spread get less, by their distance from the centre, and the face carries the sum of them all; 0 makes the face flat.
20. **Sigma**: The spread of the roughness's wavelengths about the centre wavelength, in pixels: a wavelength gets the amplitude times the bell e^(−(λ − centre)² / 2σ²). 0 leaves the centre wavelength alone, a single regular ripple; the larger, the more wavelengths join in and the less regular the face.
21. **Profile height**: The rough face from its highest point to its lowest: what the ripples add up to. A read-out.
22. **Ripples**: How many wavelengths make up the roughness: one at sigma 0, thirteen otherwise, those below two pixels left out. A read-out.

### Interference filter

![properties-interference-filter](Assets/properties-interference-filter.svg)

An interference filter: a plate of glass or mirror with a stack of thin layers on its front, each with its own thickness and index. Waves reflected at the layers' boundaries interfere, so the stack reflects some wavelengths and passes others; a new filter is a quarter-wave stack for a wavelength of 16 pixels in the usual design, beginning and ending with the high index: from the outside in, five pairs of a layer of index 2.00, 2.00 pixels thick, and one of 1.50, 2.67 pixels thick, then one more layer of 2.00 on the plate, eleven layers that reflect most of a wave at that wavelength. The layers lie on the plate's front, outside it, so the plate keeps its length and the filter grows by the stack; the first layer in the list is the outermost, the one a wave from the left meets first. Ticking Mirror makes the plate a mirror and leaves the layers glass in front of it: a coated mirror. The plate's anti-reflection band can lie only on its back, the face away from the layers. On the pictures, a selected filter shows a grip on the plate's back corner that sets the length and the width. Settings 1 to 6 are those every part shares.

7. **Mirror**: Makes the plate a mirror instead of glass; the layers stay glass in front of it. The plate's refractive index, coating and absorption are ignored then.
8. **Refractive index**: Refractive index of the plate, 1.00 to 2.55 in steps of 0.01.
9. **AR coating**: Thickness in pixels of the anti-reflection band just inside the plate's back; 0 for none.
10. **Coating profile**: As for the glass parts: a layer at √n, or a graded rise from the medium outside to the glass.
11. **Coat the back**: Puts the band on the plate's back, the face away from the layers. Clear it to leave the back bare.
12. **Absorption**: How strongly the plate absorbs waves, 0 (not at all) to 255, as for the glass parts. The layers do not absorb.
13. **Absorption ramp**: Depth in pixels over which the plate's absorption rises from nothing at its outline to its full value.
14. **Length**: Extent of the plate along the axis in pixels, without the layers, which lie on its front.
15. **Width**: Extent across the axis in pixels, of the plate and the layers alike.
16. **Stack thickness**: The layers' thicknesses added up: how far the stack stands out of the plate's front. A read-out.
17. **A layer's row**: Drag the grip at the left to move the layer through the list; the row moves as the pointer passes the others. Then the layer's number, its thickness in pixels to two decimals (a quarter-wave layer is the wavelength divided by four times the index; a layer of 0 is left out), its refractive index, and buttons that move it up (outwards) or down (towards the plate) and remove it. The buttons are in the Tab order, so the keyboard moves a layer as well: the focus stays on the moved layer's button, so pressing Space again moves it on.
18. **Add a layer**: Appends a copy of the last layer, so a stack of alternating layers grows by copying and changing.
19. **Remove all**: Removes every layer at once, to build another stack from nothing; Undo (Ctrl+Z) brings them back.

### Simple 2D grating

![properties-simple-grating](Assets/properties-simple-grating.svg)

A simple 2D grating: a lattice of round dots, so many columns along the axis by so many rows across, each direction with its own spacing, nothing between them. The dots are mirror, or glass with an index; a wave passing the lattice is diffracted into the orders the spacings set. One column makes a line grating. A new grating is one column of 31 mirror dots, 16 pixels apart and 2 pixels across: a line grating across the axis, which a wave from the left meets. Odd counts keep the middle dot on the part's position, even counts put the position between two dots. The dots have no anti-reflection coating. Settings 1 to 6 are those every part shares.

7. **Mirror**: Makes the dots mirrors instead of glass. The refractive index and the absorption are ignored then.
8. **Refractive index**: Refractive index of the glass dots, 1.00 to 2.55 in steps of 0.01.
9. **Absorption**: How strongly the dots absorb waves, 0 (not at all) to 255, as for the glass parts.
10. **Absorption ramp**: Depth in pixels over which a dot's absorption rises from nothing at its rim to its full value.
11. **Columns**: Number of dots along the axis; one column is a line grating across the axis.
12. **Rows**: Number of dots across the axis.
13. **Column spacing**: Distance between neighbouring columns in pixels: the grating's period along the axis.
14. **Row spacing**: Distance between neighbouring rows in pixels: the grating's period across the axis.
15. **Dot diameter**: Diameter of each dot in pixels, at most half the smaller spacing; the box stops there. A dot of 2 pixels covers a square of 2 × 2 pixels; a mirror dot under 2 pixels covers no pixel centre and paints nothing.

### Facetted grating

![properties-facetted-grating](Assets/properties-facetted-grating.svg)

A facetted grating: a plate of glass or mirror whose front face carries a periodic relief, a sawtooth, a square wave or a sinusoid repeated every pitch, the back flat. A diffuser of one wavelength: the pitch and the wavelength set the angles the orders leave at. The anti-reflection coating can lie only on the flat back. On the pictures, a selected grating shows a grip on its back corner that sets the length and the width. Settings 1 to 6 are those every part shares, 7 to 13 the glass and mirror settings with the coating on the back alone.

14. **Length**: Extent along the axis in pixels: the plate's thickness at the mean of its facetted face.
15. **Width**: Extent across the axis in pixels: the width of the facetted face.
16. **Pattern**: The facets' shape over each pitch: a sawtooth, a ramp then a step; a square wave, a crest and a trough of half a pitch each; or a sinusoid.
17. **Pitch**: The period of the facets across the face in pixels: the grating's pitch, which with the wavelength sets the diffraction angles. Two pixels at least.
18. **Amplitude**: The relief's height either side of the mean face in pixels: the facets run from this far out to this far in, twice it from crest to trough. 0 makes the face flat.

### Meta lens

![properties-meta-lens](Assets/properties-meta-lens.svg)

A meta lens: a carrier plate with a sub-wavelength pattern on its front, a ridge of its own index in every cell, whose fill is computed from the target frequency and the focal length so the cells delay the wave as a lens would, zone by zone. The wave sees each cell as a mean index between the plate's surroundings and the ridge's, the more of the cell the ridge fills, the higher; a converging lens fills the cells most at the centre and less outward within each zone, where the fill starts over, a diverging one the other way. A new meta lens is cut for a wavelength of 32 pixels with a focal length of 500, cells of 4 pixels and ridges 32 pixels high of index 2.00: just the height a full turn of phase needs, which the form shows. The anti-reflection coating can lie on the ridges' tops (the front) and the plate's back. On the pictures, a selected meta lens shows a grip on the plate's back corner that sets the length and the width. Settings 1 to 6 are those every part shares, 7 to 14 the glass and mirror settings without the sides.

15. **Length**: Thickness of the carrier plate along the axis in pixels; the pattern stands on its front, outside it.
16. **Width**: Width of the plate and its pattern across the axis in pixels.
17. **Target frequency**: The source frequency the lens is designed for, on the same scale as a source's frequency. The cells' fill follows from the wavelength it gives, so the lens focuses this frequency at the focal length; other frequencies focus elsewhere.
18. **Wavelength**: The wavelength the target frequency gives in a medium of index 1. A read-out.
19. **Focal length**: Distance in pixels from the lens to its focus, for a plane wave arriving along the axis; a diverging lens spreads the wave as if from a focus that far behind it.
20. **Lens type**: Converging, the fill greatest at the centre and falling outward within each zone; or diverging, the fill growing outward.
21. **Zones**: The zones from the centre to the rim, each a full turn of phase. A read-out.
22. **Pattern index**: Refractive index of the ridges, usually above the plate's. The higher, the lower the pattern can be.
23. **Pitch**: The width of a cell of the pattern in pixels, one ridge per cell. Well below the wavelength, so the wave sees each cell as a mean index rather than diffracting at it.
24. **Height**: The height of the ridges in front of the plate in pixels. A full turn of phase takes the wavelength over the pattern index less one; a lower pattern cannot delay enough.
25. **Full-turn height**: The height at which a filled cell delays by a whole turn. A read-out.

### Meta prism

![properties-meta-prism](Assets/properties-meta-prism.svg)

A meta prism: a carrier plate with a sub-wavelength pattern on its front whose cells delay the wave in a line across the face, so a wave arriving along the axis leaves turned by a set angle, as a blazed grating's first order. The fill of the cells climbs across each zone and starts over at the next; a zone is a full turn of phase, the wavelength over the sine of the angle wide, and must stay a few cells wide, so steep angles need fine cells. A new meta prism is cut for a wavelength of 32 pixels and turns the wave by 20°, with cells of 4 pixels and ridges 32 pixels high of index 2.00. The anti-reflection coating can lie on the ridges' tops and the plate's back. Settings 1 to 6 are those every part shares, 7 to 14 the glass and mirror settings without the sides, 15 and 16 the plate's length and width as the meta lens's.

17. **Target frequency**: The source frequency the prism is designed for, on the same scale as a source's frequency; the wavelength it gives sets the cells' fill. Another frequency is turned by another angle.
18. **Wavelength**: The wavelength the target frequency gives in a medium of index 1. A read-out.
19. **Deflection**: The angle in degrees by which a wave arriving along the axis is turned, counter-clockwise on the screen for a positive angle; 0 turns nothing.
20. **Zone width**: The width of a zone across the face, a full turn of phase. A read-out.
21. **Zones**: How many zones fit across the face. A read-out.
22. **Pattern index**, **Pitch**, **Height**, **Full-turn height**: As the meta lens's, badges 22 to 25.

### Meta axicon

![properties-meta-axicon](Assets/properties-meta-axicon.svg)

A meta axicon: a carrier plate with a sub-wavelength pattern on its front whose cells delay the wave in proportion to the distance from the axis, so the two halves of a wave arriving along the axis are turned toward it by a set angle and cross along a line behind the surface rather than at a point: a long, narrow focus that keeps its shape over its length. The fill is greatest on the axis and falls outward within each zone, as a converging lens's, but at an even pace. A new meta axicon is cut for a wavelength of 32 pixels with a cone angle of 10°, cells of 4 pixels and ridges 32 pixels high of index 2.00. Settings 1 to 6 are those every part shares, 7 to 14 the glass and mirror settings without the sides, 15 and 16 the plate's length and width as the meta lens's.

17. **Target frequency**: The source frequency the axicon is designed for; the wavelength it gives sets the cells' fill.
18. **Wavelength**: The wavelength the target frequency gives in a medium of index 1. A read-out.
19. **Cone angle**: The angle in degrees by which each half of the wave is turned toward the axis. The steeper, the shorter the line of focus and the narrower the zones.
20. **Line of focus**: How far behind the surface the line of focus reaches: half the width over the tangent of the cone angle. A read-out.
21. **Zone width**: The width of a zone across the face, a full turn of phase. A read-out.
22. **Zones**: How many zones fit from the centre to the rim. A read-out.
23. **Pattern index**, **Pitch**, **Height**, **Full-turn height**: As the meta lens's, badges 22 to 25, here 23 to 26.

### XOR gate

![properties-xor-gate](Assets/properties-xor-gate.svg)

An optical XOR gate: two input waveguides, a phase section putting the lower input a quarter period ahead, and a coupler whose upper output carries their difference. It lights when one input is lit, not both, for coherent inputs at the design frequency. Feed the inputs with equal waves, the same frequency, amplitude and phase; a missing wave is a logical 0. The outputs line up with the inputs of a gate placed after it, so gates can be chained, as in the example XOR AND OR logic. Badges 1 to 6 are the shared settings.

7. **Design frequency**: The source frequency the gate is designed for, on the same scale as a source's frequency: the frequency of the sources that drive it. The wavelength, and from it the core width, the coupler and the phase section, follow from it, so the gate works at this frequency only.
8. **Wavelength**: The wavelength the design frequency gives in a medium of index 1, with the simulation's source frequency scale. A read-out.
9. **Core index**: Refractive index of the waveguide cores, 1.00 to 2.55 in steps of 0.01. It must be above the cladding's; the larger the difference, the narrower the cores and the tighter the bends.
10. **Cladding index**: Refractive index of the cladding around the cores, 1.00 to 2.55 in steps of 0.01.
11. **AR coating**: Thickness in pixels of the anti-reflection band at the ends ticked below, with the index √n of the core and of the cladding; 0 for none. A quarter-wave band, the least reflecting, is λ / (4 √n) pixels thick; the form shows it for the core.
12. **AR on input side**: Puts the anti-reflection band on the input end. Switch it off where the gate takes its light from a gate before it, so the wave passes through the junction.
13. **AR on output side**: Puts the anti-reflection band on the output end. Switch it off where the gate hands its light to a gate after it, so the wave passes through the junction.
14. **Quarter-wave coating**: The thickness of the least reflecting coating on the core, λ / (4 √n). A read-out.
15. **Port spacing**: Distance between the two inputs, and between the two outputs, in pixels. A value below what the design needs to keep the arms apart is raised to it, and the box shows the spacing used.
16. **Upper output**: Lets the upper output's light leave the gate. Switched off, an absorber with a soft rise takes it in the last stretch of the arm.
17. **Lower output**: Lets the lower output's light leave the gate. Switched off, an absorber with a soft rise takes it in the last stretch of the arm.
18. **Core width**: The layout, computed from the design wavelength and the indices with the simulator's own wave equation: the width of the cores.
19. **Coupler gap**: The gap between the arms in the coupler.
20. **Coupler length**: The length of the coupler.
21. **Total length**: The length of the whole gate along its axis.
22. **Input A**: Where the upper input expects its wave, in canvas pixels; put a source there, or the output of a gate before it.
23. **Input B**: The lower input.
24. **Upper output**: Where the upper output's light leaves, in canvas pixels.
25. **Lower output**: Where the lower output's light leaves.

### AND gate

![properties-and-gate](Assets/properties-and-gate.svg)

An optical AND gate: the same coupler with the lower input a quarter period behind, so the upper output carries the sum. It is brightest with both inputs lit, about four times a single one; a passive linear gate has no truer AND. Its form is the XOR gate's; see [XOR gate](#xor-gate).

### OR gate

![properties-or-gate](Assets/properties-or-gate.svg)

An optical OR gate: the same coupler with the lower input a twelfth of a period ahead, so the upper output carries half the power of either input and of both. It lights with one input or both. Its form is the XOR gate's; see [XOR gate](#xor-gate).

## Probes

A probe measures the wave where you put it and draws a graph over the picture of the layer it measures. A point probe records the field's amplitude, or the intensity's value or its integral, at one pixel over the steps of the run; a line probe records the values along a line from A to B at each frame. Add a probe with the second button of the project tree, which opens a menu: a standard probe, or under Probabilistic a counting or an imaging detector; it lands at the centre of the canvas, its line end to the right.

A probabilistic detector is a line probe that turns the intensity along its line into quantised detections, as a photon counter sees light: each step, every point of the line detects a random number of times, Poisson-distributed about the intensity there times the detector sensitivity (the detector's quantum efficiency and the scale from intensity to detections in one number). A counting detector adds the line's detections up and draws them step by step over time, the pulses of a photomultiplier: the denser, the brighter the line. An imaging detector keeps the detections of every point and draws them along the line, added up from the start of the run, so the intensity profile builds up from single detections as it does in a SPAD array or an image intensifier; scrubbed back, the picture shows fewer. Its histogram has bins of a settable width in pixels: the detections of a bin's pixels are added up, so wider bins fill faster and show less detail. The detections are drawn from the project's seed, so a project gives the same detections every time, and a new seed others. A probe's measurements are exported as CSV from its context menu, in the tree or on its graph box: a point probe one row per step, a line probe one row per frame, semicolons between the values and a header row; when the run is not complete, it is made first. Moving a probe or changing its graph needs no new run; a probe switched on, or a new one, is recorded by the next run, which starts by itself.

### The kinds of probes

![menu-add-probe](Assets/menu-add-probe.svg)

The Add probe menu of the project tree.

1. **Standard probe**: A standard probe: the field's amplitude, or the intensity's value or its integral, at a point over time or along a line.
2. **Probabilistic**: The detectors, in a submenu of their own.

![menu-add-probe-probabilistic](Assets/menu-add-probe-probabilistic.svg)

The Probabilistic submenu.

1. **Counting detector**: A counting detector: the detections of the whole line, step by step, as a photomultiplier's pulses, drawn over time. Their rate is the line's intensity times the sensitivity; each detection is a chance.
2. **Imaging detector**: An imaging detector: the detections at every point of the line added up over the run, as a SPAD array or an image intensifier builds an image, drawn along the line.

### Probe

![properties-probe](Assets/properties-probe.svg)

The form of a point probe of the field.

1. **Name**: Name shown in the project tree and in the graph title.
2. **Active**: Samples and draws this probe.
3. **Start after step**: Step at which recording starts; 0 records from the first step.
4. **Detector**: A standard probe, or a probabilistic detector: counting, the detections of the whole line step by step over time; or imaging, the detections at every point of the line added up over the run. A detector is a line probe of the integrated intensity: choosing one sets the layer, the measurement and the kind.
5. **Detector sensitivity**: Expected detections per unit of intensity per step at a point of the line; detectors only. 0 detects nothing. A point source's wave is faint by the time it reaches a line a hundred pixels away, so the sensitivity is in the tens.
6. **Bin size**: Pixels of the line per bin of an imaging detector's histogram, 1 to 20; the detections of a bin's pixels are added up, and the graph shows the bin's total across them.
7. **Layer**: Field or Intensity: the output the probe samples and is drawn on.
8. **Measurement**: Recorded quantity. The field offers its amplitude only; the intensity layer its value, or the value integrated over the run.
9. **Kind**: A single point, or a line from A to B.
10. **A x**: X of the probe pixel, or of the start of the line, in canvas pixels.
11. **A y**: Y of the probe pixel, or of the start of the line, in canvas pixels.
12. **B x**: X of the end of the line in canvas pixels; line probes only.
13. **B y**: Y of the end of the line in canvas pixels; line probes only.
14. **Box size**: Nominal size of the graph box drawn over the output, from tiny to extra large, or custom; a box larger than the canvas is shrunk to fit it, aspect kept.
15. **Box x**: Left edge of the graph box in canvas pixels. A box placed on the probe's point A moves with the probe; dragging it close snaps it on.
16. **Box y**: Top edge of the graph box in canvas pixels. A box placed on the probe's point A moves with the probe; dragging it close snaps it on.
17. **Box width**: Width of the graph box in canvas pixels; custom box size only. A box larger than the canvas is shrunk to fit it, aspect kept.
18. **Box height**: Height of the graph box in canvas pixels; custom box size only. A box larger than the canvas is shrunk to fit it, aspect kept.
19. **Step axis**: Running, a sliding window of the last steps, or Full, the whole run; point probes only.
20. **Window**: Width of the sliding step window, in steps.
21. **Auto-scale steps**: Lets the step axis follow the samples recorded so far; point probes only.
22. **Auto-scale values**: Fits the value axis to the data instead of the fixed minimum and maximum.
23. **Value axis**: A linear or a logarithmic value axis; intensity probes only, the field is always linear, its amplitude axis symmetric about zero.
24. **Decades**: Decades a logarithmic value axis spans below its top; smaller values, zero included, sit on the bottom line.
25. **Value minimum**: Lower bound of the value axis when it is not auto-scaled.
26. **Value maximum**: Upper bound of the value axis when it is not auto-scaled.

### Line probe

![properties-line-probe](Assets/properties-line-probe.svg)

The same form for a line probe of the intensity with a logarithmic value axis. A line probe's graph shows the values along the line from A to B at the frame shown, so the step axis settings (19 to 21) do not apply; both ends have a handle on the picture, and the line between them shows where it measures.

### Counting detector

![properties-counting-detector](Assets/properties-counting-detector.svg)

The same form for a counting detector. The detector sets the layer, the measurement and the kind (7 to 9), which are greyed; the graph shows the detections of the whole line step by step, so the step axis settings (19 to 21) apply, and the bin size (6) does not.

### Imaging detector

![properties-imaging-detector](Assets/properties-imaging-detector.svg)

The same form for an imaging detector. The detector sets the layer, the measurement and the kind (7 to 9), which are greyed; the graph shows the detections along the line from A to B, added up to the frame shown, in bins of the bin size (6), so the step axis settings (19 to 21) do not apply.

## Colour schemes

The pictures are coloured through a colour scheme: a gradient of colour stops mapped onto the values of a picture. The Display settings pick a scheme for each picture from the schemes that ship with the program, your own schemes, saved as .wdscheme files in the `ColorSchemes` folder of your WaveDude Next data (`%LOCALAPPDATA%\WaveDudeNext\ColorSchemes`), and the schemes of the project, which are saved in its file and listed in the Colour schemes group of the tree. The picker in the Display form also offers Load from file… and Edit a copy in the project…, which makes a project scheme from the chosen one so you can change it. Renaming a project scheme keeps the pictures that use it.

![properties-colour-scheme](Assets/properties-colour-scheme.svg)

The form of a project's colour scheme.

1. **Name**: Name the display settings refer to.
2. **Kind**: Field or Intensity, the output the scheme is meant for; the pickers list it for that output only.
3. **Description**: Free text: what the scheme is, where it comes from.
4. **Based on**: Copies the stops of an existing scheme into this one, to change them from there.
5. **A stop**: Its position along the gradient, 0 at the lowest value to 1 at the highest; its colour swatch, which opens the picker (Space opens it, Enter keeps the colour, Escape puts the old one back); the colour as #RRGGBB, applied with Enter; and a button that removes the stop.
6. **Add a stop**: Inserts a stop in the widest gap, with the gradient's colour there.

In the picker, the square sets the saturation (Left, Right) and the brightness (Up, Down) and the bar the hue (Up and Down by a degree); Shift moves by ten steps; the channel boxes take 0 to 255.
