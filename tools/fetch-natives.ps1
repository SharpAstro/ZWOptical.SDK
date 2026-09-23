<#
.SYNOPSIS
    Unpacks ZWO's three SDK downloads (ASI camera, EAF focuser, EFW filter wheel) into lib/, include/
    and doc/, building the Windows filter-wheel library from ZWO's static one on the way.

.DESCRIPTION
    Re-run against new downloads, then commit what changed: it is the one way those folders get
    filled, and it records the zips it filled them from in lib/SOURCE.txt, so the repository always
    says which vendor drop it carries.

    Each download is a zip holding a Windows zip and a Linux + macOS tar.bz2 (the ASI one an Android
    zip too, which is not taken). lib/<rid>/ is named by .NET runtime identifier, and every library
    lands under ONE name per device on every platform, which the vendor does not do:

      ZWO's name                                   here
      ASICamera2.dll / libASICamera2.so|.dylib     ASICamera2.dll / libASICamera2.so|.dylib
      EAF_focuser.dll / libEAFFocuser.so|.dylib    EAFFocuser.dll / libEAFFocuser.so|.dylib
      EFW_filter.dll  / libEFWFilter.so|.dylib     EFWFilter.dll  / libEFWFilter.so|.dylib

    so the binding names each library once. Linux ships libX.so as a symlink to libX.so.<version>;
    the real file is taken, under the unversioned name the runtime probes for.

    What is taken, and what is not:
      - win-x64, linux-x64, linux-arm64 (armv8), linux-arm (armv7), osx-x64 and osx-arm64.
      - Not win-x86 or linux-x86 (unused, dropped 2026-09-23), not armv6 (no .NET RID runs there),
        not Android.
      - EAF: the "Windows" build, not "Windows7". Same exports; the Windows 10 build adds Bluetooth
        over WinRT, and .NET 10 does not run on Windows 7 anyway.
      - EAF on Linux x64: libWrapperSdbus.so and libsdbus-c++.so.2 are left out. They are the
        Bluetooth transport, libEAFFocuser.so does not NEED either (it dlopens libWrapperSdbus.so by
        bare name, which never searches the application directory), and USB focusers work without
        them. See issue #8.

    The Windows filter wheel is BUILT, not copied. ZWO's EFW_filter.dll imports MSVCR90.dll, the
    Visual C++ 2008 runtime, bound through an embedded side-by-side manifest (Microsoft.VC90.CRT
    9.0.21022.8), so it loads only where that runtime was installed. EFWFilter.dll is linked here
    from ZWO's own EFW_filter-static.lib with today's STATIC runtime, and imports nothing but
    KERNEL32, HID, SETUPAPI and USER32. tools/efw-wrapper/ holds the export list and the one-symbol
    shim the link needs. This step needs Visual Studio's C++ tools and runs on Windows only; CI never
    runs it, it builds against the committed result.

.PARAMETER AsiZip
    ASI camera SDK download. Defaults to the newest ASI_Camera_SDK*.zip in ~/Downloads.
.PARAMETER EafZip
    EAF focuser SDK download. Defaults to the newest EAF_SDK_V*.zip in ~/Downloads.
.PARAMETER EfwZip
    EFW filter wheel SDK download. Defaults to the newest EFW_SDK*.zip in ~/Downloads.
#>
param(
    [string] $AsiZip,
    [string] $EafZip,
    [string] $EfwZip
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Resolve-Download([string] $given, [string] $filter) {
    if ($given) { return (Resolve-Path $given).Path }
    $found = Get-ChildItem -Path (Join-Path $HOME 'Downloads') -Filter $filter |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
    if (-not $found) { throw "No $filter in ~/Downloads; pass it explicitly." }
    $found
}

$AsiZip = Resolve-Download $AsiZip 'ASI_Camera_SDK*.zip'
$EafZip = Resolve-Download $EafZip 'EAF_SDK_V*.zip'
$EfwZip = Resolve-Download $EfwZip 'EFW_SDK*.zip'

$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path ([System.IO.Path]::GetTempPath()) "zwo-sdk-$([System.Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $work | Out-Null

# Unpacks one download, then the Windows zip and the Linux/macOS tar.bz2 inside it, all into $dir.
function Expand-Download([string] $zip, [string] $dir) {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $dir)
    foreach ($inner in Get-ChildItem -Path $dir -Recurse -Filter '*.zip' | Where-Object Name -notlike '*ANDROID*') {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($inner.FullName, $inner.DirectoryName)
    }
    # Windows' own bsdtar, by path: a Git for Windows PATH puts GNU tar first, which reads "C:" in
    # the archive path as a remote host.
    $bsdtar = Join-Path $env:SystemRoot 'System32\tar.exe'
    foreach ($tar in Get-ChildItem -Path $dir -Recurse -Filter '*.tar.bz2') {
        & $bsdtar -xjf $tar.FullName -C $tar.DirectoryName
        if ($LASTEXITCODE -ne 0) { throw "tar failed on $($tar.Name)" }
    }
}

# The one directory under $dir whose path ends in $tail (forward slashes, wildcards allowed, so a
# version in a folder name need not be written here), or a clear failure.
function Find-Dir([string] $dir, [string] $tail) {
    $hits = Get-ChildItem -Path $dir -Recurse -Directory |
        Where-Object { $_.FullName.Replace('\', '/') -like "*/$tail" }
    if (@($hits).Count -ne 1) { throw "Expected one '$tail' under $dir, found $(@($hits).Count); the vendor layout changed, update the script." }
    $hits.FullName
}

# The real (non-symlink) file in $dir matching $pattern: libX.so is a link to libX.so.<version>.
function Find-RealFile([string] $dir, [string] $pattern) {
    $hit = Get-ChildItem -Path $dir -Filter $pattern -File |
        Where-Object { -not $_.LinkType -and $_.Extension -ne '.a' } |
        Sort-Object Length -Descending | Select-Object -First 1
    if (-not $hit) { throw "No $pattern in $dir; the vendor layout changed, update the script." }
    $hit.FullName
}

function Copy-Native([string] $source, [string] $relative) {
    $target = Join-Path $root $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force
    '{0,-40} ({1:N0} bytes)' -f $relative, (Get-Item -LiteralPath $target).Length
}

# Links EFWFilter.dll from ZWO's static library inside a Visual Studio x64 developer environment.
function Build-EfwWrapper([string] $staticLib, [string] $relative) {
    $installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
    $vs = & (Join-Path $installer 'vswhere.exe') -latest -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if (-not $vs) { throw 'No Visual Studio with the C++ x64 tools; the EFW wrapper cannot be built.' }
    $vcvars = Join-Path $vs 'VC\Auxiliary\Build\vcvars64.bat'

    $build = Join-Path $work 'efw-wrapper'
    New-Item -ItemType Directory -Path $build | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'efw-wrapper\*') $build
    Copy-Item $staticLib (Join-Path $build 'EFW_filter-static.lib')

    # /LTCG because one object in the library was compiled with /GL; the static CRT is LIBCMT plus
    # the static vcruntime and ucrt; legacy_stdio_definitions carries the sprintf the 2008 objects
    # call; MSVCRT is refused outright so nothing can pull the DLL runtime back in.
    $script = @(
        "@cd /d `"$build`""
        "@set `"PATH=$installer;%PATH%`""
        "@call `"$vcvars`" >nul || exit /b 1"
        'cl /nologo /c /MT /O2 strncpy-shim.c || exit /b 1'
        'link /nologo /DLL /LTCG /MACHINE:X64 /OUT:EFWFilter.dll /DEF:EFWFilter.def strncpy-shim.obj EFW_filter-static.lib libcmt.lib libvcruntime.lib libucrt.lib legacy_stdio_definitions.lib hid.lib setupapi.lib user32.lib kernel32.lib /NODEFAULTLIB:msvcrt.lib || exit /b 1'
        'dumpbin /nologo /dependents EFWFilter.dll > dependents.txt || exit /b 1'
    )
    # By full path: with NoDefaultCurrentDirectoryInExePath set, cmd will not run a bare name from
    # the current directory.
    $cmdFile = Join-Path $build 'build.cmd'
    Set-Content -Path $cmdFile -Value $script -Encoding ascii
    cmd /c "`"$cmdFile`""
    if ($LASTEXITCODE -ne 0) { throw "The EFW wrapper link failed (exit $LASTEXITCODE)." }

    # The point of the wrapper is what it does NOT import, so that is checked, not assumed.
    $imports = Get-Content (Join-Path $build 'dependents.txt') | ForEach-Object Trim |
        Where-Object { $_ -match '^[\w.-]+\.dll$' }
    if (-not $imports) { throw 'Read no imports back from dumpbin; the check would pass vacuously.' }
    $allowed = 'KERNEL32.dll', 'HID.DLL', 'SETUPAPI.dll', 'USER32.dll'
    $unexpected = $imports | Where-Object { $_ -notin $allowed }
    if ($unexpected) { throw "EFWFilter.dll imports $($unexpected -join ', '); it must import only system libraries." }

    Copy-Native (Join-Path $build 'EFWFilter.dll') $relative
}

try {
    $asi = Join-Path $work 'asi'; Expand-Download $AsiZip $asi
    $eaf = Join-Path $work 'eaf'; Expand-Download $EafZip $eaf
    $efw = Join-Path $work 'efw'; Expand-Download $EfwZip $efw

    # Windows x64.
    Copy-Native (Join-Path (Find-Dir $asi 'ASI SDK/lib/x64') 'ASICamera2.dll') 'lib/win-x64/ASICamera2.dll'
    Copy-Native (Join-Path (Find-Dir $eaf 'lib/Windows/x64/Release') 'EAF_focuser.dll') 'lib/win-x64/EAFFocuser.dll'
    Build-EfwWrapper (Join-Path (Find-Dir $efw 'EFW_Windows_SDK_V*/lib/x64/Release') 'EFW_filter-static.lib') 'lib/win-x64/EFWFilter.dll'

    # Linux: vendor directory -> RID.
    foreach ($pair in @(@('x64', 'linux-x64'), @('armv8', 'linux-arm64'), @('armv7', 'linux-arm'))) {
        $vendor, $rid = $pair
        Copy-Native (Find-RealFile (Find-Dir $asi "ASI_linux_mac_SDK_V*/lib/$vendor") 'libASICamera2.so*') "lib/$rid/libASICamera2.so"
        Copy-Native (Find-RealFile (Find-Dir $eaf "eaf/lib/$vendor") 'libEAFFocuser.so*') "lib/$rid/libEAFFocuser.so"
        Copy-Native (Find-RealFile (Find-Dir $efw "efw/lib/$vendor") 'libEFWFilter.so*') "lib/$rid/libEFWFilter.so"
    }

    # macOS. ASI names its Intel directory "mac" (a universal i386 + x86_64 binary); EAF and EFW
    # say "mac_x64".
    Copy-Native (Find-RealFile (Find-Dir $asi 'ASI_linux_mac_SDK_V*/lib/mac') 'libASICamera2.dylib*') 'lib/osx-x64/libASICamera2.dylib'
    Copy-Native (Find-RealFile (Find-Dir $asi 'ASI_linux_mac_SDK_V*/lib/mac_arm64') 'libASICamera2.dylib*') 'lib/osx-arm64/libASICamera2.dylib'
    foreach ($pair in @(@('mac_x64', 'osx-x64'), @('mac_arm64', 'osx-arm64'))) {
        $vendor, $rid = $pair
        Copy-Native (Find-RealFile (Find-Dir $eaf "eaf/lib/$vendor") 'libEAFFocuser.dylib*') "lib/$rid/libEAFFocuser.dylib"
        Copy-Native (Find-RealFile (Find-Dir $efw "efw/lib/$vendor") 'libEFWFilter.dylib*') "lib/$rid/libEFWFilter.dylib"
    }

    # udev rules, for a Linux host's /etc/udev/rules.d (not packed).
    Copy-Native (Join-Path (Find-Dir $asi 'ASI_linux_mac_SDK_V*/lib') 'asi.rules') 'lib/udev/asi.rules'
    Copy-Native (Join-Path (Find-Dir $eaf 'eaf/lib') 'eaf.rules') 'lib/udev/eaf.rules'
    Copy-Native (Join-Path (Find-Dir $efw 'efw/lib') 'efw.rules') 'lib/udev/efw.rules'

    # Headers (the binding's reference), manuals and licence, from the Windows drops.
    Copy-Native (Join-Path (Find-Dir $asi 'ASI SDK/include') 'ASICamera2.h') 'include/ASICamera2.h'
    Copy-Native (Join-Path (Find-Dir $eaf 'EAF_Windows_SDK_V*/include') 'EAF_focuser.h') 'include/EAF_focuser.h'
    Copy-Native (Join-Path (Find-Dir $efw 'EFW_Windows_SDK_V*/include') 'EFW_filter.h') 'include/EFW_filter.h'
    foreach ($doc in @(Get-ChildItem (Find-Dir $asi 'ASI SDK/doc') -Filter *.pdf) +
                     @(Get-ChildItem (Find-Dir $eaf 'EAF_Windows_SDK_V*/doc') -Filter *.pdf) +
                     @(Get-ChildItem (Find-Dir $efw 'EFW_Windows_SDK_V*/doc') -Filter *.pdf)) {
        Copy-Native $doc.FullName "doc/$($doc.Name)"
    }
    Copy-Native (Join-Path (Find-Dir $asi 'ASI SDK') 'license.txt') 'license.txt'
    Copy-Native (Join-Path (Find-Dir $asi 'ASI SDK') 'SDK Instruction.txt') 'SDK Instruction.txt'
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}

$lines = foreach ($zip in $AsiZip, $EafZip, $EfwZip) {
    "zip:    $(Split-Path -Leaf $zip)"
    "sha256: $((Get-FileHash -Algorithm SHA256 -Path $zip).Hash.ToLowerInvariant())"
}
$lines += "at:     $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))"
Set-Content -Path (Join-Path $root 'lib/SOURCE.txt') -Value $lines -Encoding utf8
'wrote lib/SOURCE.txt'
