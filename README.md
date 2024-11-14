ZWO SDK (ASI, EAF, EFW) for .NET Standard
============================================

This package wraps the ZWO Camera API SDK, the EFW SDK and the EAF SDK as available from: https://www.zwoastro.com/software/ and uses
NuGet's built-in multi-platform support to allow any consumer of this package to have the respective native (platforms supported: Win-x86, Win-x64, Linux 32 and 64 bit, Linux ARM7 and ARM8 (64-bit)) library available.

Usage
=====

One way is to use `using static ZWOptical.ASISDK.ASICameraDll2` and start by querying: `ASIGetSDKVersion` followed by `ASIGetNumOfConnectedCameras`.

EAF (Electronic Automatic Focuser) SDK
======================================

To use the EAF SDK, you can use `using static ZWOptical.SDK.EAFFocuser1_6` and start by querying: `EAFGetSDKVersion` followed by `EAFGetNum`.

EFW (Electronic Filter Wheel) SDK
=================================

To use the EFW SDK, you can use `using static ZWOptical.SDK.EFW1_7` and start by querying: `EFWGetSDKVersion` followed by `EFWGetNum`.
