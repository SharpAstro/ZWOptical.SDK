using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TianWen.DAL;

namespace ZWOptical.SDK;

public static partial class EFWFilter
{
    // libEFWFilter.so names no libudev in its DT_NEEDED and calls sixteen of its functions anyway, so
    // those symbols have to be in the global namespace before the runtime loads it, or the
    // first call into this class kills the process with a symbol lookup error. A static
    // constructor runs before this class's first P/Invoke, which is exactly when that load
    // happens. See NativeDependencies for why NativeLibrary.Load cannot do it.
    static EFWFilter() => NativeDependencies.EnsureLinuxUdevIsGloballyVisible();

    public enum EFW_ERROR_CODE
    {
        EFW_SUCCESS = 0,
        EFW_ERROR_INVALID_INDEX,
        EFW_ERROR_INVALID_ID,
        EFW_ERROR_INVALID_VALUE,
        EFW_ERROR_REMOVED,
        EFW_ERROR_MOVING,
        EFW_ERROR_ERROR_STATE,
        EFW_ERROR_GENERAL_ERROR,//other error
        EFW_ERROR_NOT_SUPPORTED,
        /// <summary>New in SDK 1.8, inserted before <see cref="EFW_ERROR_CLOSED"/>.</summary>
        EFW_ERROR_INVALID_LENGTH,
        /// <summary>The wheel is not open. <c>EFWGetProperty</c>, <c>EFWGetPosition</c> and
        /// <c>EFWSetPosition</c> all answer this for a wheel that has not been opened.</summary>
        /// <remarks>
        /// <b>Pinned at 11 by measurement, not by the header.</b> The 1.8.4 header counts it to 10,
        /// one past <see cref="EFW_ERROR_INVALID_LENGTH"/>, but the 1.8.4 library answers 11 for an
        /// unopened EFW(8PosPlan), from all three calls, on 2026-09-23 (SDK 1.7 answered 9, which is
        /// where its own header put it). So the library carries a member the header does not, and
        /// 10 is left unnamed rather than guessed at. Nothing should branch on this value alone:
        /// <see cref="DeviceIterator{TDeviceInfo}"/> retries a failed property read with the wheel
        /// open whatever the code, so the next drop moving it again costs nothing there.
        /// </remarks>
        EFW_ERROR_CLOSED = 11,
        EFW_ERROR_END = -1
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct EFW_INFO : INativeDeviceInfo
    {
        private readonly int _id;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        private readonly byte[] _name;
        private readonly int _slotNum;

        public int ID => _id;

        public int NumberOfSlots => _slotNum;

        public string Name => Encoding.ASCII.GetString(_name).TrimEnd((char)0);

        /// <summary>
        /// Opens the wheel AND reads its properties, because the SDK will not move a wheel it has not
        /// read the properties of.
        /// </summary>
        /// <remarks>
        /// Straight after <c>EFWOpen</c>, <c>EFWSetPosition</c> answers <c>EFW_ERROR_GENERAL_ERROR</c>
        /// for EVERY target, including the slot the wheel already sits in; after one
        /// <c>EFWGetProperty</c> on the open handle, every move succeeds. Measured on an EFW(8PosPlan)
        /// (firmware 3.0.9) 2026-09-23, identically under SDK 1.7 and 1.8.4, while SharpCap moved the
        /// same wheel fine. The SDK learns the slot count from that read and range-checks the target
        /// against it, which is why the order the 1.8.4 header now documents is Open, then
        /// GetProperty, then the moves. Doing it here means no caller can get the order wrong; a wheel
        /// whose properties cannot be read is reported as not opened, since it could not be moved.
        /// </remarks>
        public bool Open()
            => EFWOpen(ID) is EFW_ERROR_CODE.EFW_SUCCESS
               && EFWGetProperty(ID, out _) is EFW_ERROR_CODE.EFW_SUCCESS;

        public bool Close() => EFWClose(ID) is EFW_ERROR_CODE.EFW_SUCCESS;

        /// <summary>
        /// Factory serial as a 16-char hex string, or null if not programmed. Same
        /// convention as <see cref="ASICameraInfo.SerialNumber"/>: the native 8-byte
        /// ID is raw binary, rendered in hexadecimal. All-zero / all-0xFF patterns
        /// are treated as missing.
        /// </summary>
        public string? SerialNumber
        {
            get
            {
                if (EFWGetSerialNumber(ID, out var sn) is not EFW_ERROR_CODE.EFW_SUCCESS)
                    return null;
                var hex = sn.ToHexString();
                return hex is "0000000000000000" or "FFFFFFFFFFFFFFFF" ? null : hex;
            }
        }

        public bool IsUSB3Device => false;

        public string CustomId => Name;
    };

    const string EFWSharedLib = "EFWFilter";

    // Functions with non-blittable struct parameters use DllImport
    [DllImport(EFWSharedLib, EntryPoint = "EFWGetProperty", CallingConvention = CallingConvention.Cdecl)]
    public static extern EFW_ERROR_CODE EFWGetProperty(int ID, out EFW_INFO pInfo);

    [DllImport(EFWSharedLib, EntryPoint = "EFWGetSerialNumber", CallingConvention = CallingConvention.Cdecl)]
    public static extern EFW_ERROR_CODE EFWGetSerialNumber(int ID, out ZWO_ID sn);

    [DllImport(EFWSharedLib, EntryPoint = "EFWSetID", CallingConvention = CallingConvention.Cdecl)]
    public static extern EFW_ERROR_CODE EFWSetID(int ID, ZWO_ID alias);

    // Functions with blittable parameters use LibraryImport
    [LibraryImport(EFWSharedLib, EntryPoint = "EFWGetSDKVersion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial IntPtr _EFWGetSDKVersion();

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWGetNum")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int EFWGetNum();

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWGetID")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWGetID(int index, out int ID);

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWOpen")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWOpen(int index);

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWClose")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWClose(int ID);

    /// <summary>
    /// get position of slot
    /// </summary>
    /// <param name="ID">the ID of filter wheel</param>
    /// <param name="pPosition">pointer to slot position, this value is between 0 to M - 1, M is slot number and -1 if filter wheel is moving</param>
    /// <returns>
    /// <list type="table">
    ///   <listheader><term>Code</term><description>Meaning</description></listheader>
    ///   <item><term>EFW_SUCCESS</term><description>operation succeeds</description></item>
    ///   <item><term>EFW_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
    ///   <item><term>EFW_ERROR_CLOSED</term><description>filter wheel disconnected/not opened</description></item>
    ///   <item><term>EFW_ERROR_REMOVED</term><description>filter wheel is removed</description></item>
    ///   <item><term>EFW_ERROR_ERROR_STATE</term><description>filter wheel is in error state</description></item>
    /// </list>
    /// </returns>
    [LibraryImport(EFWSharedLib, EntryPoint = "EFWGetPosition")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWGetPosition(int ID, out int pPosition);

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWSetPosition")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWSetPosition(int ID, int Position);

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWSetDirection")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWSetDirection(int ID, [MarshalAs(UnmanagedType.I1)] bool bUnidirectional);

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWGetDirection")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWGetDirection(int ID, [MarshalAs(UnmanagedType.I1)] out bool bUnidirectional);

    [LibraryImport(EFWSharedLib, EntryPoint = "EFWGetFirmwareVersion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EFW_ERROR_CODE EFWGetFirmwareVersion(int ID, out byte pbMajor, out byte pbMinor, out byte pbBuild);

    public static Version EFWGetSDKVersion() => Common.ParseVersionString(_EFWGetSDKVersion());
}
