using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.SDK
{
    public static class EAFFocuser1_6
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

        public struct EAF_INFO : IZWODeviceInfo
        {
            private int _id;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            private byte[] _name;

            public int MaxStep;//fixed maximum position

            public int ID => _id;

            public string Name => Encoding.ASCII.GetString(_name).TrimEnd((char)0);

            public bool Open() => EAFOpen(ID) is EAF_ERROR_CODE.EAF_SUCCESS;

            public bool Close() => EAFClose(ID) is  EAF_ERROR_CODE.EAF_SUCCESS;

            public SDK_ID? SerialNumber => EAFGetSerialNumber(ID, out var sn) is EAF_ERROR_CODE.EAF_SUCCESS ? sn : null as SDK_ID?;
        }

        const string EAFSharedLib = "EAFFocuser1.6";

        public static readonly int EAF_ID_MAX = 128;
        public static readonly int ZWO_VENDOR_ID = 0x03C3;
        public static readonly int ZWO_EAF_PRODUCT_ID = 0x1f10;

        /// <summary>
        /// This should be the first API to be called
        /// get number of connected EAF focuser, call this API to refresh device list if EAF is connected
        /// or disconnected.
        /// </summary>
        /// <returns>number of connected EAF focuser. 1 means 1 focuser is connected.</returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetNum", CallingConvention = CallingConvention.Cdecl)]
        public static extern int EAFGetNum();

        /// <summary>
        /// Set the alias of the EAF.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="alias"></param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>Not connected to focuser <see cref="EAFOpen(int)"/></description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></item>
        ///   <item><term>EAF_ERROR_NOT_SUPPORTED</term><description>the firmware does not support setting alias</description></item>
        /// </list>
        /// </returns>  
        [DllImport(EAFSharedLib, EntryPoint = "EAFSetID", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFSetID(int ID, SDK_ID alias);

        /// <summary>
        /// Check if the device is EAF.
        /// </summary>
        /// <param name="vid">Vendor ID of the device, use <see cref="ZWO_VENDOR_ID"/> for ZWO Vendor</param>
        /// <param name="pid">PID of the device, use <see cref="ZWO_EAF_PRODUCT_ID"/> for ZWO EAF</param>
        /// <returns>If the device is EAF, return 1, otherwise return 0</returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int EAFCheck(int vid, int pid);

        /// <summary>
        /// Get ID of focuser.
        /// </summary>
        /// <param name="index">the index of focuser, from 0 to N - 1, N is returned by <see cref="EAFGetNum"/></param>
        /// <param name="ID">the ID is a unique integer, between 0 to <see cref="EAF_ID_MAX"/> - 1, after opened,
        /// all the operation is base on this ID, the ID will not change.</param>
        /// 
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_GENERAL_ERROR</term><description>number of opened focuser reaches the maximum value.</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetID", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFGetID(int index, out int ID);

        /// <summary>
        /// Connect to focuser.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// 
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_GENERAL_ERROR</term><description>number of opened focuser reaches the maximum value.</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFOpen", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFOpen(int ID);

        /// <summary>
        /// Disconnect to focuser.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// 
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFClose", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFClose(int ID);

        [DllImport(EAFSharedLib, EntryPoint = "EAFGetSDKVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _EAFGetSDKVersion();

        /// <summary>
        /// Get SDK version, e.g. 1.4.0
        /// </summary>
        /// <returns>EAF SDK Version</returns>
        public static Version EAFGetSDKVersion() => Common.ParseVersionString(_EAFGetSDKVersion());

        /// <summary>
        /// Get focuser properties.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="info">EAF property structure</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetProperty", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFGetProperty(int ID, out EAF_INFO info);

        [DllImport(EAFSharedLib, EntryPoint = "EAFGetFirmwareVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE _EAFGetFirmwareVersion(int ID, out byte major, out byte minor, out byte build);

        /// <summary>
        /// Get firmware version of focuser.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="version">Firmware version</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not connected</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></item>
        /// </list>
        /// </returns>
        public static EAF_ERROR_CODE EAFGetFirmwareVersion(int ID, out Version version)
        {
            var code = _EAFGetFirmwareVersion(ID, out var major, out var minor, out var build);

            version = new Version(major, minor, build);

            return code;
        }

        /// <summary>
        /// Get the serial number from a EAF
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="serialNumber">Focuser serial number (if supported)</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not connected</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></item>
        ///   <item><term>EFW_ERROR_NOT_SUPPORTED</term><description>the firmware does not support serial number</description></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetSerialNumber", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFGetSerialNumber(int ID, out SDK_ID serialNumber);

        /// <summary>
        /// Move focuser to an absolute position.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="iStep">step value is between 0 to EAF_INFO::MaxStep</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFMove", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFMove(int ID, int iStep);

        /// <summary>
        /// Stop moving.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFStop", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFStop(int ID);

        /// <summary>
        /// Check if the focuser is moving.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="isMoving">True if EAF is moving</param>
        /// <param name="isUsingHandControl">True if EAF is moving due to hand control key press</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFIsMoving", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFIsMoving(int ID, [MarshalAs(UnmanagedType.I1)] out bool isMoving, [MarshalAs(UnmanagedType.I1)] out bool isUsingHandControl);

        /// <summary>
        /// Get current position.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="position">Absolute focuser position.</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetPosition", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFGetPosition(int ID, out int position);

        /// <summary>
        /// Set as current position.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="position">New current focus position.</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFResetPostion", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFResetPostion(int ID, int position);

        /// <summary>
        /// Get the value of the temperature detector, if it's moved by handle, the temperature value is unreasonable, the value is -273 and return error <see cref="EAF_ERROR_CODE.EAF_ERROR_GENERAL_ERROR"/>.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="temp">Focuser temperature (if valid)</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        ///   <item><term>EAF_ERROR_GENERAL_ERROR</term><description>temperature value is unusable</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetTemp", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFGetTemp(int ID, out float temp);

        /// <summary>
        /// Turn on/off beep, if true the focuser will beep at the moment when it begins to move.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="setBeep">turn on beep if true</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFSetBeep", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFSetBeep(int ID, bool setBeep);

        /// <summary>
        /// Get if beep is turned on.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="isBeepSet">true if beep is turned on</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetBeep", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFGetBeep(int ID, [MarshalAs(UnmanagedType.I1)] out bool isBeepSet);

        /// <summary>
        /// Set the maximum position (in steps).
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="maxStep">maximum position</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        ///   <item><term>EAF_ERROR_MOVING</term><description>focuser is moving, should wait until idle</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFSetMaxStep", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFSetMaxStep(int ID, int maxStep);

        /// <summary>
        /// Get the maximum position (in steps).
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="maxStep">maximum position</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        ///   <item><term>EAF_ERROR_MOVING</term><description>focuser is moving, should wait until idle</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetMaxStep", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFGetMaxStep(int ID, out int maxStep);

        /// <summary>
        /// Get the position range.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="range">range</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        ///   <item><term>EAF_ERROR_MOVING</term><description>focuser is moving, should wait until idle</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFStepRange", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFStepRange(int ID, out int range);

        /// <summary>
        /// Set moving direction of focuser.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="setReverse">if set as true, the focuser will move along reverse direction</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFSetReverse", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFSetReverse(int ID, bool setReverse);

        /// <summary>
        /// Get moving direction of focuser.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="isReverse">true focuser direction is reversed</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFGetReverse", CallingConvention = CallingConvention.Cdecl)]
        private static extern EAF_ERROR_CODE EAFGetReverse(int ID, [MarshalAs(UnmanagedType.I1)] out bool isReverse);

        /// <summary>
        /// Set backlash of focuser.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="backlash">backlash (0..255), in steps</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        ///   <item><term>EAF_ERROR_INVALID_VALUE</term><description>needs to be between 0 and 255</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFSetBacklash", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFSetBacklash(int ID, int backlash);

        /// <summary>
        /// Get backlash of focuser.
        /// </summary>
        /// <param name="ID">the ID of focuser</param>
        /// <param name="backlash">set backlash value</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EAF_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EAF_ERROR_CLOSED</term><description>not opened</description></item>
        ///   <item><term>EAF_ERROR_ERROR_STATE</term><description>focuser is in error state</description></item>
        ///   <item><term>EAF_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EAF_SUCCESS</term><description>operation succeeds</description></term></item>
        /// </list>
        /// </returns>
        [DllImport(EAFSharedLib, EntryPoint = "EAFSetBacklash", CallingConvention = CallingConvention.Cdecl)]
        public static extern EAF_ERROR_CODE EAFSetBacklash(int ID, out int backlash);
    }
}
