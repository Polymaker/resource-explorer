using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceExplorer.Native.Enums
{
    [Flags]
    public enum ImageLoadOption : uint
    {
        LR_DEFAULTCOLOR = 0x00000000,
        LR_CREATEDIBSECTION = 0x00002000,
        LR_DEFAULTSIZE = 0x00000040,
        LR_LOADTRANSPARENT = 0x00000020,
        LR_LOADFROMFILE = 0x00000010
    }
}
