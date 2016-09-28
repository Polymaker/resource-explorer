using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ResourceExplorer.Native.Enums;
using ResourceExplorer.Native.Types;
using System.Drawing;
using System.Runtime.ExceptionServices;

namespace ResourceExplorer.Native.API
{
    public static class User32
    {
        #region Signatures

        [DllImport("user32.dll")]
        public static extern IntPtr LoadBitmap(IntPtr hInstance, string lpBitmapName);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadBitmap(IntPtr hInstance, uint resourceId);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, string lpIconName);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, uint resourceId);

        #endregion


        #region Utility methods

        [HandleProcessCorruptedStateExceptions]
        public static Bitmap GetResourceBitmap(IntPtr hInstance, uint resourceId)
        {
            try
            {
                IntPtr bmpHandle = User32.LoadBitmap(hInstance, resourceId);
                return Bitmap.FromHbitmap(bmpHandle);
            }
            catch(System.AccessViolationException ex)
            {
                return null;
            }
        }

        public static Icon GetResourceIcon(IntPtr hInstance, uint resourceId)
        {
            IntPtr iconHandle = User32.LoadIcon(hInstance, resourceId);
            return Icon.FromHandle(iconHandle);
        }

        #endregion
    }
}
