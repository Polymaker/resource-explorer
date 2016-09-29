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
        private readonly Assembly _Library;

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

        public TemporaryAssembly(string assemblyLocation)
        {
            _Library = Assembly.LoadFrom(assemblyLocation);
        }

        public string[] GetManifestResourceNames()
        {
            return Library.GetManifestResourceNames();
        }

        public Stream GetManifestResourceStream(string resourceName)
        {
            return Library.GetManifestResourceStream(resourceName);
        }

        public ResourceManagerProxy GetResourceManager(string name)
        {
            return TemporaryAppDomain.CreateRefObject<ResourceManagerProxy>(AppDomain.CurrentDomain, name, Library);
        }
    }
}
