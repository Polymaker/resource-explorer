using ResourceExplorer.Native.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResourceExplorer.Native.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public BitmapCompressionMode Compression;
        public uint SizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint ColorUsed;
        public uint ColorImportant;

        public static BITMAPINFOHEADER Default => new BITMAPINFOHEADER() { Size = (uint)Marshal.SizeOf<BITMAPINFOHEADER>() };
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct BITMAPFILEHEADER
    {
        [FieldOffset(0)]
        public short Header;
        [FieldOffset(2)]
        public int Size;
        [FieldOffset(6)]
        public short Reserved1;
        [FieldOffset(8)]
        public short Reserved2;
        [FieldOffset(10)]
        public int DataOffset;

        public static BITMAPFILEHEADER Default => new BITMAPFILEHEADER()
        {
            Header = BitConverter.ToInt16(new byte[] { 0x42, 0x4D }, 0)
        };
    }
}
