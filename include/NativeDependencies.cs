using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace ZWOptical.SDK;

/// <summary>
/// Makes libudev's symbols globally visible on Linux before any ZWO native is loaded.
/// </summary>
/// <remarks>
/// <para><b>The defect this works around is in the vendor's binaries.</b> `libEFW1.7.so` (filter
/// wheels) and `libEAFFocuser1.6.so` (focusers) call fifteen or sixteen udev functions each --
/// <c>udev_new</c>, <c>udev_enumerate_scan_devices</c>, <c>udev_device_get_devnode</c> and the rest --
/// and their <c>DT_NEEDED</c> lists name only libstdc++, libm, libgcc_s and libc. They record no
/// dependency on libudev at all, so nothing causes it to be loaded, and the first call into either
/// library kills the process outright:</para>
/// <code>symbol lookup error: libEFW1.7.so: undefined symbol: udev_new</code>
/// <para>The camera and QHY libraries do not have this problem; `libASICamera2.so` correctly declares
/// <c>libusb-1.0.so.0</c>. It is specific to the two device families, and it is why running any
/// TianWen app that references the vendor drivers used to need an <c>LD_PRELOAD</c> in front of it.</para>
/// <para><b>Why <see cref="NativeLibrary.Load(string)"/> cannot do this.</b> It loads with
/// <c>RTLD_LOCAL</c>, whose whole point is that the library's symbols are NOT added to the global
/// namespace used to resolve libraries loaded afterwards. A local libudev would therefore satisfy
/// nothing. <c>RTLD_GLOBAL</c> is the flag that makes the symbols available to the ZWO libraries the
/// runtime loads later, and reaching it means calling <c>dlopen</c> directly.</para>
/// <para><b>Best effort by design.</b> A machine with no libudev cannot drive an EFW or an EAF
/// whatever we do here, and failing the module initializer would take down apps that only wanted a
/// camera. The outcome is recorded in <see cref="LinuxUdevLoaded"/> so a host can say why a filter
/// wheel was not found, instead of leaving a support question with no answer.</para>
/// </remarks>
internal static partial class NativeDependencies
{
    // glibc and musl agree on these (bits/dlfcn.h): resolve now, and add the symbols to the global
    // namespace so the ZWO libraries loaded after this one can bind against them.
    private const int RtldNow = 0x00002;
    private const int RtldGlobal = 0x00100;

    /// <summary>
    /// Whether libudev was loaded globally on this process. False off Linux, and false on a Linux
    /// box without libudev, where the EFW and EAF natives cannot work.
    /// </summary>
    internal static bool LinuxUdevLoaded { get; private set; }

    private static int _attempted;

    /// <summary>
    /// Loads libudev globally, once per process. Idempotent and safe to call from several static
    /// constructors racing on different threads.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT annotated with a platform: it is called from static constructors that run
    /// wherever the app runs, and the <see cref="OperatingSystem.IsLinux"/> test below is the guard
    /// the analyzer checks <see cref="DlOpen"/> against.
    /// </remarks>
    internal static void EnsureLinuxUdevIsGloballyVisible()
    {
        if (!OperatingSystem.IsLinux() || Interlocked.Exchange(ref _attempted, 1) != 0)
        {
            return;
        }

        try
        {
            LinuxUdevLoaded = DlOpen("libudev.so.1", RtldNow | RtldGlobal) != IntPtr.Zero;
        }
        catch (DllNotFoundException)
        {
            // No dlopen to call: not a platform that can load the vendor natives either.
        }
        catch (EntryPointNotFoundException)
        {
            // dlopen moved into libc in glibc 2.34; on anything older it is in libdl, and a host
            // that old is not one these drivers are supported on.
        }
    }

    /// <remarks>
    /// Declared linux-only rather than merely called that way, so the platform analyzer checks the
    /// guard instead of a comment asserting it. There is no equivalent to annotate for: this package
    /// ships natives for Linux and Windows only, and the Windows ones have no such defect.
    /// <para>The <c>libc</c> name is right for glibc, where dlopen moved out of libdl in 2.34, and
    /// the catch in the caller covers a C library that does not answer to it.</para>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    [LibraryImport("libc", EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr DlOpen(string file, int flags);
}
