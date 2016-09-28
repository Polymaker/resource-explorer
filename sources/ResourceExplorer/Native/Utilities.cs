using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ResourceExplorer.Native.API;
using ResourceExplorer.Utilities;

namespace ResourceExplorer.Native
{
    public static class Utilities
    {
        public static T[] MarshalDynamicArray<T>(IntPtr objectPtr, int offset, int itemCount)
        {
            return MarshalDynamicArray<T>(objectPtr, offset, itemCount, Marshal.SizeOf(typeof(T)));
        }

        public static T[] MarshalDynamicArray<T>(IntPtr objectPtr, int offset, int itemCount, int objSize)
        {
            var itemList = new T[itemCount];
            long arraylen = objSize * itemCount;
            byte[] arrayData = new byte[arraylen];
            Marshal.Copy(new IntPtr((long)objectPtr + offset), arrayData, 0, (int)arraylen);

            for (int i = 0; i < itemCount; i++)
            {
                IntPtr arrayItemPTr = Marshal.AllocHGlobal(objSize);
                Marshal.Copy(arrayData, i * objSize, arrayItemPTr, objSize);
                var arrayItem = (T)Marshal.PtrToStructure(arrayItemPTr, typeof(T));
                itemList[i] = arrayItem;
                Marshal.FreeHGlobal(arrayItemPTr);
            }
            return itemList;
        }

        #region NT path conversion

        private static DateTime DeviceTimestamp = DateTime.MinValue;
        private static Dictionary<string, string> NtPathToLogicalDrive = new Dictionary<string, string>();

        public static string ConvertNtPathToWin32(string nativePath)
        {
            //TODO: implement update mechanism based on hardware changes
            if ((DateTime.Now - DeviceTimestamp).Minutes > 1)//requery every minutes (to detect new/removed devices eg: USB key)
            {
                NtPathToLogicalDrive.Clear();
                foreach (string strDrivePath in Environment.GetLogicalDrives())
                {
                    string driveLetter = strDrivePath.Substring(0, 2);//e.g. C: (without '\')
                    StringBuilder sbTargetPath = new StringBuilder(255);
                    var result = Kernel32.QueryDosDevice(driveLetter, sbTargetPath, 255);
                    if (result > 0)
                    {
                        string win32Path = sbTargetPath.ToString();
                        if (win32Path.IndexOf('\0') > 0)
                        {
                            string[] paths = win32Path.Split('\0');
                            for (int i = 0; i < paths.Length; i++)
                                NtPathToLogicalDrive.Add(paths[i], driveLetter);
                        }
                        else
                            NtPathToLogicalDrive.Add(win32Path, driveLetter);
                    }
                }
                DeviceTimestamp = DateTime.Now;
            }

            foreach (var ntLogicalDrive in NtPathToLogicalDrive.Keys.OrderByDescending(k => k.Length))
            {
                if (nativePath.StartsWith(ntLogicalDrive))
                {
                    return nativePath.Replace(ntLogicalDrive, NtPathToLogicalDrive[ntLogicalDrive]);
                }
            }

            if (nativePath.StartsWith(@"\Device\Mup\"))
            {
                string networkPath = nativePath.RemoveFirst(@"\Device\Mup");
                if (networkPath.Count(c => c == '\\') > 3)
                {
                    string serverMapping = networkPath.Substring(0, networkPath.IndexOfOccurrence('\\', 3));
                    if (NtPathToLogicalDrive.Keys.Any(p => p.StartsWith(@"\Device\LanmanRedirector\") && p.EndsWith(serverMapping)))
                    {
                        var match = NtPathToLogicalDrive.First(kv => kv.Key.StartsWith(@"\Device\LanmanRedirector\") && kv.Key.EndsWith(serverMapping));
                        return networkPath.Replace(serverMapping, match.Value);
                    }
                }
                return "\\" + networkPath;//add '\'  eg: \REMOTEPC\Public\MyFile.txt -> \\REMOTEPC\Public\MyFile.txt
            }
            return nativePath;
        }

        public static string ConvertNtKeyToRegistryKey(string nativeKey)
        {
            string regKey = nativeKey.Remove(0, 10); // removes "\REGISTRY\"
            string hiveStr = regKey.IndexOf('\\') > 0 ? regKey.Substring(0, regKey.IndexOf('\\')) : regKey;
            regKey = regKey.Substring(hiveStr.Length);

            switch (hiveStr)
            {
                case "MACHINE":
                    return "HKEY_LOCAL_MACHINE" + regKey;
                case "USER":
                    if (UserAccount.CurrentUser != null && regKey.StartsWith("\\" + UserAccount.CurrentUser.SID, StringComparison.OrdinalIgnoreCase))
                    {
                        regKey = regKey.Substring(1 + UserAccount.CurrentUser.SID.Length);
                        if (regKey.StartsWith("_CLASSES", StringComparison.OrdinalIgnoreCase))
                        {
                            regKey = regKey.RemoveFirst("_CLASSES", StringComparison.OrdinalIgnoreCase);
                            return "HKEY_CURRENT_USER\\Software\\Classes" + regKey;
                        }
                        return "HKEY_CURRENT_USER" + regKey;
                    }
                    return "HKEY_USERS" + regKey;
                default:
                    return string.Format("HKEY_{0}{1}", hiveStr, regKey);
            }
        }

        #endregion
    }
}
