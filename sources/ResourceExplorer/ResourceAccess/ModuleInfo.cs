using ResourceExplorer.Native.API;
using ResourceExplorer.ResourceAccess.Managed;
using ResourceExplorer.ResourceAccess.Native;
using ResourceExplorer.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public class ModuleInfo
    {
        private bool _ResourcesLoaded;
        private readonly string _FileName;
        private /*readonly*/ string _Name;
        private readonly bool _IsManaged;
        private readonly string _Location;
        private readonly FileVersionInfo _VersionInfo;
        private List<ResourceInfo> _Resources;
        private List<SatelliteAssemblyInfo> _SatelliteAssemblies;

        public string Name
        {
            get { return _Name; }
        }

        public string FileName
        {
            get { return _FileName; }
        }

        public FileVersionInfo VersionInfo
        {
            get { return _VersionInfo; }
        }

        public string Description
        {
            get { return VersionInfo != null ? VersionInfo.FileDescription : string.Empty; }
        }

        public string Location
        {
            get { return _Location; }
        }

        public bool IsManaged
        {
            get { return _IsManaged; }
        }

        public bool ResourcesLoaded
        {
            get { return _ResourcesLoaded; }
        }

        public IList<SatelliteAssemblyInfo> SatelliteAssemblies
        {
            get { return _SatelliteAssemblies.AsReadOnly(); }
        }

        public IList<ResourceInfo> Resources
        {
            get { return _Resources.AsReadOnly(); }
        }

        public ModuleInfo(string location)
        {
            _Location = location;
            _IsManaged = CheckManagedAssembly(location);
            _VersionInfo = FileVersionInfo.GetVersionInfo(location);
            _FileName = Path.GetFileName(location);
            _Name = Path.GetFileNameWithoutExtension(FileName);
            _ResourcesLoaded = false;
            _Resources = new List<ResourceInfo>();
            _SatelliteAssemblies = new List<SatelliteAssemblyInfo>();
        }

        public void FindSatelliteAssemblies()
        {
            var moduleDirectory = new DirectoryInfo(Path.GetDirectoryName(Location));
            
            foreach (var resourceDllFile in moduleDirectory.EnumerateFiles(Name + ".resources.dll", SearchOption.AllDirectories))
            {
                if (!PathUtils.AreEqual(resourceDllFile.Directory.Parent, moduleDirectory))//only level 1 subdir
                    continue;
                try
                {
                    var culture = CultureInfo.GetCultureInfo(resourceDllFile.Directory.Name);
                    if (culture != null)
                    {
                        _SatelliteAssemblies.Add(new SatelliteAssemblyInfo(resourceDllFile.FullName, culture));
                    }
                }
                catch { }
            }
        }

        #region Resources loading

        public void LoadResources()
        {
            if (ResourcesLoaded)
                return;

            LoadNativeResources();

            if (IsManaged)
                LoadManagedResources();

            _ResourcesLoaded = true;
        }

        private void LoadNativeResources()
        {
            IntPtr moduleHandle = Kernel32.LoadLibraryEx(Location, IntPtr.Zero, Kernel32.LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                var resourceTypes = Kernel32.EnumResourceTypes(moduleHandle);
                foreach (var resType in resourceTypes)
                {
                    var resources = Kernel32.EnumResourceNames(moduleHandle, resType);
                    foreach (var resName in resources)
                    {
                        _Resources.Add(new NativeResourceInfo(this, (NativeResourceType)resType, resName.ID, resName.Name));
                        resName.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception in LoadNativeResources:\r\n" + ex);
            }
            finally
            {
                if (moduleHandle != IntPtr.Zero)
                    Kernel32.FreeLibrary(moduleHandle);
            }
        }

        private void LoadManagedResources()
        {
            using (var tempAppDom = new TemporaryAppDomain(FileName))
            {
                var tmpAssembly = tempAppDom.LoadFrom(Location);
                _Name = tmpAssembly.Name;
                var resourceNames = tmpAssembly.GetManifestResourceNames();
                foreach (var resName in resourceNames)
                {
                    if (resName.EndsWith(".resources"))
                    {
                        var resManager = tmpAssembly.GetResourceManager(resName);
                        _Resources.Add(new ManagedResourceInfo(this, ManagedResourceType.ResourceManager, resName, typeof(System.Resources.ResourceManager)));

                        foreach (var resourceEntry in resManager.Resources)
                        {
                            _Resources.Add(new ManagedResourceInfo(this, ManagedResourceType.Designer, resourceEntry.Key, resourceEntry.Value, resName));
                        }
                    }
                    else
                    {
                        _Resources.Add(new ManagedResourceInfo(this, ManagedResourceType.Embedded, resName, typeof(Stream)));
                    }
                }
            }
        }

        #endregion

        public ResourceAccessor GetAccessor()
        {
            return new ResourceAccessor(this);
        }

        public static bool CheckManagedAssembly(string fileName)
        {
            using (Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if (fileStream.Length < 64)
                        return false;

                    //PE Header starts @ 0x3C (60). Its a 4 byte header.
                    fileStream.Position = 0x3C;
                    uint peHeaderPointer = binaryReader.ReadUInt32();
                    if (peHeaderPointer == 0)
                        peHeaderPointer = 0x80;

                    // Ensure there is at least enough room for the following structures:
                    //     24 byte PE Signature & Header
                    //     28 byte Standard Fields         (24 bytes for PE32+)
                    //     68 byte NT Fields               (88 bytes for PE32+)
                    // >= 128 byte Data Dictionary Table
                    if (peHeaderPointer > fileStream.Length - 256)
                    {
                        return false;
                    }


                    // Check the PE signature.  Should equal 'PE\0\0'.
                    fileStream.Position = peHeaderPointer;
                    uint peHeaderSignature = binaryReader.ReadUInt32();
                    if (peHeaderSignature != 0x00004550)
                    {
                        return false;
                    }

                    // skip over the PEHeader fields
                    fileStream.Position += 20;

                    const ushort PE32 = 0x10b;
                    const ushort PE32Plus = 0x20b;

                    // Read PE magic number from Standard Fields to determine format.
                    var peFormat = binaryReader.ReadUInt16();
                    if (peFormat != PE32 && peFormat != PE32Plus)
                    {
                        return false;
                    }

                    // Read the 15th Data Dictionary RVA field which contains the CLI header RVA.
                    // When this is non-zero then the file contains CLI data otherwise not.
                    ushort dataDictionaryStart = (ushort)(peHeaderPointer + (peFormat == PE32 ? 232 : 248));
                    fileStream.Position = dataDictionaryStart;

                    uint cliHeaderRva = binaryReader.ReadUInt32();
                    if (cliHeaderRva == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
        }
    }
}
