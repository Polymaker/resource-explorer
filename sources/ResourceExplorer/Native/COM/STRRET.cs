using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ResourceExplorer.Native.COM
{
    [StructLayout(LayoutKind.Sequential)]
    public struct STRRET
    {
        public uint uType;
        public STRRETinternal data;
    }

    // this works too...from Unions.cs
    [StructLayout(LayoutKind.Explicit, Size = 520)]
    public struct STRRETinternal
    {
        [FieldOffset(0)]
        public IntPtr pOleStr;

        [FieldOffset(0)]
        public IntPtr pStr;  // LPSTR pStr;   NOT USED

        [FieldOffset(0)]
        public uint uOffset;

    }
}
