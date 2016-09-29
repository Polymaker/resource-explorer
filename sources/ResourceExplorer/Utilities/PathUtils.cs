using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ResourceExplorer.Utilities
{
    public static class PathUtils
    {

        public static bool AreEqual(DirectoryInfo dir1, DirectoryInfo dir2)
        {
            return AreEqual(dir1.FullName, dir2.FullName);
        }

        public static bool AreEqual(string path1, string path2)
        {
            path1 = Path.GetFullPath(path1);
            path2 = Path.GetFullPath(path2);
            return string.Equals(path1, path2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsValidPath(string path)
        {
            var invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }
    }
}
