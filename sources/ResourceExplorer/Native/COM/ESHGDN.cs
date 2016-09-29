using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceExplorer.Native.COM
{
    public enum ESHGDN
    {
        SHGDN_NORMAL = 0x0000,
        SHGDN_INFOLDER = 0x0001,
        SHGDN_FOREDITING = 0x1000,
        SHGDN_FORADDRESSBAR = 0x4000,
        SHGDN_FORPARSING = 0x8000,
    }
}
