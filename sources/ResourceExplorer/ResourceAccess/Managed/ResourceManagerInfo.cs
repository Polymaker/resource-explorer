using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public class ResourceManagerInfo : ManagedResourceInfo
    {
        private ResourceManagerType _DesignerType;

        public ResourceManagerType DesignerType
        {
            get { return _DesignerType; }
        }

        public ResourceManagerInfo(ModuleInfo module, string name, ResourceManagerType designerType) 
            : base(module, ManagedResourceType.ResourceManager, name)
        {
            _DesignerType = designerType;
        }
    }
}
