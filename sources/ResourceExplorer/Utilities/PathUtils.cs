using System;
using System.Collections;
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

        public static IEnumerable<FileInfo> SafeEnumerateFiles(this DirectoryInfo directory, string searchPattern = null, int maxDirectoryDepth = 0)
        {
            return SafeEnumerateFiles(directory, searchPattern ?? string.Empty, maxDirectoryDepth, 0);
        }

        private static IEnumerable<FileInfo> SafeEnumerateFiles(this DirectoryInfo directory, string searchPattern, int maxDirectoryDepth, int curDirectoryDepth)
        {
            if (maxDirectoryDepth == -1 || (maxDirectoryDepth > 0 && curDirectoryDepth < maxDirectoryDepth))
            {
                var dirEnum = directory.EnumerateDirectories().GetEnumerator();
                while (true)
                {
                    try
                    {
                        if (!dirEnum.MoveNext())
                            break;
                    }
                    catch { }
                    if (dirEnum.Current != null)
                    {
                        var fileEnum = SafeEnumerateFiles(dirEnum.Current, searchPattern, maxDirectoryDepth, curDirectoryDepth + 1).GetEnumerator();
                        while (true)
                        {
                            try
                            {
                                if (!fileEnum.MoveNext())
                                    break;
                            }
                            catch { }
                            if (fileEnum.Current != null)
                                yield return fileEnum.Current;
                        }
                    }
                }
            }

            foreach (var foundFile in directory.EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly))
                yield return foundFile;
        }

        public static IEnumerable<FileInfo> SafeEnumerateFiles(this DirectoryInfo directory, string searchPattern, SearchOption searchOption)
        {
            return SafeEnumerateFiles(directory, searchPattern ?? string.Empty, searchOption == SearchOption.AllDirectories ? -1 : 0, 0);
        }
    }
}
