# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ZWOptical.SDK — a .NET Standard 2.0 NuGet package providing C# P/Invoke bindings to ZWO's native C SDKs for astronomical cameras (ASI), electronic focusers (EAF), and filter wheels (EFW). Part of the SharpAstro ecosystem.

## Build

```bash
dotnet build -c Release
```

NuGet package is generated on every build (`GeneratePackageOnBuild`). No test projects exist.

## CI/CD

GitHub Actions (`.github/workflows/dotnet.yml`): builds on push/PR to `main`, auto-publishes `.nupkg` to NuGet.org on merge. Versioning uses `3.4.<run_number>` scheme set via `-p:Version=...` in CI.

## Architecture

All source is in `/include/`. The project is a thin wrapper — each file maps 1:1 to a native SDK:

- **ASICamera2.cs** — Camera SDK bindings. `ASI_CAMERA_INFO` implements `ICMOSNativeInterface` from TianWen.DAL.
- **EAFFocuser1_6.cs** — Focuser SDK bindings. `EAF_INFO` implements `INativeDeviceInfo`.
- **EFW1_7.cs** — Filter wheel SDK bindings. `EFW_INFO` implements `INativeDeviceInfo`.
- **ZWO_ID.cs** — Shared `ZWO_ID` struct for serial numbers/device aliases.
- **DeviceIterator.cs** — Generic `DeviceIterator<TDeviceInfo>` extending `NativeDeviceIteratorBase<T>` from TianWen.DAL; dispatches enumeration across all three device types.

The original C headers (`.h` files in `/include/`) serve as reference for the bindings.

## Key Dependency

`TianWen.DAL` provides base abstractions (`INativeDeviceInfo`, `ICMOSNativeInterface`, `NativeDeviceIteratorBase<T>`, `CMOSControlType`, `CMOSErrorCode`, etc.). All public types live under the `ZWOptical.SDK` namespace.

## Native Libraries

Platform-specific native binaries in `/lib/` are packed into the NuGet package for 6 targets: win-x86, win-x64, linux-x86, linux-x64, linux-arm, linux-arm64. Each platform includes `ASICamera2`, `EAFFocuser1.6`, and `EFW1.7` shared libraries.

## Conventions

- Namespace `ZWOptical.SDK` does not match folder structure — this is intentional (suppressed via `GlobalSuppressions.cs`).
- P/Invoke method signatures should match the C headers exactly.
- Struct layouts use `[StructLayout(LayoutKind.Sequential)]` to match native memory layout.
