using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public class ResourceManagerInfo : ManagedResourceInfo
    {
        private ResourceManagerType _DesignerType;
        private string _ShortName;

        public ResourceManagerType DesignerType
        {
            get { return _DesignerType; }
        }

        public IEnumerable<ManagedResourceInfo> Entries
        {
            get { return Module.ManagedResources.Where(r => r.ResourceManagerName == Name); }
        }

        public string ShortName { get { return _ShortName; } }

        public ResourceManagerInfo(ModuleInfo module, string name, ResourceManagerType designerType) 
            : base(module, ManagedResourceType.ResourceManager, name)
        {
            _DesignerType = designerType;
            
            if (Name.StartsWith(Module.DefaultNamespace + "."))
                _ShortName = Name.Substring(Name.IndexOf('.') + 1);
            else
                _ShortName = Name;
            if (_ShortName.EndsWith(".resources"))
                _ShortName = _ShortName.Substring(0, _ShortName.Length - 10);
        }
    }
}
