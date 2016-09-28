using System;
using System.Collections.Generic;
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
    }
}
