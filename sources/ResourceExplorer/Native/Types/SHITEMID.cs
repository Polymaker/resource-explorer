using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ResourceExplorer.Native.Types
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHITEMID
    {
        /// <summary>
        /// The size of identifier, in bytes, including cb itself.
        /// </summary>
        public ushort cb;
        /// <summary>
        /// A variable-length item identifier.
        /// </summary>
        public byte[] abID;
    }
}
