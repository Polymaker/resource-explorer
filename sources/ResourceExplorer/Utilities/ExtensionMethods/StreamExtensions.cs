using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static class StreamExtensions
    {
        public static T ReadStructure<T>(this Stream stream) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] buffer = new byte[size];
            stream.Read(buffer, 0, size);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            object ret = Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);

            return (T)ret;
        }

        public static T ReadStructure<T>(this BinaryReader stream) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] buffer = new byte[size];
            stream.Read(buffer, 0, size);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            object ret = Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);

            return (T)ret;
        }

        public static string ReadNullTerminatedString(this BinaryReader br)
        {
            string str = "";
            char ch;
            while ((int)(ch = br.ReadChar()) != 0)
                str = str + ch;
            return str;
        }
    }
}
