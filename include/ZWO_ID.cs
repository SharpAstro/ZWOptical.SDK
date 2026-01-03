using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.SDK
{
    public readonly struct ZWO_ID
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        private readonly byte[] _id;

        public override string ToString() => Encoding.ASCII.GetString(_id).TrimEnd((char)0);
    }
}
