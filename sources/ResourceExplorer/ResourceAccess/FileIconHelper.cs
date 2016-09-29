using Microsoft.Win32;
using ResourceExplorer.Native.API;
using ResourceExplorer.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public static class FileIconHelper
    {
        public static Icon GetIconForFile(string filepath)
        {
            return GetFileIcon(filepath, Path.GetExtension(filepath));
        }

        public static Icon GetIconForExtension(string extension)
        {
            return GetFileIcon(string.Empty, extension);
        }

        public static Icon GetFileIcon(string filepath, string extension)
        {
            var extensionInfo = GetFileExtensionInfo(extension);
            if (extensionInfo == null)
                return null;

            if (!string.IsNullOrEmpty(extensionInfo.IconLocation) && File.Exists(extensionInfo.IconLocation))
            {
                if (Path.GetExtension(extensionInfo.IconLocation).Equals(".ico", StringComparison.InvariantCultureIgnoreCase) && !extensionInfo.IndexSpecified)
                    return new Icon(extensionInfo.IconLocation);

                var iconPtr = Shell32.ExtractIcon(0, extensionInfo.IconLocation, extensionInfo.IconIndex);
                if (iconPtr != IntPtr.Zero)
                    return Icon.FromHandle(iconPtr);
            }

            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
                return null;

            if (!string.IsNullOrEmpty(extensionInfo.IconHandlerLocation) && File.Exists(extensionInfo.IconHandlerLocation))
            {

            }
            return null;
        }

        private static FileExtensionInfo GetFileExtensionInfo(string extension)
        {
            if (!extension.StartsWith("."))
                extension = "." + extension;

            var rootKey = Registry.ClassesRoot;
            var fileTypeKey = rootKey.OpenSubKey(extension, false);
            if (fileTypeKey == null)
                return null;

            var defaultValue = fileTypeKey.GetValue(string.Empty);
            if (defaultValue == null)
                return null;

            var fileIconKey = rootKey.OpenSubKey(defaultValue.ToString());
            if (fileIconKey == null)
                return null;

            var extInfo = new FileExtensionInfo();

            var defaultIconKey = fileIconKey.OpenSubKey("DefaultIcon");
            if (defaultIconKey != null)
            {
                var iconLocation = defaultIconKey.GetValue(string.Empty).ToString();
                var iconPath = iconLocation.Replace("\"", string.Empty);

                if (iconLocation.IndexOf(',') > 0)
                    iconPath = iconPath.Split(',')[0];

                if (iconPath.Contains("%"))
                    iconPath = Environment.ExpandEnvironmentVariables(iconPath);

                if (!Path.IsPathRooted(iconPath))
                {
                    var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    if (File.Exists(Path.Combine(system32, iconPath)))
                        iconPath = Path.Combine(system32, iconPath);
                }

                extInfo.IconLocation = iconPath;

                if (iconLocation.IndexOf(',') > 0)
                {
                    extInfo.IconIndex = int.Parse(iconLocation.Split(',')[1]);
                    extInfo.IndexSpecified = true;
                }
            }

            var iconHandlerKey = fileIconKey.OpenSubKey("shellex\\IconHandler");
            if (iconHandlerKey != null)
            {
                
                var shellexClsid = iconHandlerKey.GetValue(string.Empty).ToString();
                var clsisKey = rootKey.OpenSubKey(string.Format("CLSID\\{0}\\InprocServer32", shellexClsid));
                if (clsisKey != null)
                {
                    extInfo.IconHandlerCLSID = shellexClsid;
                    extInfo.IconHandlerLocation = clsisKey.GetValue(string.Empty).ToString();
                }
            }

            if (string.IsNullOrEmpty(extInfo.IconLocation) && string.IsNullOrEmpty(extInfo.IconHandlerLocation))
                return null;

            return extInfo;
        }

        class FileExtensionInfo
        {
            public string IconLocation { get; set; }
            public int IconIndex { get; set; }
            public bool IndexSpecified { get; set; }
            public string IconHandlerLocation { get; set; }
            public string IconHandlerCLSID { get; set; }
        }
    }
}
