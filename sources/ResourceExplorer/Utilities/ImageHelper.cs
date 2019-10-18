using ResourceExplorer.Native.API;
using ResourceExplorer.Native.Enums;
using ResourceExplorer.Native.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResourceExplorer.Utilities
{
    static class ImageHelper
    {
        [DllImport("GdiPlus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int GdipCreateBitmapFromGdiDib(IntPtr pBIH, IntPtr pPix, out IntPtr pBitmap);

        //[DllImport("GdiPlus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        //private static extern int GdipSaveImageToFile(IntPtr pBitmap, string filename, ref Guid clsid, intp);

        private static MethodInfo FromGDIplusMi;

        public static Image ImageFromUnmanagedResource(IntPtr dibPtr, uint dataSize)
        {
            var bih = Marshal.PtrToStructure<BITMAPINFOHEADER>(dibPtr);
            var pixelFormat = GetPixelFormat(bih);

            if (pixelFormat == PixelFormat.Format32bppArgb)
            {
                var newBmp = new Bitmap(bih.Width, bih.Height);
                var bmpData = newBmp.LockBits(new Rectangle(0, 0, bih.Width, bih.Height), 
                    ImageLockMode.ReadWrite, pixelFormat);
 
                int rowSize = bih.Width * 4;

                for (int y = 1; y <= bih.Height; y++)
                {
                    int offset = (int)bih.Size + ((bih.Height - y) * rowSize);
                    Kernel32.CopyMemory(bmpData.Scan0 + ((y-1) * rowSize), dibPtr + offset, (uint)rowSize);
                }

                newBmp.UnlockBits(bmpData);
                return newBmp;
            }
            else
            {
                switch (bih.Compression)
                {
                    case BitmapCompressionMode.BI_RGB:
                        return FromGdiPlus(dibPtr, bih);
                    default:
                        return ReconstructBitmap(dibPtr, bih);
                }
            }
        }

        private static Image FromGdiPlus(IntPtr dibPtr, BITMAPINFOHEADER bih)
        {
            IntPtr dataPtr = dibPtr + (int)(bih.Size);

            int status = GdipCreateBitmapFromGdiDib(dibPtr, dataPtr, out IntPtr bmpPtr);

            if (FromGDIplusMi == null)
                FromGDIplusMi = typeof(Bitmap).GetMethod("FromGDIplus", BindingFlags.Static | BindingFlags.NonPublic);

            if (status == 0 && bmpPtr != IntPtr.Zero)
                return (Bitmap)FromGDIplusMi.Invoke(null, new object[] { bmpPtr });

            return null;
        }

        private static Image ReconstructBitmap(IntPtr dibPtr, BITMAPINFOHEADER bih)
        {
            var pixelFormat = GetPixelFormat(bih);
            int pixelOffset = 0;

            if (pixelFormat == PixelFormat.Format4bppIndexed)
                pixelOffset = 16 * 4;
            else if (pixelFormat == PixelFormat.Format8bppIndexed)
                pixelOffset = 256 * 4;
            else if (bih.ColorUsed > 0)
                pixelOffset = (int)bih.ColorUsed * 4;

            var bfh = BITMAPFILEHEADER.Default;

            int dataSize = (int)bih.Size + (int)bih.SizeImage + pixelOffset;

            bfh.Size = (int)(14 + dataSize);
            bfh.DataOffset = (int)(14 + bih.Size + pixelOffset);

            IntPtr bmpPtr = Marshal.AllocHGlobal((int)bfh.Size);

            try
            {
                Marshal.StructureToPtr(bfh, bmpPtr, false);
                Kernel32.CopyMemory(bmpPtr + 14, dibPtr, (uint)dataSize);

                byte[] buffer = new byte[bfh.Size];
                Marshal.Copy(bmpPtr, buffer, 0, bfh.Size);

                using (var ms = new MemoryStream(buffer))
                    return Image.FromStream(ms, true, true);
            }
            finally
            {
                Marshal.FreeHGlobal(bmpPtr);
            }
        }

        public static Icon IconFromUnmanagedResource(IntPtr dibPtr, uint dataSize)
        {
            var bih = Marshal.PtrToStructure<BITMAPINFOHEADER>(dibPtr);

            int bmpDataSize = (int)dataSize;
            int totalSize = ICONDIR.SIZE + ICONDIRENTRY.SIZE + bmpDataSize;

            IntPtr iconPtr = Marshal.AllocHGlobal(totalSize);
            try
            {
                var dir = new ICONDIR()
                {
                    Type = 1,
                    ImageCount = 1
                };

                Marshal.StructureToPtr(dir, iconPtr, false);

                int adjHeight = bih.Height;
                if (bih.Height == bih.Width * 2)
                    adjHeight = bih.Width;

                var dirEntry = new ICONDIRENTRY()
                {
                    Width = bih.Width > 255 ? (byte)0 : (byte)bih.Width,
                    Height = adjHeight > 255 ? (byte)0 : (byte)adjHeight,
                    Colors = (byte)bih.ColorUsed,
                    ValueA = (byte)bih.Planes,
                    ValueB = (short)bih.BitCount,
                    Size = bmpDataSize,
                    Offset = 22,
                };

                Marshal.StructureToPtr(dirEntry, iconPtr + ICONDIR.SIZE, false);
                Kernel32.CopyMemory(iconPtr + ICONDIR.SIZE + ICONDIRENTRY.SIZE, dibPtr, (uint)bmpDataSize);
                byte[] buffer = new byte[totalSize];
                Marshal.Copy(iconPtr, buffer, 0, totalSize);

                using (var ms = new MemoryStream(buffer))
                    return new Icon(ms);
            }
            finally
            {
                Marshal.FreeHGlobal(iconPtr);
            }
        }

        public static Icon CursorFromUnmanagedResource(IntPtr dibPtr, uint dataSize, bool asIcon = true)
        {

            var bih = Marshal.PtrToStructure<BITMAPINFOHEADER>(dibPtr + 4);

            int bmpDataSize = (int)dataSize - 4;
            int totalSize = ICONDIR.SIZE + ICONDIRENTRY.SIZE + bmpDataSize;

            IntPtr iconPtr = Marshal.AllocHGlobal(totalSize);
            try
            {
                var dir = new ICONDIR()
                {
                    Type = (short)(asIcon ? 1 : 2),
                    ImageCount = 1
                };

                int adjHeight = bih.Height;
                if (bih.Height == bih.Width * 2)
                    adjHeight = bih.Width;

                Marshal.StructureToPtr(dir, iconPtr, false);

                var dirEntry = new ICONDIRENTRY()
                {
                    Width = bih.Width > 255 ? (byte)0 : (byte)bih.Width,
                    Height = adjHeight > 255 ? (byte)0 : (byte)adjHeight,
                    Colors = (byte)bih.ColorUsed,
                    ValueA = asIcon ? (short)bih.Planes : Marshal.ReadInt16(dibPtr),
                    ValueB = asIcon ? (short)bih.BitCount : Marshal.ReadInt16(dibPtr, 2),
                    Size = bmpDataSize,
                    Offset = 22
                };

                Marshal.StructureToPtr(dirEntry, iconPtr + ICONDIR.SIZE, false);
                Kernel32.CopyMemory(iconPtr + ICONDIR.SIZE + ICONDIRENTRY.SIZE, dibPtr + 4, (uint)bmpDataSize);

                byte[] buffer = new byte[totalSize];
                Marshal.Copy(iconPtr, buffer, 0, totalSize);

                using (var ms = new MemoryStream(buffer))
                    return new Icon(ms);
            }
            finally
            {
                Marshal.FreeHGlobal(iconPtr);
            }
        }

        public static PixelFormat GetPixelFormat(BITMAPINFOHEADER bih)
        {
            if (bih.Compression == Native.Enums.BitmapCompressionMode.BI_PNG)
                return PixelFormat.Format32bppArgb;
            switch (bih.BitCount)
            {
                case 1:
                    return PixelFormat.Format1bppIndexed;
                case 4:
                    return PixelFormat.Format4bppIndexed;
                case 8:
                    return PixelFormat.Format8bppIndexed;
                case 16:
                    return PixelFormat.Format16bppArgb1555;
                case 24:
                    return PixelFormat.Format24bppRgb;
                case 32:
                    return PixelFormat.Format32bppArgb;
            }
            return PixelFormat.Undefined;
        }
    }
}
