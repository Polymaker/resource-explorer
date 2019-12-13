using ResourceExplorer.Native.API;
using ResourceExplorer.ResourceAccess.Managed;
using ResourceExplorer.ResourceAccess.Native;
using ResourceExplorer.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        #region Native

        public IntPtr GetResourcePointer(NativeResourceInfo nativeResource)
        {
            var resHandle = nativeResource.GetHandle(ModuleHandle);
            return Kernel32.GetResourceDataPointer(resHandle, ModuleHandle);
        }

        public IntPtr GetResourcePointer(NativeResourceInfo nativeResource, out uint dataSize)
        {
            var resHandle = nativeResource.GetHandle(ModuleHandle);
            Kernel32.GetResourceDataPointer(resHandle, ModuleHandle, out IntPtr resPtr, out dataSize);
            return resPtr;
        }

        public bool GetResourcePointer(NativeResourceInfo nativeResource, out IntPtr resPtr, out uint dataSize)
        {
            var resHandle = nativeResource.GetHandle(ModuleHandle);
            return Kernel32.GetResourceDataPointer(resHandle, ModuleHandle, out resPtr, out dataSize);
        }

        #endregion

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

            if (resource is NativeResourceInfo nativeResource)
            {
                var resourceType = nativeResource.ResourceType;

                if (resourceType.IsKnownType && resourceType.KnownType == KnownResourceType.Bitmap)
                {
                    if (GetResourcePointer(nativeResource, out IntPtr dataPtr, out _))
                    {
                        var img = net_bmp.BitmapImage.Read(dataPtr);
                        return img.GetSystemBitmap();
                    }

                    if (nativeResource.IsNamedResource)
                        return User32.GetResourceBitmap(ModuleHandle, nativeResource.Name);
                    else
                        return User32.GetResourceBitmap(ModuleHandle, nativeResource.Id);
                }
                else
                {
                    var stream = GetStream(resource);
                    if (stream != null)
                        return Image.FromStream(stream);
                }
            }
            else if (resource is ManagedResourceInfo managedResource)
            {
                //if (!typeof(Image).IsAssignableFrom(managedResource.SystemType))
                //    return null;

                if (managedResource.IsResourceEntry && 
                    typeof(Image).IsAssignableFrom(managedResource.SystemType))
                {
                    var resourceManager = GetResourceManager(managedResource);
                    var proxyImage = resourceManager.GetObject(managedResource.Name) as Image;
                    return TempAppDomain.ReleaseObject(proxyImage);
                }

                if (typeof(Stream).IsAssignableFrom(managedResource.SystemType))
                {
                    var imageStream = GetStream(resource);
                    if (imageStream != null)
                    {
                        imageStream.Position = 0;
                        var header = net_bmp.BitmapHeader.Read(imageStream, true);
                        if (header != null)
                        {
                            imageStream.Position = 0;
                            var bmp = net_bmp.BitmapImage.Read(imageStream);
                            return bmp?.GetSystemBitmap();
                        }
                        return Image.FromStream(imageStream);
                    }
                    //return TempAppDomain.ReleaseObject(proxyStream);
                }
                //Embedded resources are always stored as stream, and we can't know for sure if a stream is an image
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

                if (!(nativeResource.NativeType == KnownResourceType.Icon 
                    || nativeResource.NativeType == KnownResourceType.IconGroup
                    || nativeResource.NativeType == KnownResourceType.Cursor
                    || nativeResource.NativeType == KnownResourceType.CursorGroup))
                    return null;

                if (nativeResource.NativeType == KnownResourceType.Icon)
                {
                    if (GetResourcePointer(nativeResource, out IntPtr dataPtr, out uint dataSize))
                        return ImageHelper.IconFromUnmanagedResource(dataPtr, dataSize);
                }
                else if (nativeResource.NativeType == KnownResourceType.IconGroup)
                {
                    if(nativeResource.IsNamedResource)
                        return User32.GetResourceIconGroup(ModuleHandle, nativeResource.Name);
                    return User32.GetResourceIconGroup(ModuleHandle, nativeResource.Id);
                }
                else if (nativeResource.NativeType == KnownResourceType.Cursor)
                {
                    if (GetResourcePointer(nativeResource, out IntPtr dataPtr, out uint dataSize))
                        return ImageHelper.CursorFromUnmanagedResource(dataPtr, dataSize, true);
                }
                else if(nativeResource.NativeType == KnownResourceType.CursorGroup)
                    return User32.GetResourceCursor(ModuleHandle, nativeResource.Id);
                
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
