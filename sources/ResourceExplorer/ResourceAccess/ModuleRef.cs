using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public class ModuleRef
    {
        private readonly ModuleType _Type;
        private readonly string _ModuleName;

        public string ModuleName
        {
            get { return _ModuleName; }
        }

        public ModuleType Type
        {
            get { return _Type; }
        }

        public ModuleRef(ModuleType type, string moduleName)
        {
            _Type = type;
            _ModuleName = moduleName;
        }
    }
}
