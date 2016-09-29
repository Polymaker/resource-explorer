using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceExplorer.Native.COM
{
    [Flags()]
    public enum ExtractIconuFlags : uint
    {
        GilAsync = 0x0020,
        GilDefaulticon = 0x0040,
        GilForshell = 0x0002,
        GilForshortcut = 0x0080,
        GilOpenicon = 0x0001,
        GilCheckshield = 0x0200
    }
}
