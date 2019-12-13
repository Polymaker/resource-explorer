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
        public NativeResourceType ResourceType { get; }

        public KnownResourceType NativeType => ResourceType.KnownType;

        public uint Id { get; }

        public bool IsNamedResource { get; }

        public NativeResourceInfo(ModuleInfo module, NativeResourceType type, ResourceExplorer.Native.Types.ResourceName resourceName)
            : base(module, resourceName.Name)
        {
            ResourceType = type;
            Id = resourceName.ID;
            IsNamedResource = resourceName.IsNamedResource;
        }

        public override void DetectContentType()
        {
            if (ResourceType.IsKnownType)
            {
                switch (ResourceType.KnownType)
                {
                    case KnownResourceType.Icon:
                    case KnownResourceType.IconGroup:
                    case KnownResourceType.Cursor:
                    case KnownResourceType.CursorGroup:
                    case KnownResourceType.AnimatedIcon:
                    case KnownResourceType.AnimatedCursor:
                        ContentType = ContentType.Icon;
                        break;
                    case KnownResourceType.Bitmap:
                        ContentType = ContentType.Image;
                        break;
                    case KnownResourceType.String:
                        ContentType = ContentType.Text;
                        break;
                    default:
                        ContentType = ContentType.Unknown;
                        break;
                }
            }
            else
            {
                switch (ResourceType.Name.ToUpper())
                {
                    case "PNG":
                    case "BMP":
                    case "BITMAP":
                    case "IMAGE":
                        ContentType = ContentType.Image;
                        break;
                    default:
                        ContentType = ContentType.Unknown;
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

        public override string ToString()
        {
            return $"{ResourceType} {Name}";
        }
    }
}
