using System;
using System.Runtime.InteropServices;
using System.Text;
using TianWen.DAL;

namespace ZWOptical.SDK
{
    public static class EFW1_7
    {
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

            public bool Open() => EFWOpen(ID) is EFW_ERROR_CODE.EFW_SUCCESS;

            public bool Close() => EFWClose(ID) is  EFW_ERROR_CODE.EFW_SUCCESS;

            public string SerialNumber => EFWGetSerialNumber(ID, out var sn) is EFW_ERROR_CODE.EFW_SUCCESS ? sn.ToString() : null;

            public bool IsUSB3Device => false;

            public string CustomId => Name;
        };

        const string EFWSharedLib = "EFW1.7";

        [DllImport(EFWSharedLib, EntryPoint = "EFWGetSDKVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _EFWGetSDKVersion();

        public static Version EFWGetSDKVersion() => Common.ParseVersionString(_EFWGetSDKVersion());

        [DllImport(EFWSharedLib, EntryPoint = "EFWGetNum", CallingConvention = CallingConvention.Cdecl)]
        public static extern int EFWGetNum();

        [DllImport(EFWSharedLib, EntryPoint = "EFWGetID", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWGetID(int index, out int ID);

        [DllImport(EFWSharedLib, EntryPoint = "EFWGetProperty", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWGetProperty(int ID, out EFW_INFO pInfo);

        [DllImport(EFWSharedLib, EntryPoint = "EFWOpen", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWOpen(int index);


        [DllImport(EFWSharedLib, EntryPoint = "EFWClose", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWClose(int ID);
      
        /// <summary>
        /// get position of slot
        /// </summary>
        /// <param name="ID">the ID of filter wheel</param>
        /// <param name="pPosition">pointer to slot position, this value is between 0 to M - 1, M is slot number and -1 if filter wheel is moving</param>
        /// <returns>
        /// <list type="table">
        ///   <listheader><term>Code</term><description>Meaning</description></listheader>
        ///   <item><term>EFW_ERROR_INVALID_ID</term><description>invalid ID value</description></item>
        ///   <item><term>EFW_ERROR_CLOSED</term><description>filter wheel disconnected/not opened</description></item>
        ///   <item><term>EFW_ERROR_REMOVED</term><description>filter wheel is removed</description></item>
        ///   <item><term>EFW_ERROR_REMOVED</term><description>focuser is removed</description></item>
        ///   <item><term>EFW_ERROR_ERROR_STATE</term><description>operation succeeds</description></item>
        /// </list>
        /// </returns>
        [DllImport(EFWSharedLib, EntryPoint = "EFWGetPosition", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWGetPosition(int ID, out int pPosition);
      

        [DllImport(EFWSharedLib, EntryPoint = "EFWSetPosition", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWSetPosition(int ID, int Position);

        [DllImport(EFWSharedLib, EntryPoint = "EFWSetDirection", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWSetDirection(int ID, [MarshalAs(UnmanagedType.I1)]bool bUnidirectional);

      
        [DllImport(EFWSharedLib, EntryPoint = "EFWGetDirection", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWGetDirection(int ID, [MarshalAs(UnmanagedType.I1)]out bool bUnidirectional);

        [DllImport(EFWSharedLib, EntryPoint = "EFWGetFirmwareVersion", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWGetFirmwareVersion(int ID, out byte pbMajor, out byte pbMinor, out byte pbBuild);

        [DllImport(EFWSharedLib, EntryPoint = "EFWGetSerialNumber", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWGetSerialNumber(int ID, out ZWO_ID sn);

        [DllImport(EFWSharedLib, EntryPoint = "EFWSetID", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWSetID(int ID, ZWO_ID alias);
    }
}
