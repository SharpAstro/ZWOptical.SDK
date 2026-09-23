ZWO SDK (ASI, EAF, EFW) for .NET
=================================

This package wraps ZWO's three device SDKs (the ASI camera SDK, the EAF focuser SDK and the EFW
filter wheel SDK, from https://www.zwoastro.com/software/) and uses NuGet's multi-platform support
to put the right native library beside any consumer.

| | Camera (ASI) | Focuser (EAF) | Filter wheel (EFW) |
|---|---|---|---|
| Vendor SDK | 1.41 | 1.8.1 | 1.8.4 |
| Binding | `ZWOptical.SDK.ASICamera2` | `ZWOptical.SDK.EAFFocuser` | `ZWOptical.SDK.EFWFilter` |

Platforms: **win-x64, linux-x64, linux-arm64, linux-arm, osx-x64, osx-arm64.** Every library carries
one name on every platform (`EFWFilter.dll`, `libEFWFilter.so`, `libEFWFilter.dylib`), which ZWO's
own downloads do not (`EFW_filter.dll` on Windows); `lib/SOURCE.txt` names the downloads the
package was built from.

> **6.0 is a breaking release.** `EFW1_7` is now `EFWFilter` and `EAFFocuser1_6` is `EAFFocuser`,
> with the native libraries renamed to match, and win-x86 and linux-x86 are no longer shipped.
> 4.x and later target .NET 10+; the last .NET Standard 2.0 version is 3.x.

Usage
=====

`using static ZWOptical.SDK.ASICamera2`, then `ASIGetSDKVersion` and `ASIGetNumOfConnectedCameras`;
`using static ZWOptical.SDK.EAFFocuser`, then `EAFGetSDKVersion` and `EAFGetNum`;
`using static ZWOptical.SDK.EFWFilter`, then `EFWGetSDKVersion` and `EFWGetNum`.
`DeviceIterator<TDeviceInfo>` enumerates any of the three as TianWen.DAL device infos.

Platform notes
==============

- **Windows: the filter wheel needs no runtime.** ZWO's `EFW_filter.dll` imports `MSVCR90.dll`, the
  Visual C++ 2008 runtime, through a side-by-side manifest, so it loads only where ZWO's installer
  or a 2008 redistributable put that runtime. The `EFWFilter.dll` here is linked from ZWO's own
  static library with a static runtime instead, and imports only Windows system libraries.
  `tools/fetch-natives.ps1` builds it; `tools/efw-wrapper/` holds its export list and the one shim
  the link needs. The EAF build is ZWO's Windows 10 one (Bluetooth over WinRT), not the Windows 7
  one.
- **Linux: libudev is loaded for the focuser and the wheel.** Both libraries call libudev without
  declaring it, so the package loads it globally before either is used; a host without libudev
  cannot drive those two. The camera needs `libusb-1.0.so.0`. `lib/udev/` holds ZWO's rules for
  `/etc/udev/rules.d`, needed to open the devices without root.
- **Linux: EAF over Bluetooth is not packaged.** ZWO's x64 EAF download also carries
  `libWrapperSdbus.so` and `libsdbus-c++.so.2`, the Bluetooth transport. `libEAFFocuser.so` does
  not depend on them, USB focusers work without them, and they are left out (issue #8).
- **macOS: the camera needs libusb.** `libASICamera2.dylib` loads `libusb-1.0.0.dylib`: on Apple
  silicon from Homebrew (`brew install libusb`), on Intel from beside the library. The focuser and
  wheel libraries need nothing.

Updating the vendor SDKs
========================

Download the three SDK zips from ZWO, then run `tools/fetch-natives.ps1` (Windows, with Visual
Studio's C++ tools, for the wheel's wrapper) and commit what changed. It unpacks every platform,
renames each library to its one name, builds `EFWFilter.dll`, checks that it imports nothing but
system libraries, and copies the headers and manuals. Then compare the new headers with the
bindings in `include/`: an enum member ZWO inserts renumbers everything after it.
