using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.ASISDK
{
    public static class EAFFocuser16
    {
        [DllImport("EAFFocuser1.6", EntryPoint = "EAFGetNum", CallingConvention = CallingConvention.Cdecl)]
        public static extern int EAFGetNum();
    }
}
