﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ResourceExplorer.Native.Enums;
using ResourceExplorer.Native.Types;

namespace ResourceExplorer.Native.API
{
    public static class Kernel32
    {
        public static readonly IntPtr INVALID_HANDLE = new IntPtr(-1);

        #region Signatures

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool EnumResourceNames(IntPtr hModule, ResourceType dwID, EnumResourceNamesDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool EnumResourceTypes(IntPtr hModule, EnumResourceTypesDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LockResource(IntPtr hResData);

        #region FindResource

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, ResourceType lpType);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindResource(IntPtr hModule, string lpName, ResourceType lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr FindResource(IntPtr hModule, uint lpName, ResourceType lpType);
        
        #endregion

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint QueryDosDevice(string lpDeviceName, IntPtr lpTargetPath, int ucchMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [DllImport("Kernel32.dll", EntryPoint = "SizeofResource", SetLastError = true)]
        public static extern uint SizeOfResource(IntPtr hModule, IntPtr hResource);

        [DllImport("kernel32.dll")]
        public static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll")]
        public static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        #endregion

        #region Delegates

        public delegate bool EnumResourceNamesDelegate(IntPtr hModule, ResourceType lpszType, IntPtr lpszName, IntPtr lParam);

        public delegate bool EnumResourceTypesDelegate(IntPtr hModule, ResourceType lpszType, IntPtr lParam);

        #endregion

        #region Local-only (generally) Enums 

        [Flags]
        public enum LoadLibraryFlags : uint
        {
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        }

        #endregion

        #region Utility methods

        public static List<ResourceType> EnumResourceTypes(IntPtr moduleHandle)
        {
            var typeList = new List<ResourceType>();
            EnumResourceTypesDelegate enumDelegate = (hModule, lpszType, lParam) =>
            {
                typeList.Add(lpszType);
                return true;
            };
            GC.KeepAlive(enumDelegate);
            EnumResourceTypes(moduleHandle, enumDelegate, IntPtr.Zero);
            typeList = typeList.Distinct().ToList();
            return typeList;
        }

        public static List<ResourceName> EnumResourceNames(IntPtr moduleHandle, ResourceType resourceType)
        {
            var resList = new List<ResourceName>();
            EnumResourceNamesDelegate enumDelegate = (hModule, lpszType, lpszName, lParam) =>
            {
                resList.Add(new ResourceName(lpszName));
                return true;
            };
            GC.KeepAlive(enumDelegate);
            EnumResourceNames(moduleHandle, resourceType, enumDelegate, IntPtr.Zero);

            return resList;
        }

        public static List<MODULEENTRY32> EnumProcessModules(uint processID)
        {
            List<MODULEENTRY32> modules = new List<MODULEENTRY32>();

            IntPtr snapshot = CreateToolhelp32Snapshot(SnapshotFlags.Module | SnapshotFlags.Module32, processID);
            if (snapshot == new IntPtr(-1))
            {
                int lastError = Marshal.GetLastWin32Error();
                return modules;
            }
            MODULEENTRY32 mod = MODULEENTRY32.Instanciate();
            if (!Module32First(snapshot, ref mod))
                return modules;

            do
            {
                modules.Add(mod);
            }
            while (Module32Next(snapshot, ref mod));

            CloseHandle(snapshot);

            return modules;
        }


        public static byte[] GetResourceData(IntPtr hResInfo, IntPtr hModule)
        {
            if (hResInfo == IntPtr.Zero || hModule == IntPtr.Zero)
                return new byte[0];

            //Load the resource.
            IntPtr hResData = LoadResource(hModule, hResInfo);
            //Lock the resource to read data.
            IntPtr hGlobal = LockResource(hResData);
            //Get the resource size.
            long resSize = (long)SizeOfResource(hModule, hResInfo);
            //Allocate the requested size.
            byte[] buffer = new byte[resSize];
            Marshal.Copy(hGlobal, buffer, 0, buffer.Length);
            return buffer;
        }

        //public static IntPtr GetResourceDataHandle(IntPtr hResInfo, IntPtr hModule)
        //{
        //    if (hResInfo == IntPtr.Zero || hModule == IntPtr.Zero)
        //        return IntPtr.Zero;

        //    //Load the resource.
        //    IntPtr hResData = LoadResource(hModule, hResInfo);
        //    //Lock the resource to read data.
        //    IntPtr hGlobal = LockResource(hResData);

        //    return hResData;

        //}

        #endregion
    }
}
