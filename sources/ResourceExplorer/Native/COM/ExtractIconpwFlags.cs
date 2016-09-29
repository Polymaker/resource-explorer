using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceExplorer.Native.COM
{
    [Flags()]
    public enum ExtractIconpwFlags : uint
    {
        GilDontcache = 0x0010,
        GilNotfilename = 0x0008,
        GilPerclass = 0x0004,
        GilPerinstance = 0x0002,
        GilSimulatedoc = 0x0001,
        GilShield = 0x0200,
        GilForcenoshield = 0x0400
    }
}
