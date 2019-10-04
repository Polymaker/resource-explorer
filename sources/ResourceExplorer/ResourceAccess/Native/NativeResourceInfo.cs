using ResourceExplorer.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ResType = ResourceExplorer.Native.Enums.ResourceType;
namespace ResourceExplorer.ResourceAccess.Native
{
    public class NativeResourceInfo : ResourceInfo
    {
        private readonly ContentType _ContentType;

        public NativeResourceType ResourceType { get; }

        public KnownResourceType NativeType => ResourceType.KnownType;

        public uint Id { get; }

        public override bool IsNative
        {
            get { return true; }
        }

        public bool IsNamedResource { get; }

        public override ContentType ContentType { get { return _ContentType; } }

        public NativeResourceInfo(ModuleInfo module, NativeResourceType type, ResourceExplorer.Native.Types.ResourceName resourceName)
            : base(module, resourceName.Name)
        {
            ResourceType = type;
            Id = resourceName.ID;
            IsNamedResource = resourceName.IsNamedResource;

            if (type.IsKnownType)
            {
                switch (type.KnownType)
                {
                    case KnownResourceType.Icon:
                    case KnownResourceType.IconGroup:
                    case KnownResourceType.Cursor:
                    case KnownResourceType.CursorGroup:
                    case KnownResourceType.AnimatedIcon:
                    case KnownResourceType.AnimatedCursor:
                        _ContentType = ContentType.Icon;
                        break;
                    case KnownResourceType.Bitmap:
                        _ContentType = ContentType.Image;
                        break;
                    default:
                        _ContentType = ContentType.Unknown;
                        break;
                }
            }
            else
            {
                switch (type.Name)
                {
                    case "PNG":
                    case "BMP":
                    case "BITMAP":
                    case "IMAGE":
                        _ContentType = ContentType.Image;
                        break;
                    default:
                        _ContentType = ContentType.Unknown;
                        break;
                }
            }
        }

        internal IntPtr GetHandle(IntPtr moduleHandle)
        {
            IntPtr resType = ResourceType.GetLPSZ();
            try
            {
                if (IsNamedResource)
                    return Kernel32.FindResource(moduleHandle, Name, resType);
                else
                    return Kernel32.FindResource(moduleHandle, Id, resType);
            }
            finally
            {
                if (ResourceType.IsCustom)
                    Marshal.FreeHGlobal(resType);
            }
        }
    }
}
