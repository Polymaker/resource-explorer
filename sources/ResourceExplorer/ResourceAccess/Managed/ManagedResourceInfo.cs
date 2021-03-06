﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public class ManagedResourceInfo : ResourceInfo
    {
        private readonly string _ResourceManagerName;
        private readonly ManagedResourceType _Kind;
        private Type _SystemType;
        private /*readonly*/ ContentType _ContentType;

        public override bool IsNative
        {
            get { return false; }
        }

        public Type SystemType
        {
            get { return _SystemType; }
            set
            {
                _SystemType = value;
                CheckContentType();
            }
        }

        public ManagedResourceType Kind
        {
            get { return _Kind; }
        }

        public override ContentType ContentType { get { return _ContentType; } }

        public string ResourceManagerName
        {
            get { return _ResourceManagerName; }
        }

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
            : this(module, kind, name, typeof(Object), string.Empty) { }

        public ManagedResourceInfo(ModuleInfo module, ManagedResourceType kind, string name, Type systemType)
            : this(module, kind, name, systemType, string.Empty) { }

        public ManagedResourceInfo(ModuleInfo module, ManagedResourceType kind, string name, Type systemType, string managerName) : base(module, name)
        {
            _Kind = kind;
            _SystemType = systemType;
            _ResourceManagerName = managerName;
            _ContentType = ContentType.Unknown;
            CheckContentType();
        }

        private void CheckContentType()
        {
            if (typeof(System.Drawing.Image).IsAssignableFrom(SystemType))
                _ContentType = ContentType.Image;
            else if (typeof(System.Drawing.Icon).IsAssignableFrom(SystemType))
                _ContentType = ContentType.Icon;
            else if (Kind == ManagedResourceType.ResourceEntry)
                _ContentType = ContentType.Data;
            else// if (Kind == ManagedResourceType.Embedded)
                _ContentType = ContentType.Unknown;
        }
    }
}
