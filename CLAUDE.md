# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ZWOptical.SDK — a .NET 10 NuGet package providing C# P/Invoke bindings to ZWO's native C SDKs for astronomical cameras (ASI), electronic focusers (EAF), and filter wheels (EFW). Part of the SharpAstro ecosystem.

## Build

```bash
dotnet build -c Release
```

NuGet package is generated on every build (`GeneratePackageOnBuild`). No test projects exist.

## CI/CD

GitHub Actions (`.github/workflows/dotnet.yml`): builds on push/PR to `main`, auto-publishes `.nupkg` to NuGet.org on a push to main. The version is `<VersionMajorMinor>` in `Directory.Build.props` plus the run number; that one line is the whole of a release bump.

## Vendor drops: `tools/fetch-natives.ps1`, never by hand

The script is the only way `lib/`, the headers in `include/` and `doc/` get filled, and it writes
`lib/SOURCE.txt` naming the three downloads. It renames every library to ONE name per device
(`ASICamera2`, `EAFFocuser`, `EFWFilter`) on every platform, so the bindings name each once; ZWO's
Windows names differ from its Linux ones (`EFW_filter.dll` against `libEFWFilter.so`).

**The Windows filter wheel is built, not copied.** ZWO's `EFW_filter.dll` imports `MSVCR90.dll`
through a side-by-side manifest (VC++ 2008), and loads only where that runtime was installed. The
script links `EFWFilter.dll` from ZWO's `EFW_filter-static.lib` with the static runtime
(`tools/efw-wrapper/`: the export list, and a one-symbol shim for the single object compiled against
the DLL runtime), and fails unless the result imports only KERNEL32, HID, SETUPAPI and USER32.
It needs Visual Studio's C++ tools, so it runs on a Windows dev box and the result is committed; CI
builds against that. Linking 2008 objects with a current toolset is outside what Microsoft
guarantees (binary compatibility starts at VS 2015), so a future drop that fails to link is a real
possibility, and the script says so at once.

**After a drop, diff the new headers against the enums in `include/*.cs`.** ZWO inserts members
mid-enum, which renumbers everything after them, and the bindings are hand transcriptions.

- **The header can be wrong about its own library.** EFW 1.8.4's header puts `EFW_ERROR_CLOSED` at
  10; the library answers 11 for an unopened wheel. It is pinned by measurement, and the iterator
  retries a failed property read with the wheel open whatever the code.
- **Transcribe from the C header, never from ZWO's bundled `ASICameraDll2.cs`.** That file still ends
  `ASI_CONTROL_TYPE` at `ASI_HUMIDITY`, `ASI_ENABLE_DDR` (22, 23), which the header never had and now
  gives to `ASI_FAN_ADJUST` and `ASI_PWRLED_BRIGNT`.

## The EFW and EAF natives need libudev loaded for them

`libEFWFilter.so` and `libEAFFocuser.so` call sixteen udev functions each and name no libudev in
their `DT_NEEDED` (still true of EFW 1.8.4 and EAF 1.8.1), so nothing causes it to be loaded and the
first call into either one kills the process with `undefined symbol: udev_new`. `libASICamera2.so`
is not affected (it declares `libusb-1.0.so.0` properly). `NativeDependencies` loads libudev with
`RTLD_GLOBAL` from the static constructors of those two classes, which is the only way the symbols
become visible to a library the runtime loads afterwards -- `NativeLibrary.Load` uses `RTLD_LOCAL`
and would not help. Best effort: the result is in `NativeDependencies.LinuxUdevLoaded`, and a box
with no libudev simply cannot drive those two device families.

## Architecture

All source is in `/include/`. The project is a thin wrapper — each file maps 1:1 to a native SDK:

- **ASICamera2.cs** — Camera SDK bindings. `ASI_CAMERA_INFO` implements `ICMOSNativeInterface` from TianWen.DAL.
- **EAFFocuser.cs** — Focuser SDK bindings. `EAF_INFO` implements `INativeDeviceInfo`.
- **EFWFilter.cs** — Filter wheel SDK bindings. `EFW_INFO` implements `INativeDeviceInfo`.
- **ZWO_ID.cs** — Shared `ZWO_ID` struct for serial numbers/device aliases.
- **DeviceIterator.cs** — Generic `DeviceIterator<TDeviceInfo>` extending `NativeDeviceIteratorBase<T>` from TianWen.DAL; dispatches enumeration across all three device types.

The original C headers (`.h` files in `/include/`) serve as reference for the bindings.

## Key Dependency

`TianWen.DAL` provides base abstractions (`INativeDeviceInfo`, `ICMOSNativeInterface`, `NativeDeviceIteratorBase<T>`, `CMOSControlType`, `CMOSErrorCode`, etc.). All public types live under the `ZWOptical.SDK` namespace.

## Native Libraries

`lib/<rid>/` per .NET runtime identifier, packed as `runtimes/<rid>/native` for six targets: win-x64,
linux-x64, linux-arm64, linux-arm, osx-x64, osx-arm64 (win-x86 and linux-x86 dropped in 6.0). The
build also copies the natives for its own RID beside the consumer, because a `ProjectReference`
gets no `runtimes/` resolution. `lib/udev/` holds ZWO's rules and is not packed.

## Conventions

- Namespace `ZWOptical.SDK` does not match folder structure — this is intentional (suppressed via `GlobalSuppressions.cs`).
- P/Invoke method signatures should match the C headers exactly.
- Struct layouts use `[StructLayout(LayoutKind.Sequential)]` to match native memory layout.
