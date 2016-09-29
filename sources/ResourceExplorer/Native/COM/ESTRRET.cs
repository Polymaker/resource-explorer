using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceExplorer.Native.COM
{
    public enum ESTRRET : int
    {
        eeRRET_WSTR = 0x0000,    // Use STRRET.pOleStr
        STRRET_OFFSET = 0x0001,    // Use STRRET.uOffset to Ansi
        STRRET_CSTR = 0x0002    // Use STRRET.cStr
    }
}
