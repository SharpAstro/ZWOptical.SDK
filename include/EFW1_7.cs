using System;
using System.Runtime.InteropServices;
using System.Text;

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
        public struct EFW_INFO : IZWODeviceInfo
        {
            private int _id;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            private byte[] name;
            public int slotNum;

            public int ID => _id;

            public string Name => Encoding.ASCII.GetString(name).TrimEnd((char)0);

            public bool Open() => EFWOpen(ID) is EFW_ERROR_CODE.EFW_SUCCESS;

            public bool Close() => EFWClose(ID) is  EFW_ERROR_CODE.EFW_SUCCESS;

            public SDK_ID? SerialNumber => EFWGetSerialNumber(ID, out var sn) is EFW_ERROR_CODE.EFW_SUCCESS ? sn : null as SDK_ID?;

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
        public static extern EFW_ERROR_CODE EFWGetSerialNumber(int ID, out SDK_ID sn);

        [DllImport(EFWSharedLib, EntryPoint = "EFWSetID", CallingConvention = CallingConvention.Cdecl)]
        public static extern EFW_ERROR_CODE EFWSetID(int ID, SDK_ID alias);
    }
}
