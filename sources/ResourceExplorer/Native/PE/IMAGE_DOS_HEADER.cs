using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ResourceExplorer.Native.PE
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DOS_HEADER// DOS .EXE header
    {
        public ushort Magic;              // Magic number
        public ushort Cblp;               // Bytes on last page of file
        public ushort Cp;                 // Pages in file
        public ushort Crlc;               // Relocations
        public ushort Cparhdr;            // Size of header in paragraphs
        public ushort Minalloc;           // Minimum extra paragraphs needed
        public ushort Maxalloc;           // Maximum extra paragraphs needed
        public ushort Ss;                 // Initial (relative) SS value
        public ushort Sp;                 // Initial SP value
        public ushort Csum;               // Checksum
        public ushort Ip;                 // Initial IP value
        public ushort Cs;                 // Initial (relative) CS value
        public ushort Lfarlc;             // File address of relocation table
        public ushort Ovno;               // Overlay number
        public ushort Res_0;              // Reserved words
        public ushort Res_1;              // Reserved words
        public ushort Res_2;              // Reserved words
        public ushort Res_3;              // Reserved words
        public ushort Oemid;              // OEM identifier (for e_oeminfo)
        public ushort Oeminfo;            // OEM information; e_oemid specific
        public ushort Res2_0;             // Reserved words
        public ushort Res2_1;             // Reserved words
        public ushort Res2_2;             // Reserved words
        public ushort Res2_3;             // Reserved words
        public ushort Res2_4;             // Reserved words
        public ushort Res2_5;             // Reserved words
        public ushort Res2_6;             // Reserved words
        public ushort Res2_7;             // Reserved words
        public ushort Res2_8;             // Reserved words
        public ushort Res2_9;             // Reserved words
        public uint   Lfanew;             // File address of new exe header
    }
}
