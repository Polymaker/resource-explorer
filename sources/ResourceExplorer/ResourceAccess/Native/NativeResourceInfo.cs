using ResourceExplorer.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Native
{
    public class NativeResourceInfo : ResourceInfo
    {
        // Fields...
        private readonly uint _Id;
        private readonly NativeResourceType _Kind;
        private readonly ContentType _ContentType;

        public NativeResourceType Kind
        {
            get { return _Kind; }
        }

        public uint Id
        {
            get { return _Id; }
        }

        public override bool IsNative
        {
            get { return true; }
        }

        public override ContentType ContentType { get { return _ContentType; } }

        public NativeResourceInfo(ModuleInfo module, NativeResourceType kind, uint id, string name)
            : base(module, name)
        {
            _Kind = kind;
            _Id = id;
            switch (kind)
            {
                case NativeResourceType.Icon:
                case NativeResourceType.IconGroup:
                case NativeResourceType.Cursor:
                case NativeResourceType.CursorGroup:
                case NativeResourceType.AnimatedCursor:
                case NativeResourceType.AnimatedIcon:
                    _ContentType = ContentType.Icon;
                    break;
                case NativeResourceType.Bitmap:
                    _ContentType = ContentType.Image;
                    break;
                default:
                    _ContentType = ContentType.Unknown;
                    break;
            }
        }

        internal IntPtr GetHandle(IntPtr moduleHandle)
        {
            IntPtr resourceHandle = Kernel32.FindResource(moduleHandle, Id, (ResourceExplorer.Native.Enums.ResourceType)Kind);
            if (resourceHandle != IntPtr.Zero)
                return resourceHandle;
            resourceHandle = Kernel32.FindResource(moduleHandle, Name, (ResourceExplorer.Native.Enums.ResourceType)Kind);
            return resourceHandle;
        }
    }
}
