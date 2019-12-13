using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ResourceExplorer.Native.Types
{
    public class ResourceName : IDisposable
    {
        public uint ID { get; private set; }
        public IntPtr Handle { get; private set; }
        public string Name { get; private set; }

        public bool IsNamedResource { get; private set; }

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceName"/> class.
        /// </summary>
        /// <param name="lpName"></param>
        public ResourceName(IntPtr lpName)
        {
            ID = (uint)lpName.ToInt64();
            if ((ID >> 16) == 0)  //Integer resource
            {
                Name = "#" + ID;
                Handle = lpName;
            }
            else
            {
                Name = Marshal.PtrToStringAnsi(lpName);
                Handle = Marshal.StringToHGlobalAuto(Name);
                IsNamedResource = true;
            }
        }

        ~ResourceName()
        {
            if (!IsDisposed)
                Free();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Free();
                GC.SuppressFinalize(this);
                IsDisposed = true;
            }
        }

        private void Free()
        {
            if (Handle != IntPtr.Zero)
            {
                try { Marshal.FreeHGlobal(Handle); }
                catch { }
                Handle = IntPtr.Zero;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
