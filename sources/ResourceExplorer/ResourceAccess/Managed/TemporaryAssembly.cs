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
        private Assembly _Library;

        /// <summary>
        /// this should be referenced only by objects residing in the same (temp) AppDomain as this object
        /// </summary>
        protected internal Assembly Library
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

        internal void Initialize(string assemblyLocation)
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
            return TemporaryAppDomain.CreateRefObject<ResourceManagerProxy>(AppDomain.CurrentDomain, name, this);
        }
    }
}
