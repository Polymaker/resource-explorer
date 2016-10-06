using ResourceExplorer.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public class TemporaryAssembly : MarshalByRefObject
    {
        private /*readonly*/ Assembly _Library;

        private Assembly Library
        {
            get { return _Library; }
        }

        public string Name
        {
            get { return Library.GetName().Name; }
        }

        public string FullName
        {
            get { return Library.FullName; }
        }

        public string Location
        {
            get { return Library.Location; }
        }

        public TemporaryAssembly()
        {
            _Library = null;
        }

        public TemporaryAssembly(string assemblyLocation)
        {
            _Library = Assembly.LoadFrom(assemblyLocation);
        }

        public bool LoadFrom(string assemblyLocation)
        {
            if (_Library == null)
            {
                try
                {
                    _Library = Assembly.LoadFrom(assemblyLocation);
                }
                catch { }
            }
            return _Library != null;
        }

        public bool Load(string assemblyString)
        {
            if (_Library == null)
            {
                try
                {
                    _Library = Assembly.Load(assemblyString);
                }
                catch { }
            }
            return _Library != null;
        }

        public string[] GetManifestResourceNames()
        {
            return Library.GetManifestResourceNames();
        }

        public Stream GetManifestResourceStream(string resourceName)
        {
            return Library.GetManifestResourceStream(resourceName);
        }

        public AssemblyName[] GetReferencedAssemblies()
        {
            return Library.GetReferencedAssemblies();
        }

        public ResourceManagerProxy GetResourceManager(string name)
        {
            return TemporaryAppDomain.CreateRefObject<ResourceManagerProxy>(AppDomain.CurrentDomain, name, Library);
        }
    }
}
