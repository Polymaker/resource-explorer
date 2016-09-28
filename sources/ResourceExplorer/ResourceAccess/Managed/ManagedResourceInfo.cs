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

        public override bool IsNative
        {
            get { return false; }
        }

        public Type SystemType
        {
            get { return _SystemType; }
            set { _SystemType = value; }
        }

        public ManagedResourceType Kind
        {
            get { return _Kind; }
        }

        public string ResourceManagerName
        {
            get { return _ResourceManagerName; }
        }
        
        public bool IsFromDesigner
        {
            get { return ResourceManagerName.Length > 0; }
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
        }
    }
}