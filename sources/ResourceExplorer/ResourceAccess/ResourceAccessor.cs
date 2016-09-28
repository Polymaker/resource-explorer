using ResourceExplorer.Native.API;
using ResourceExplorer.ResourceAccess.Managed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public class ResourceAccessor : IDisposable
    {
        // Fields...
        private ModuleInfo _Module;
        private IntPtr ModuleHandle;
        private TemporaryAppDomain TempAppDomain;
        private TemporaryAssembly ManagedAssembly;
        private Dictionary<string, ResourceManagerProxy> ResourceManagers;

        public ModuleInfo Module
        {
            get { return _Module; }
        }

        public ResourceAccessor(ModuleInfo module)
        {
            _Module = module;
            _Module = module;
            ModuleHandle = IntPtr.Zero;
            ManagedAssembly = null;
            TempAppDomain = null;
            ResourceManagers = new Dictionary<string, ResourceManagerProxy>();
            Initialize();
        }

        private void Initialize()
        {
            ModuleHandle = Kernel32.LoadLibraryEx(Module.Location, IntPtr.Zero, Kernel32.LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
            if (Module.IsManaged)
            {
                TempAppDomain = new TemporaryAppDomain(Module.Name);
                ManagedAssembly = TempAppDomain.LoadFrom(Module.Location);
            }
        }

        #region Managed resources methods & functions

        private ResourceManagerProxy GetResourceManager(ManagedResourceInfo managedResInfo)
        {
            if (ResourceManagers.ContainsKey(managedResInfo.ResourceManagerName))
                return ResourceManagers[managedResInfo.ResourceManagerName];

            var resManagerRef = ManagedAssembly.GetResourceManager(managedResInfo.ResourceManagerName);
            ResourceManagers.Add(managedResInfo.ResourceManagerName, resManagerRef);
            return resManagerRef;
        }

        #endregion

        ~ResourceAccessor()
        {
            Dispose();
        }

        public void Dispose()
        {
            foreach (var resManager in ResourceManagers.Values)
                resManager.Dispose();

            ResourceManagers.Clear();
            if (TempAppDomain != null)
            {
                TempAppDomain.Dispose();
                TempAppDomain = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
