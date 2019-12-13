using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public class ManagedResourceInfo : ResourceInfo
    {
        private Type _SystemType;

        public Type SystemType
        {
            get { return _SystemType; }
            set
            {
                if (_SystemType != value)
                {
                    _SystemType = value;
                    DetectContentType();
                }
            }
        }

        public ManagedResourceType Kind { get; }

        public string ResourceManagerName { get; }

        public ResourceManagerInfo ResourceManager
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceManagerName))
                    return null;
                return Module.Resources.OfType<ResourceManagerInfo>().FirstOrDefault(r => r.Name == ResourceManagerName);
            }
        }

        /// <summary>
        /// AKA a ResourceManager entry
        /// </summary>
        public bool IsResourceEntry
        {
            //get { return ResourceManagerName.Length > 0; }
            get { return Kind == ManagedResourceType.ResourceEntry; }
        }

        public bool IsResourceManager
        {
            get { return SystemType == typeof(System.Resources.ResourceManager); }
        }

        public ManagedResourceInfo(ModuleInfo module, ManagedResourceType kind, string name)
            : this(module, kind, name, typeof(object), string.Empty) { }

        public ManagedResourceInfo(ModuleInfo module, ManagedResourceType kind, string name, Type systemType)
            : this(module, kind, name, systemType, string.Empty) { }

        public ManagedResourceInfo(ModuleInfo module, ManagedResourceType kind, string name, Type systemType, string managerName) : base(module, name)
        {
            Kind = kind;
            _SystemType = systemType;
            ResourceManagerName = managerName;
        }

        public override void DetectContentType()
        {
            if (typeof(System.Drawing.Image).IsAssignableFrom(SystemType))
                ContentType = ContentType.Image;
            else if (typeof(System.Drawing.Icon).IsAssignableFrom(SystemType))
                ContentType = ContentType.Icon;
            else if (Kind == ManagedResourceType.ResourceEntry)
                ContentType = ContentType.Data;
            else// if (Kind == ManagedResourceType.Embedded)
                ContentType = ContentType.Unknown;
        }
    }
}
