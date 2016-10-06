using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ResourceExplorer.Utilities
{
    public static class StreamUtils
    {
        public static MemoryStream ToMemoryStream(Stream source, bool disposeSource = false)
        {
            var ms = new MemoryStream();
            source.CopyTo(ms);
            if (disposeSource)
                source.Dispose();
            if (ms.Position != 0)
                ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public static void PrintStream(Stream stream, long maxLen)
        {
            var origPos = stream.Position;
            //stream.Position = 0;
            long totalRead = 0;
            byte[] buffer = new byte[32];
            int byteRead = 0;

            do
            {
                //Trace.Write(totalRead.ToString().PadRight(24));
                //Trace.Write((totalRead + 8).ToString().PadRight(24));
                //Trace.Write((totalRead + 16).ToString().PadRight(24));
                //Trace.Write((totalRead + 24).ToString().PadRight(24) + Environment.NewLine);
                byteRead = stream.Read(buffer, 0, buffer.Length);

                for (int i = 0; i < byteRead; i++)
                    Trace.Write(buffer[i].ToString("X2").PadRight(3));
                Trace.Write(Environment.NewLine);
                
                totalRead += byteRead;
                if (maxLen > 0 && totalRead > maxLen)
                    break;
            }
            while (byteRead >= buffer.Length);
            stream.Position = origPos;
        }
    }
}
