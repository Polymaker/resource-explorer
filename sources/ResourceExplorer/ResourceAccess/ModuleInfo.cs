using ResourceExplorer.Native;
using ResourceExplorer.Native.API;
using ResourceExplorer.Native.PE;
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
        private bool _Is64Bit;
        private readonly string _Location;
        private readonly FileVersionInfo _VersionInfo;
        private List<ResourceInfo> _Resources;
        private List<SatelliteAssemblyInfo> _SatelliteAssemblies;
        private List<ModuleRef> _ReferencedModules;

        #region Properties

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

        public bool Is64Bit
        {
            get { return _Is64Bit; }
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

        public IList<ModuleRef> ReferencedModules
        {
            get { return _ReferencedModules.AsReadOnly(); }
        }

        public IList<ResourceInfo> Resources
        {
            get { return _Resources.AsReadOnly(); }
        }

        public IList<CultureInfo> Cultures
        {
            get
            {
                return SatelliteAssemblies.Select(s => s.Culture).Distinct().ToList();
            }
        }

        #endregion

        public ModuleInfo(string location)
        {
            _Location = location;
            if (!PEHelper.VerifyPEModule(location, out _IsManaged, out _Is64Bit))
                throw new BadImageFormatException("");

            _VersionInfo = FileVersionInfo.GetVersionInfo(location);
            _FileName = Path.GetFileName(location);
            _Name = Path.GetFileNameWithoutExtension(FileName);
            _ResourcesLoaded = false;
            _Resources = new List<ResourceInfo>();
            _SatelliteAssemblies = new List<SatelliteAssemblyInfo>();
            _ReferencedModules = new List<ModuleRef>();
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

        public ResourceAccessor GetAccessor()
        {
            return new ResourceAccessor(this);
        }

        #endregion

        #region Referenced modules searching

        public void FindSatelliteAssemblies()
        {
            if (!IsManaged)
                return;

            _SatelliteAssemblies.Clear();

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

        public void FindReferencedModules()
        {
            _ReferencedModules.Clear();
            FindNativeReferences();
            if (IsManaged)
                FindManagedReferences();
        }

        private void FindNativeReferences()
        {
            using (Stream fileStream = new FileStream(Location, FileMode.Open, FileAccess.Read))
            {
                var importTableInfo = PEHelper.GetImageDirectories(fileStream)[1];
                var sections = PEHelper.GetImageSections(fileStream);
                var importSection = sections.FirstOrDefault(s =>
                s.VirtualAddress <= importTableInfo.VirtualAddress &&
                s.VirtualAddress + s.SizeOfRawData > importTableInfo.VirtualAddress);

                if (importSection.SizeOfRawData > 0)
                {
                    var dirOffset = importSection.PointerToRawData + (importTableInfo.VirtualAddress - importSection.VirtualAddress);
                    fileStream.Seek(dirOffset, SeekOrigin.Begin);
                    var binaryReader = new BinaryReader(fileStream);

                    IMAGE_IMPORT_DIRECTORY moduleImportEntry;
                    do
                    {
                        moduleImportEntry = fileStream.ReadStructure<IMAGE_IMPORT_DIRECTORY>();
                        if (moduleImportEntry.ModuleName == 0)
                            break;

                        var currentStreamPos = fileStream.Position;
                        var dirNameOffset = importSection.PointerToRawData + (moduleImportEntry.ModuleName - importSection.VirtualAddress);
                        fileStream.Seek(dirNameOffset, SeekOrigin.Begin);

                        var moduleName = binaryReader.ReadNullTerminatedString();
                        _ReferencedModules.Add(new ModuleRef(ModuleType.Native, moduleName));

                        fileStream.Position = currentStreamPos;
                    }
                    while (true);
                }
               
            }
        }

        private void FindManagedReferences()
        {
            using (var tempAppDom = new TemporaryAppDomain(FileName))
            {
                var tmpAssembly = tempAppDom.LoadFrom(Location);
                foreach (var assemName in tmpAssembly.GetReferencedAssemblies())
                {
                    _ReferencedModules.Add(new ModuleRef(ModuleType.Managed, assemName.Name));
                }
            }
        }

        #endregion

        public static ModuleInfo LoadReference(ModuleRef moduleRef)
        {

            return null;
        }
    }
}
