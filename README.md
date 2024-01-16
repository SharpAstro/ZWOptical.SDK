ZWO ASI Camera API wrapper for .NET Standard
============================================

This package wrappes the ZWO Camera API SDK as available from: https://www.zwoastro.com/software/ and uses
NuGets build=in multi-platform support to allow any consumer of this package to have the respective native
library available.

Usage
=====

One way is to use `using static ZWOptical.ASISDK.ASICameraDll2` and start by querying: `ASIGetSDKVersion` followed by `ASIGetNumOfConnectedCameras`
