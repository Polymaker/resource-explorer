using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public abstract class ResourceInfo
    {
        private readonly ModuleInfo _Module;
        private readonly string _Name;

        public ModuleInfo Module
        {
            get { return _Module; }
        }

        public string Name
        {
            get { return _Name; }
        }

        public abstract bool IsNative { get; }

        public /*virtual*/ bool IsManaged { get { return !IsNative; } }

        public abstract ContentType ContentType { get; }

        public ResourceInfo(ModuleInfo module, string name)
        {
            _Module = module;
            _Name = name;
        }
    }
}
