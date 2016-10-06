using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ResourceExplorer.Native.PE
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_IMPORT_DIRECTORY
    {
        public uint ImportLookupTable;
        public uint TimeDateStamp;
        public uint ForwarderChain;
        public uint ModuleName;// RVA from base of image
        public uint ImportAddressTable;// RVA from base of image
    }
}
