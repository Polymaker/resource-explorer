using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ResourceExplorer.Native.Enums;
using ResourceExplorer.Native.Types;

namespace ResourceExplorer.Native.API
{
    public static class Shell32
    {

        [DllImport("shell32.dll")]
        public static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath,  [In, Out] ushort lpiIcon);

        [DllImport("shell32.dll", EntryPoint = "ExtractIconA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr ExtractIcon(int hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr phiconLarge, IntPtr phiconSmall, uint nIcons);

        [DllImport("shell32.dll")]
        public static extern int SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out()] out IntPtr pidl, uint sfgaoIn, [Out()] out SFGAOF psfgaoOut);

        [DllImport("shell32.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void SHBindToParent(
        [In, MarshalAs(UnmanagedType.LPStruct)] ITEMIDLIST pidl,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out object ppv,
        IntPtr ppidlLast);

        [DllImport("shell32.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void SHBindToParent(
        IntPtr pidl,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out object ppv,
        IntPtr ppidlLast);


    }
}
