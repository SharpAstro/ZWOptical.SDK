using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TianWen.DAL;

namespace ZWOptical.SDK;

public static partial class EAFFocuser1_6
{
    public enum EAF_ERROR_CODE
    {
        EAF_SUCCESS = 0,

        EAF_ERROR_INVALID_INDEX,

        EAF_ERROR_INVALID_ID,

        EAF_ERROR_INVALID_VALUE,

        /// <summary>
        /// Failed to find the focuser, maybe the focuser has been removed
        /// </summary>
        EAF_ERROR_REMOVED,

        /// <summary>
        /// Focuser is moving
        /// </summary>
        EAF_ERROR_MOVING,

        /// <summary>
        /// focuser is in error state
        /// </summary>
        EAF_ERROR_ERROR_STATE,

        /// <summary>
        /// Other unspecified error
        /// </summary>
        EAF_ERROR_GENERAL_ERROR,

        EAF_ERROR_NOT_SUPPORTED,

        EAF_ERROR_CLOSED,

        EAF_ERROR_END = -1
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EAF_INFO : INativeDeviceInfo
    {
        private readonly int _id;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
        private readonly byte[] _name;

        public int MaxStep;//fixed maximum position

        public int ID => _id;

        public string Name => Encoding.ASCII.GetString(_name).TrimEnd((char)0);

        public bool Open() => EAFOpen(ID) is EAF_ERROR_CODE.EAF_SUCCESS;

        public bool Close() => EAFClose(ID) is EAF_ERROR_CODE.EAF_SUCCESS;

        /// <summary>
        /// Factory serial as a 16-char hex string, or null if not programmed. Same
        /// convention as <see cref="ASICameraInfo.SerialNumber"/>: the native 8-byte
        /// ID is raw binary, rendered in hexadecimal. All-zero / all-0xFF patterns
        /// are treated as missing.
        /// </summary>
        public string SerialNumber
        {
            get
            {
                if (EAFGetSerialNumber(ID, out var sn) is not EAF_ERROR_CODE.EAF_SUCCESS)
                    return null;
                var hex = sn.ToHexString();
                return hex is "0000000000000000" or "FFFFFFFFFFFFFFFF" ? null : hex;
            }
        }

        public bool IsUSB3Device => false;

        public string CustomId => Name;
    }

    const string EAFSharedLib = "EAFFocuser1.6";

    public static readonly int EAF_ID_MAX = 128;
    public static readonly int ZWO_VENDOR_ID = 0x03C3;
    public static readonly int ZWO_EAF_PRODUCT_ID = 0x1f10;

    // Functions with non-blittable struct parameters use DllImport
    [DllImport(EAFSharedLib, EntryPoint = "EAFSetID", CallingConvention = CallingConvention.Cdecl)]
    public static extern EAF_ERROR_CODE EAFSetID(int ID, ZWO_ID alias);

    [DllImport(EAFSharedLib, EntryPoint = "EAFGetProperty", CallingConvention = CallingConvention.Cdecl)]
    public static extern EAF_ERROR_CODE EAFGetProperty(int ID, out EAF_INFO info);

    [DllImport(EAFSharedLib, EntryPoint = "EAFGetSerialNumber", CallingConvention = CallingConvention.Cdecl)]
    public static extern EAF_ERROR_CODE EAFGetSerialNumber(int ID, out ZWO_ID serialNumber);

    // Functions with blittable parameters use LibraryImport
    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetNum")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int EAFGetNum();

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFCheck")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int EAFCheck(int vid, int pid);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetID")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFGetID(int index, out int ID);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFOpen")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFOpen(int ID);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFClose")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFClose(int ID);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetSDKVersion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial IntPtr _EAFGetSDKVersion();

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetFirmwareVersion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial EAF_ERROR_CODE _EAFGetFirmwareVersion(int ID, out byte major, out byte minor, out byte build);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFMove")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFMove(int ID, int iStep);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFStop")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFStop(int ID);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFIsMoving")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFIsMoving(int ID, [MarshalAs(UnmanagedType.I1)] out bool isMoving, [MarshalAs(UnmanagedType.I1)] out bool isUsingHandControl);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetPosition")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFGetPosition(int ID, out int position);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFResetPostion")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFResetPostion(int ID, int position);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetTemp")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFGetTemp(int ID, out float temp);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFSetBeep")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFSetBeep(int ID, [MarshalAs(UnmanagedType.I1)] bool setBeep);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetBeep")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFGetBeep(int ID, [MarshalAs(UnmanagedType.I1)] out bool isBeepSet);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFSetMaxStep")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFSetMaxStep(int ID, int maxStep);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetMaxStep")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFGetMaxStep(int ID, out int maxStep);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFStepRange")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFStepRange(int ID, out int range);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFSetReverse")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFSetReverse(int ID, [MarshalAs(UnmanagedType.I1)] bool setReverse);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetReverse")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial EAF_ERROR_CODE EAFGetReverse(int ID, [MarshalAs(UnmanagedType.I1)] out bool isReverse);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFSetBacklash")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFSetBacklash(int ID, int backlash);

    [LibraryImport(EAFSharedLib, EntryPoint = "EAFGetBacklash")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial EAF_ERROR_CODE EAFGetBacklash(int ID, out int backlash);

    /// <summary>
    /// Get SDK version, e.g. 1.4.0
    /// </summary>
    /// <returns>EAF SDK Version</returns>
    public static Version EAFGetSDKVersion() => Common.ParseVersionString(_EAFGetSDKVersion());

    /// <summary>
    /// Get firmware version of focuser.
    /// </summary>
    /// <param name="ID">the ID of focuser</param>
    /// <param name="version">Firmware version</param>
    public static EAF_ERROR_CODE EAFGetFirmwareVersion(int ID, out Version version)
    {
        var code = _EAFGetFirmwareVersion(ID, out var major, out var minor, out var build);

        version = new Version(major, minor, build);

        return code;
    }
}
