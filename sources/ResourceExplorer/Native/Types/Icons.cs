using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResourceExplorer.Native.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ICONDIR
    {
        public short Reserved;
        /// <summary>
        /// 1 = Icon, 2 = Cursor
        /// </summary>
        public short Type;
        public short ImageCount;

        public static readonly int SIZE = 6;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ICONDIRENTRY
    {
        /// <summary> Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels. </summary>
        public byte Width;
        /// <summary> Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels. </summary>
        public byte Height;
        /// <summary> Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette. </summary>
        public byte Colors;
        public byte Reserved;
        /// <summary>
        /// In ICO format: Specifies color planes. Should be 0 or 1<br />
        /// In CUR format: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
        /// </summary>
        public ushort ValueA;
        /// <summary>
        /// In ICO format: Specifies bits per pixel.<br />
        /// In CUR format: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
        /// </summary>
        public ushort ValueB;
        /// <summary> Specifies the size of the image's data in bytes </summary>
        public uint Size;
        /// <summary> Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file </summary>
        public uint Offset;

        public static readonly int SIZE = 16;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GRPICONDIRENTRY
    {
        /// <summary> Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels. </summary>
        public byte Width;
        /// <summary> Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels. </summary>
        public byte Height;
        /// <summary> Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette. </summary>
        public byte ColorCount;
        public byte Reserved;
        /// <summary>
        /// In ICO format: Specifies color planes. Should be 0 or 1<br />
        /// In CUR format: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
        /// </summary>
        public ushort Planes;
        /// <summary>
        /// In ICO format: Specifies bits per pixel.<br />
        /// In CUR format: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
        /// </summary>
        public ushort BitCount;
        /// <summary> Specifies the size of the image's data in bytes </summary>
        public uint Size;
        /// <summary> Specifies the resource ID </summary>
        public ushort ID;
    }
}
