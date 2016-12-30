using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public string FullName { get; set; }

        public string Location { get; set; }

        public ModuleType Type
        {
            get { return _Type; }
        }

        public ModuleRef(ModuleType type, string moduleName)
        {
            _Type = type;
            _ModuleName = moduleName;
        }

        public ModuleRef(AssemblyName assemName)
        {
            _Type = ModuleType.Managed;
            _ModuleName = assemName.Name;
            FullName = assemName.FullName;
            Location = assemName.CodeBase;
        }

        public bool IsSystemModule()
        {
            if (Location != null)
            {
                if (Location.Contains(Environment.GetFolderPath(Environment.SpecialFolder.System))
                    || Location.Contains(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)))
                    return true;
            }
            return false;
        }
    }
}
