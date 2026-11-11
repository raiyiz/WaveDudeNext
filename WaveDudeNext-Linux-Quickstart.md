# WaveDude Next on CachyOS / Linux

A small quick-start for running **WaveDude Next** natively on Linux, including CachyOS.

WaveDude Next's original interface uses Windows Presentation Foundation (WPF), which is Windows-specific. The repository also contains **`WaveDudeNext.App`**, an Avalonia-based frontend that can run on Linux and uses the same simulation engine and application models. Use that frontend instead of trying to launch the Windows executable with Wine.

Project: <https://github.com/graealex/WaveDudeNext>

## 1. Install the prerequisites

On CachyOS, install the .NET 10 SDK and the common X11/Wayland desktop libraries:

```bash
sudo pacman -Syu dotnet-sdk-10.0 xorg-xwayland libx11 libice libsm fontconfig mesa libinput gtk3
```

Check that the SDK is available:

```bash
dotnet --list-sdks
```

You should see a version beginning with `10.0`.

> **Wayland note:** On a Wayland desktop such as niri, Avalonia may use XWayland. If XWayland is not already installed and running, the window may not appear.

## 2. Download the source

Open a terminal and run:

```bash
git clone https://github.com/graealex/WaveDudeNext.git
cd WaveDudeNext
```

If you already cloned the repository, move into its directory instead. To update a clean checkout of the current branch later, use `git pull --ff-only`.

## 3. Make file dialogs work on CachyOS / niri

On some Linux desktop setups, Avalonia’s default D-Bus file picker may fail to show a dialog even though the application itself runs. On CachyOS with niri, the workaround that has been confirmed to work is to make Avalonia use GTK’s file picker instead. Install GTK 3 if it is not already installed:

```bash
sudo pacman -S gtk3
```

In `WaveDudeNext.App/Program.cs`, change `BuildAvaloniaApp()` to:

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new X11PlatformOptions
        {
            UseDBusFilePicker = false
        })
        .LogToTrace();
```

`UseDBusFilePicker = false` disables Avalonia’s D-Bus file-picker path for its X11 backend, allowing it to use the GTK picker instead. This matters when running the X11 backend through XWayland on niri. Keep the existing `using Avalonia;` at the top of `Program.cs`.

**Rebuild after editing `Program.cs`**; simply restarting with `--no-build` would keep using the old binary:

```bash
dotnet build WaveDudeNext.App/WaveDudeNext.App.csproj -c Release
```

Then launch the newly built app:

```bash
dotnet run --project WaveDudeNext.App/WaveDudeNext.App.csproj -c Release --no-build --no-restore
```

Test `File → Open`, `File → Save As` and an export.

## 4. Run WaveDude Next

Run the Avalonia frontend directly:

```bash
dotnet run --project WaveDudeNext.App/WaveDudeNext.App.csproj -c Release
```

The first run restores NuGet packages and compiles the application. The window may look a little different from the Windows version; it is the separate Avalonia interface.

### Optional: build first, then launch

To separate build errors from startup errors, run:

```bash
dotnet build WaveDudeNext.App/WaveDudeNext.App.csproj -c Release --verbosity minimal
```

If the build succeeds, launch the compiled app without rebuilding:

```bash
dotnet run --project WaveDudeNext.App/WaveDudeNext.App.csproj -c Release --no-build --no-restore
```

## 5. Important: choose the correct project

Use this project on Linux:

```text
WaveDudeNext.App/WaveDudeNext.App.csproj
```

It is the Avalonia frontend. The project `WaveDudeNext.WPF` is the original Windows-only WPF interface. The full solution includes that WPF project, so building the entire solution is not the first thing to try when setting up Linux.

## 6. GPU acceleration

WaveDude Next's simulation engine uses OpenCL for GPU acceleration and can fall back to its CPU engine if a usable GPU/OpenCL device is unavailable.

Get the app working first; GPU setup is a separate step. For acceleration, Linux needs an OpenCL ICD loader **and** a compatible OpenCL implementation for the graphics driver. On Arch-based systems, `ocl-icd` provides the loader but does not supply the vendor's implementation by itself.

You can inspect available OpenCL devices with `clinfo` after installing the appropriate OpenCL packages for your GPU. Package names vary by GPU vendor and driver.

## 7. Export limitation

MP4 export in the shared code uses **Windows Media Foundation**, so that export path is not available natively on Linux. Other implemented formats include PNG/JPEG image sequences, OpenEXR, MPEG-1, AVI and MOV; availability and behaviour can depend on the current frontend/version.

## 8. Troubleshooting

### The first-run .NET welcome message appears

That is normal. The SDK may print a welcome message and mention workload verification. The message alone does not indicate that WaveDude Next failed.

### You see a workload-verification warning

For example:

```text
An issue was encountered verifying workloads.
```

This does not by itself prove that a workload is missing or that the app failed. Check the build/run output that follows. Do not run `dotnet workload update` just because this warning appeared; first establish whether the build actually fails.

### Save/Open/Export does nothing

On CachyOS with niri, check that `Program.cs` contains the `X11PlatformOptions` configuration above with `UseDBusFilePicker = false`, and that GTK 3 is installed (`sudo pacman -S gtk3`). After changing the code, rebuild the project before launching it again. A restart with `--no-build` alone does not apply source changes.

### The build fails

Run the build separately and keep the complete output, especially the first `error` line:

```bash
dotnet build WaveDudeNext.App/WaveDudeNext.App.csproj -c Release --verbosity minimal
```

### The build succeeds, but no window appears

Launch with `--no-build --no-restore` as shown above and inspect the terminal output. On a Wayland desktop, also check that XWayland is available. The terminal's first exception and any inner exception are usually the most useful diagnostic details.

### OpenCL/GPU errors appear

First confirm that the app runs on the CPU. Then check whether the system has a working OpenCL ICD and a device visible to `clinfo`. Installing the loader alone is not enough; the driver-specific implementation is also required.

## 9. Quick reference

From inside the repository directory:

```bash
# Run (restore/build automatically if needed)
dotnet run --project WaveDudeNext.App/WaveDudeNext.App.csproj -c Release

# Build only
dotnet build WaveDudeNext.App/WaveDudeNext.App.csproj -c Release --verbosity minimal

# Launch after a successful build
dotnet run --project WaveDudeNext.App/WaveDudeNext.App.csproj -c Release --no-build --no-restore
```

Enjoy the waves! 🌊
