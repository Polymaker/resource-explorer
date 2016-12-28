using ResourceExplorer.Native.API;
using ResourceExplorer.ResourceAccess.Managed;
using ResourceExplorer.ResourceAccess.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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

        #region Resources methods & functions

        public Stream GetStream(ResourceInfo resource)
        {
            if (resource == null || resource.Module != Module)
                return null;

            if (resource.IsNative)
            {
                var nativeResource = (NativeResourceInfo)resource;
                var resHandle = nativeResource.GetHandle(ModuleHandle);
                var resData = Kernel32.GetResourceData(resHandle, ModuleHandle);
                return new MemoryStream(resData);
            }
            else
            {
                var managedResource = (ManagedResourceInfo)resource;

                if (managedResource.IsResourceManager)
                    return null;

                Stream resourceStream = null;

                if (managedResource.IsResourceEntry)
                {
                    var resourceManager = GetResourceManager(managedResource);
                    if (typeof(Stream).IsAssignableFrom(managedResource.SystemType))
                        resourceStream = resourceManager.GetStream(managedResource.Name);
                }
                else
                {
                    resourceStream = ManagedAssembly.GetManifestResourceStream(managedResource.Name);
                }

                if (resourceStream != null)
                    return TempAppDomain.ReleaseObject(resourceStream);
            }

            return null;
        }

        public Image GetImage(ResourceInfo resource)
        {
            if (resource == null || resource.Module != Module)
                return null;

            if (resource.IsNative)
            {
                var nativeResource = (NativeResourceInfo)resource;

                if (nativeResource.Kind != NativeResourceType.Bitmap)
                    return null;

                return User32.GetResourceBitmap(ModuleHandle, nativeResource.Id);
            }
            else
            {
                var managedResource = (ManagedResourceInfo)resource;

                if (!typeof(Image).IsAssignableFrom(managedResource.SystemType))
                    return null;

                if (managedResource.IsResourceEntry)
                {
                    var resourceManager = GetResourceManager(managedResource);
                    var proxyImage = resourceManager.GetObject(managedResource.Name) as Image;
                    return TempAppDomain.ReleaseObject(proxyImage);
                }

                //Embedded resources are always stored as stream, and we can't know for sure of a stream is an image
            }

            return null;
        }

        public Icon GetIcon(ResourceInfo resource)
        {
            if (resource == null || resource.Module != Module)
                return null;

            if (resource.IsNative)
            {
                var nativeResource = (NativeResourceInfo)resource;

                if (!(nativeResource.Kind == NativeResourceType.Icon || nativeResource.Kind == NativeResourceType.IconGroup))
                    return null;

                return User32.GetResourceIcon(ModuleHandle, nativeResource.Id);
            }
            else
            {
                var managedResource = (ManagedResourceInfo)resource;

                if (!typeof(Icon).IsAssignableFrom(managedResource.SystemType))
                    return null;

                if (managedResource.IsResourceEntry)
                {
                    var resourceManager = GetResourceManager(managedResource);
                    var proxyIcon = resourceManager.GetObject(managedResource.Name) as Icon;
                    return TempAppDomain.ReleaseObject(proxyIcon);
                }

                //Embedded resources are always stored as stream, and we can't know for sure if a stream is an image
            }

            return null;
        }

        public object GetObject(ManagedResourceInfo resource)
        {
            if (!resource.IsResourceEntry)
                return null;

            var resourceManager = GetResourceManager(resource);

            return TempAppDomain.ReleaseObject(resourceManager.GetObject(resource.Name), resource.SystemType);
        }

        public object GetObject(ManagedResourceInfo resource, CultureInfo culture)
        {
            if (!resource.IsResourceEntry)
                return null;

            var resourceManager = GetResourceManager(resource);

            return TempAppDomain.ReleaseObject(resourceManager.GetObject(resource.Name, culture), resource.SystemType);
        }

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

            if (ModuleHandle != IntPtr.Zero)
            {
                Kernel32.FreeLibrary(ModuleHandle);
                ModuleHandle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

    }
}
