using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ResourceExplorer.Native.Types
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr buffer;
        //[MarshalAs(UnmanagedType.LPWStr)]
        //public String Buffer;

        public static implicit operator string(UNICODE_STRING us)
        {
            return us.ToString();
        }

        public static UNICODE_STRING Create(string s)
        {
            var uStr = new UNICODE_STRING
            {
                Length = (ushort)(s.Length * 2),
                MaximumLength = (ushort)(s.Length + 2),
                buffer = Marshal.StringToHGlobalUni(s)
            };
            return uStr;
        }

        public override string ToString()
        {
            return Marshal.PtrToStringUni(buffer);
        }
    }
}
