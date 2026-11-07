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

This mode also fails on binding warnings/errors and writes `/tmp/wavedude-linux-workspace.png`. Native file-picker dialogs and pointer dragging are not automated by these checks.
