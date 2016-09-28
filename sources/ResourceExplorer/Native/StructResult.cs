using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ResourceExplorer.Native.API;

namespace ResourceExplorer.Native
{
    public abstract class StructResult
    {
        public enum DisposeMethod
        {
            FreeHGlobal,
            CloseHandle
        }

        public static StructResult<T> Alloc<T>()
        {
            return new StructResult<T>(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T))), DisposeMethod.FreeHGlobal);
        }
    }

    public class StructResult<T> : StructResult, IDisposable//where T : struct
    {
        // Fields...
        private T _Value;
        private IntPtr _Handle;
        private DisposeMethod method;

        public IntPtr Handle
        {
            get { return _Handle; }
        }

        public T Value
        {
            get { return _Value; }
        }

        public bool Succeeded
        {
            get
            {
                return Handle != IntPtr.Zero && Handle != Kernel32.INVALID_HANDLE;
            }
        }

        public readonly static StructResult<T> Failed = new StructResult<T>();

        public StructResult()
        {
            method = DisposeMethod.FreeHGlobal;
            _Value = default(T);
            _Handle = IntPtr.Zero;
        }

        public StructResult(IntPtr handle, DisposeMethod method = DisposeMethod.FreeHGlobal)
        {
            _Handle = handle;
            this.method = method;
            if (Succeeded)
            {
                _Value = (T)Marshal.PtrToStructure(handle, typeof(T));
            }
            else
            {
                _Value = default(T);
            }
        }

        public static StructResult<T> Alloc()
        {
            return new StructResult<T>(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T))), DisposeMethod.FreeHGlobal);
        }

        ~StructResult()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                if (method == DisposeMethod.FreeHGlobal)
                    Marshal.FreeHGlobal(Handle);
                else if (Handle != Kernel32.INVALID_HANDLE)
                    Kernel32.CloseHandle(Handle);
                _Handle = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }
    }
}
