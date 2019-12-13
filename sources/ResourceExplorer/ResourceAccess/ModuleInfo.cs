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
        private List<ResourceInfo> _Resources;
        private List<SatelliteAssemblyInfo> _SatelliteAssemblies;
        private List<ModuleRef> _ReferencedModules;
        private List<NativeResourceType> _NativeResourceTypes;

        #region Properties

        public string Name { get; }

        public string FileName { get; }

        public FileVersionInfo VersionInfo { get; }

        public string Description
        {
            get { return VersionInfo != null ? VersionInfo.FileDescription : string.Empty; }
        }

        public string Location { get; }

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

        public ProcessorArchitecture Architecture { get; }

        public bool IsManaged { get; }

        public string DefaultNamespace { get; private set; }

        public bool ResourcesLoaded { get; private set; }

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

        public IList<NativeResourceType> NativeResourceTypes
        {
            get { return _NativeResourceTypes.AsReadOnly(); }
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
            Location = location;
            
            if (!PEHelper.VerifyPEModule(location, out bool isManaged, out bool isPE64))
                throw new BadImageFormatException("Specified file is not a valid assembly.");
            IsManaged = isManaged;
            FileName = Path.GetFileName(location);
            VersionInfo = FileVersionInfo.GetVersionInfo(location);

            Name = Path.GetFileNameWithoutExtension(FileName);
            DefaultNamespace = string.Empty;

            if (IsManaged)
            {
                var assemName = AssemblyName.GetAssemblyName(location);
                Architecture = assemName.ProcessorArchitecture;
                Name = assemName.Name;
            }
            else
            {
                Architecture = isPE64 ? ProcessorArchitecture.Amd64 : ProcessorArchitecture.X86;
            }
            
            ResourcesLoaded = false;
            _Resources = new List<ResourceInfo>();
            _SatelliteAssemblies = new List<SatelliteAssemblyInfo>();
            _ReferencedModules = new List<ModuleRef>();
            _NativeResourceTypes = new List<NativeResourceType>();
        }

        #region Resources loading

        public void LoadResources()
        {
            if (ResourcesLoaded)
                return;

            LoadNativeResources();

            if (IsManaged)
                LoadManagedResources();

            foreach (var resInfo in Resources)
                resInfo.DetectContentType();

            ResourcesLoaded = true;
        }

        private void LoadNativeResources()
        {
            IntPtr moduleHandle = Kernel32.LoadLibraryEx(Location, IntPtr.Zero, Kernel32.LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                var resourceTypes = Kernel32.EnumResourceTypes(moduleHandle);
                _NativeResourceTypes.AddRange(resourceTypes);
                foreach (var resType in resourceTypes)
                {
                    var resources = Kernel32.EnumResourceNames(moduleHandle, resType);
                    foreach (var resName in resources)
                    {
                        _Resources.Add(new NativeResourceInfo(this, resType, resName));
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

                DefaultNamespace = tmpAssembly.FindDefaultNamespace();
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
                var peInfo = PEHeaderInfo.ReadInfo(fileStream);
                var importTableInfo = peInfo.ImageTables[1];
                var importSection = peInfo.GetSectionForTable(importTableInfo);
                string system32Path = peInfo.Is32Bit ? 
                    Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) :
                    Environment.GetFolderPath(Environment.SpecialFolder.System);
                
                if (importSection.SizeOfRawData > 0)
                {
                    var dirOffset = importSection.PointerToRawData + (importTableInfo.VirtualAddress - importSection.VirtualAddress);
                    fileStream.Seek(dirOffset, SeekOrigin.Begin);

                    using (var binaryReader = new BinaryReader(fileStream))
                    {

                        IMAGE_IMPORT_DIRECTORY moduleImportEntry;
                        do
                        {
                            moduleImportEntry = fileStream.ReadStructure<IMAGE_IMPORT_DIRECTORY>();
                            if (moduleImportEntry.ModuleName == 0)
                                break;

                            IMAGE_SECTION_HEADER moduleSection = importSection;
                            if (moduleImportEntry.ModuleName < importSection.VirtualAddress)
                                moduleSection = peInfo.GetSectionForVirtualOffset(moduleImportEntry.ModuleName);

                            var currentStreamPos = fileStream.Position;
                            var dirNameOffset = moduleSection.PointerToRawData + (moduleImportEntry.ModuleName - moduleSection.VirtualAddress);
                            fileStream.Seek(dirNameOffset, SeekOrigin.Begin);

                            var moduleName = binaryReader.ReadNullTerminatedString();
                            string assemblyLocation = FindModuleLocation(Path.GetDirectoryName(Location), moduleName);
                            if (string.IsNullOrEmpty(assemblyLocation))
                            {
                                assemblyLocation = FindModuleLocation(system32Path, moduleName);
                            }
                            _ReferencedModules.Add(new ModuleRef(ModuleType.Native, moduleName, assemblyLocation));

                            fileStream.Position = currentStreamPos;
                        }
                        while (true);
                    }
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
                    string assemLocation = tempAppDom.GetAssemblyLocation(assemName);
                    if (string.IsNullOrEmpty(assemLocation))
                        assemLocation = FindModuleLocation(moduleDirectory, assemName.Name);

                    _ReferencedModules.Add(new ModuleRef(assemName, assemLocation));
                }
            }
        }

        private string FindModuleLocation(string lookupDirectory, string moduleName)
        {
            //TODO: check GAC and system directorires
            var matchingFiles = Directory.EnumerateFiles(lookupDirectory, moduleName + ".*");
            if (matchingFiles.Any(x=> x.ToLower().EndsWith("dll") || x.ToLower().EndsWith("exe")))
                return matchingFiles.First();
            return string.Empty;
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
            return PEHelper.VerifyPEModule(executableLocation, out _, out _);
        }
        //TODO: combine IsValid and CanOpen to return detailed information (eg: reason why it cannot be opened)
        public static bool CanOpen(string executableLocation)
        {
            if (!PEHelper.VerifyPEModule(executableLocation, out bool isManaged, out bool is64Bit))
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
