using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public abstract class ResourceInfo
    {
        public ModuleInfo Module { get; }

        public string Name { get; }

        public bool IsNative => GetType() == typeof(Native.NativeResourceInfo);

        public bool IsManaged { get { return !IsNative; } }

        public ContentType ContentType { get; protected set; }

        public ResourceInfo(ModuleInfo module, string name)
        {
            Module = module;
            Name = name;
            ContentType = ContentType.Unknown;
        }

        public virtual void DetectContentType()
        {

        }

        public override string ToString()
        {
            return Name;
        }
    }
}
