using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceExplorer.Native.COM
{
    public enum ESFGAO : uint
    {
        SFGAO_CANCOPY = 0x00000001,
        SFGAO_CANMOVE = 0x00000002,
        SFGAO_CANLINK = 0x00000004,
        SFGAO_LINK = 0x00010000,
        SFGAO_SHARE = 0x00020000,
        SFGAO_READONLY = 0x00040000,
        SFGAO_HIDDEN = 0x00080000,
        SFGAO_FOLDER = 0x20000000,
        SFGAO_FILESYSTEM = 0x40000000,
        SFGAO_HASSUBFOLDER = 0x80000000,
    }
}
