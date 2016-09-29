using ResourceExplorer.Native.API;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ResourceExplorer.Native
{
    internal static class ComHelper
    {
        private delegate int DllGetClassObject(ref Guid clsid, ref Guid iid, [Out, MarshalAs(UnmanagedType.Interface)] out ResourceExplorer.Native.COM.IClassFactory classFactory);

        internal static object CreateInstance(LibraryModule libraryModule, Guid clsid)
        {
            var classFactory = GetClassFactory(libraryModule, clsid);
            var iid = new Guid("00000000-0000-0000-C000-000000000046"); // IUnknown
            object obj;
            classFactory.CreateInstance(null, ref iid, out obj);
            return obj;
        }

        internal static ResourceExplorer.Native.COM.IClassFactory GetClassFactory(LibraryModule libraryModule, Guid clsid)
        {
            IntPtr ptr = libraryModule.GetProcAddress("DllGetClassObject");
            var callback = (DllGetClassObject)Marshal.GetDelegateForFunctionPointer(ptr, typeof(DllGetClassObject));

            var classFactoryIid = new Guid("00000001-0000-0000-c000-000000000046");
            ResourceExplorer.Native.COM.IClassFactory classFactory;
            var hresult = callback(ref clsid, ref classFactoryIid, out classFactory);

            if (hresult != 0)
            {
                throw new Win32Exception(hresult, "Cannot create class factory");
            }
            return classFactory;
        }
    }

    internal class LibraryModule : IDisposable
    {
        private readonly IntPtr _handle;
        private readonly string _filePath;


        public static LibraryModule LoadModule(string filePath)
        {
            var libraryModule = new LibraryModule(Kernel32.LoadLibrary(filePath), filePath);
            if (libraryModule._handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "Cannot load library: " + filePath);
            }

            return libraryModule;
        }

        private LibraryModule(IntPtr handle, string filePath)
        {
            _filePath = filePath;
            _handle = handle;
        }

        ~LibraryModule()
        {
            if (_handle != IntPtr.Zero)
            {
                Kernel32.FreeLibrary(_handle);
            }
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                Kernel32.FreeLibrary(_handle);
            }
            GC.SuppressFinalize(this);
        }

        public IntPtr GetProcAddress(string name)
        {
            IntPtr ptr = Kernel32.GetProcAddress(_handle, name);
            if (ptr == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                string message = string.Format("Cannot find proc {0} in {1}", name, _filePath);
                throw new Win32Exception(error, message);
            }
            return ptr;
        }

        public string FilePath
        {
            get { return _filePath; }
        }
    }
}
