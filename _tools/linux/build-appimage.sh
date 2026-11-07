#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Build a self-contained Linux x86-64 AppImage from the current checkout.
set -euo pipefail
repo_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
cd "$repo_root"
if [[ $(uname -s) != Linux || $(uname -m) != x86_64 ]]; then
    echo "Build this package on x86-64 Linux." >&2
    exit 1
fi
for dependency in dotnet mksquashfs sha256sum; do
    command -v "$dependency" >/dev/null || { echo "Missing build prerequisite: $dependency" >&2; exit 1; }
done
appimage_tool=${APPIMAGETOOL:-appimagetool}
appimage_tool=$(command -v "$appimage_tool") || { echo "Set APPIMAGETOOL to the downloaded appimagetool-x86_64.AppImage." >&2; exit 1; }
# appimagetool changes directories internally: resolve relative tool/runtime paths first.
appimage_tool=$(realpath "$appimage_tool")
runtime_args=()
if [[ -n ${APPIMAGE_RUNTIME_FILE:-} ]]; then
    runtime_args=(--runtime-file "$(realpath "$APPIMAGE_RUNTIME_FILE")")
fi
version=${VERSION:-$(dotnet msbuild WaveDudeNext.Avalonia/WaveDudeNext.Avalonia.csproj -getProperty:Version -nologo)}
[[ $version =~ ^[a-zA-Z0-9._+-]+$ ]] || { echo "Invalid VERSION: $version" >&2; exit 1; }
build_root="$repo_root/artifacts/linux-x64"
mkdir -p "$build_root"
# A fresh staging directory avoids packaging files left by an older SDK or build.
staging=$(mktemp -d "$build_root/staging.XXXXXX")
appdir="$staging/WaveDudeNext.AppDir"
mkdir -p "$appdir/usr/lib/wavedude-next" "$appdir/usr/share/icons/hicolor/128x128/apps" "$appdir/usr/share/applications"
dotnet publish WaveDudeNext.Avalonia/WaveDudeNext.Avalonia.csproj \
    -c Release -r linux-x64 --self-contained true \
    -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false \
    -p:Version="$version" -p:DebugType=None -p:DebugSymbols=false \
    -o "$appdir/usr/lib/wavedude-next"
install -m 755 _tools/linux/AppRun "$appdir/AppRun"
install -m 644 _tools/linux/wavedude-next.desktop "$appdir/wavedude-next.desktop"
install -m 644 _tools/linux/wavedude-next.desktop "$appdir/usr/share/applications/wavedude-next.desktop"
install -m 644 WaveDudeNext.WPF/App.png "$appdir/wavedude-next.png"
install -m 644 WaveDudeNext.WPF/App.png "$appdir/usr/share/icons/hicolor/128x128/apps/wavedude-next.png"
ln -s wavedude-next.png "$appdir/.DirIcon"
install -m 644 LICENSE.md README.md LINUX_PORT.md "$appdir/usr/lib/wavedude-next/"
{
    echo "WaveDude Next $version / Linux x86-64"
    echo "Source commit: $(git rev-parse HEAD)"
    echo "SDK: $(dotnet --version)"
    echo "Built from the working tree; local changes are included."
} > "$appdir/usr/lib/wavedude-next/BUILD-INFO.txt"
chmod -R a+rX "$appdir"
if command -v desktop-file-validate >/dev/null; then
    desktop-file-validate "$appdir/wavedude-next.desktop"
fi
output="$build_root/WaveDudeNext-$version-linux-x86_64.AppImage"
# Extract-and-run avoids requiring FUSE on the build host. Limit compression threads for CI hosts.
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "$appimage_tool" --no-appstream \
    "${runtime_args[@]}" --mksquashfs-opt -processors --mksquashfs-opt 2 "$appdir" "$output"
chmod +x "$output"
(cd "$build_root" && sha256sum "$(basename "$output")" > "$(basename "$output").sha256")
{
    cat "$appdir/usr/lib/wavedude-next/BUILD-INFO.txt"
    echo "appimagetool SHA256: $(sha256sum "$appimage_tool" | cut -d ' ' -f 1)"
    if [[ -n ${APPIMAGE_RUNTIME_FILE:-} ]]; then
        echo "Runtime SHA256: $(sha256sum "$APPIMAGE_RUNTIME_FILE" | cut -d ' ' -f 1)"
    else
        echo "Runtime: downloaded by appimagetool"
    fi
} > "$output.build-info.txt"
echo "Release: $output"
echo "Staged application: $appdir"
