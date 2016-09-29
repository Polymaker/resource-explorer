using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ResourceExplorer.Native.Types
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ITEMIDLIST
    {
        /// <summary>
        /// A list of item identifiers.
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public SHITEMID mkid;
    }
}
