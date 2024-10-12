using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.SDK
{
    public struct SDK_ID
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private byte[] _id;

        public override string ToString() => Encoding.ASCII.GetString(_id).TrimEnd((char)0);
    }
}
