using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public class ModuleRef
    {
        public string ModuleName { get; }

        public string FullName { get; set; }

        public string Location { get; set; }

        public ModuleType Type { get; }

        public bool IsSystem { get; set; }

        public ModuleRef(ModuleType type, string moduleName)
        {
            Type = type;
            ModuleName = moduleName;
        }

        public ModuleRef(AssemblyName assemName, string location = null)
            : this(ModuleType.Managed, assemName.Name, location)
        {
            FullName = assemName.FullName;
            Location = location ?? string.Empty;
        }

        public ModuleRef(ModuleType type, string moduleName, string location)
        {
            Type = type;
            ModuleName = moduleName;
            Location = location ?? string.Empty;
            IsSystem = Location.ToUpper().Contains("WINDOWS");
        }

        //public bool IsSystemModule()
        //{
        //    if (Location != null)
        //    {
        //        if (Location.Contains(Environment.GetFolderPath(Environment.SpecialFolder.System))
        //            || Location.Contains(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)))
        //            return true;
        //    }
        //    return false;
        //}
    }
}
