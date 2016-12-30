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
using System.Reflection;
using System.Text;

namespace ResourceExplorer.ResourceAccess
{
    public class ModuleInfo : IDisposable
    {
        private bool _ResourcesLoaded;
        private readonly string _FileName;
        private readonly string _Name;
        private readonly bool _IsManaged;
        private readonly ProcessorArchitecture _Architecture;
        private bool isPE64;
        private readonly string _Location;
        private readonly FileVersionInfo _VersionInfo;
        private List<ResourceInfo> _Resources;
        private List<SatelliteAssemblyInfo> _SatelliteAssemblies;
        private List<ModuleRef> _ReferencedModules;
        private string _DefaultNamespace;

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
            get
            {
                if (Architecture == ProcessorArchitecture.X86 || 
                    Architecture == ProcessorArchitecture.None)//IDK when this could happen
                    return false;
                if (Architecture == ProcessorArchitecture.MSIL)
                    return ResourceExplorer.Native.Utilities.Is64BitOperatingSystem;

                return true;
            }
        }

        public ProcessorArchitecture Architecture
        {
            get { return _Architecture; }
        }

        public bool IsManaged
        {
            get { return _IsManaged; }
        }

        public string DefaultNamespace
        {
            get { return _DefaultNamespace; }
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

        public IEnumerable<ManagedResourceInfo> ManagedResources
        {
            get { return Resources.OfType<ManagedResourceInfo>(); }
        }

        public IEnumerable<NativeResourceInfo> NativeResources
        {
            get { return Resources.OfType<NativeResourceInfo>(); }
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

            if (!PEHelper.VerifyPEModule(location, out _IsManaged, out isPE64))
                throw new BadImageFormatException("Specified file is not a valid assembly.");

            _FileName = Path.GetFileName(location);
            _VersionInfo = FileVersionInfo.GetVersionInfo(location);

            _Name = Path.GetFileNameWithoutExtension(FileName);
            _DefaultNamespace = string.Empty;

            if (IsManaged)
            {
                var assemName = AssemblyName.GetAssemblyName(location);
                _Architecture = assemName.ProcessorArchitecture;
                _Name = assemName.Name;
            }
            else
            {
                _Architecture = isPE64 ? ProcessorArchitecture.Amd64 : ProcessorArchitecture.X86;
            }
            
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
                
                if (tmpAssembly == null)
                    return;

                _DefaultNamespace = tmpAssembly.FindDefaultNamespace();
                var resourceNames = tmpAssembly.GetManifestResourceNames();
                foreach (var resName in resourceNames)
                {
                    if (resName.EndsWith(".resources"))
                    {
                        using (var resManager = tmpAssembly.GetResourceManager(resName))
                        {
                            _Resources.Add(new ResourceManagerInfo(this, resName, tmpAssembly.GetResourceManagerType(resName)));

                            foreach (var resourceEntry in resManager.Resources)
                            {
                                _Resources.Add(new ManagedResourceInfo(this, ManagedResourceType.ResourceEntry, resourceEntry.Key, resourceEntry.Value, resName));
                            }
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

            foreach (var resourceDllFile in moduleDirectory.SafeEnumerateFiles(Name + ".resources.dll", 1))
            {
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
            using (Stream fileStream = new FileStream(Location, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                        _ReferencedModules.Add(new ModuleRef(ModuleType.Native, moduleName)
                        {
                            Location = FindModuleLocation(Path.GetDirectoryName(Location), moduleName)
                        });

                        fileStream.Position = currentStreamPos;
                    }
                    while (true);
                }
               
            }
        }

        private void FindManagedReferences()
        {
            var moduleDirectory = Path.GetDirectoryName(Location);
            
            using (var tempAppDom = new TemporaryAppDomain(FileName))
            {
                var tmpAssembly = tempAppDom.LoadFrom(Location);
                if (tmpAssembly == null)
                    return;

                foreach (var assemName in tmpAssembly.GetReferencedAssemblies())
                {
                    _ReferencedModules.Add(new ModuleRef(assemName)
                    {
                        Location = FindModuleLocation(moduleDirectory, assemName.Name)
                    });
                }
            }
        }

        private string FindModuleLocation(string lookupDirectory, string moduleName)
        {
            var matchingFiles = Directory.EnumerateFiles(lookupDirectory, moduleName + ".*");
            if (matchingFiles.Any())
                return matchingFiles.First();
            return null;
        }

        #endregion

        public static ModuleInfo LoadReference(ModuleRef moduleRef)
        {
            if (!string.IsNullOrEmpty(moduleRef.Location))
                return new ModuleInfo(moduleRef.Location);

            if (moduleRef.Type == ModuleType.Native)
            {

            }
            else
            {
                using (var tempAppDom = new TemporaryAppDomain(moduleRef.ModuleName))
                {
                    var tmpAssembly = tempAppDom.Load(moduleRef.FullName);
                    if (tmpAssembly != null)
                        return new ModuleInfo(tmpAssembly.Location);
                }
            }
            return null;
        }
        //TODO: combine IsValid and CanOpen to return detailed information (eg: reason why it cannot be opened)
        public static bool IsValid(string executableLocation)
        {
            bool is64Bit, isManaged;
            return PEHelper.VerifyPEModule(executableLocation, out isManaged, out is64Bit);
        }
        //TODO: combine IsValid and CanOpen to return detailed information (eg: reason why it cannot be opened)
        public static bool CanOpen(string executableLocation)
        {
            bool is64Bit, isManaged;

            if (!PEHelper.VerifyPEModule(executableLocation, out isManaged, out is64Bit))
                return false;

            if (is64Bit && !ResourceExplorer.Native.Utilities.Is64BitOperatingSystem)
                return false;

            if (isManaged)
            {
                //From my experience, is64Bit will only be set on managed assemblies when they're built explicitly for 64-bit.
                //So to check wheter or not we can load a managed assembly, we must check the .Net ProcessorArchitecture.
                //An assembly built for Any CPU (MSIL) can be loaded in a process built for any architecture (x86/x64/MSIL).
                //The only exception is that a 32-bit process cannot load an assembly explicitly built for 64-bit and vice-versa.

                var targetAssemName = AssemblyName.GetAssemblyName(executableLocation);
                if (targetAssemName == null || targetAssemName.ProcessorArchitecture == ProcessorArchitecture.None)
                    return false;

                var currentArchitecture = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
                var targetArchitecture = targetAssemName.ProcessorArchitecture;

                if (targetArchitecture == ProcessorArchitecture.X86)
                    return currentArchitecture == ProcessorArchitecture.X86;
                else //(MSIL || Amd64 || IA64 (I don't know what this is))
                    return targetArchitecture != ProcessorArchitecture.X86;
            }

            return true;//a 32-bit process can read an unmanaged 64-bit executable
        }

        public void Dispose()
        {
            _Resources.Clear();
            _SatelliteAssemblies.Clear();
            _ReferencedModules.Clear();
            GC.SuppressFinalize(this);
        }

        ~ModuleInfo()
        {
            Dispose();
        }
    }
}
