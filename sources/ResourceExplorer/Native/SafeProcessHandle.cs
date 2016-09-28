using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Security;
using ResourceExplorer.Native.API;

namespace ResourceExplorer.Native
{
    [SuppressUnmanagedCodeSecurity]
    [SecurityCritical]
    public class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static SafeProcessHandle InvalidHandle = new SafeProcessHandle(IntPtr.Zero);

        internal SafeProcessHandle()
            : base(true)
        {
        }

        internal SafeProcessHandle(IntPtr handle)
            : base(true)
        {
            base.SetHandle(handle);
        }

        //[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //internal static extern SafeProcessHandle OpenProcess(int access, bool inherit, int processId);

        internal void InitialSetHandle(IntPtr h)
        {
            this.handle = h;
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return Kernel32.CloseHandle(this.handle);
        }
    }
}
