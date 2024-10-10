using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.ASISDK
{
    public struct ASI_ID
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        public byte[] id;
        public string ID
        {
            get { return Encoding.ASCII.GetString(id).TrimEnd((Char)0); }
        }
    }
}
