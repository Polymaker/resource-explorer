using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ResourceExplorer.Native.COM
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214fa-0000-0000-c000-000000000046")]
    public interface IExtractIcon
    {
        [PreserveSig]
        int GetIconLocation(ExtractIconuFlags uFlags,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder szIconFile,
            int cchMax,
            out int piIndex,
            out ExtractIconpwFlags pwFlags);

        [PreserveSig]
        int Extract(string pszFile,
            uint nIconIndex,
            out IntPtr phiconLarge,
            out IntPtr phiconSmall,
            uint nIconSize);
    }
}
