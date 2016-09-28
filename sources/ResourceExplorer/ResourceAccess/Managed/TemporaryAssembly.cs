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
        private CultureInfo[] _Cultures;

        /// <summary>
        /// this should be referenced only by objects residing in the same (temp) AppDomain as this object
        /// </summary>
        public Assembly Library
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

        public CultureInfo[] Cultures
        {
            get { return _Cultures; }
        }

        internal void Initialize(string assemblyLocation)
        {
            _Library = Assembly.LoadFrom(assemblyLocation);
            _Cultures = new CultureInfo[1] { CultureInfo.InvariantCulture };
        }

        public void LoadSatelliteAssemblies()
        {
            var assemblyDir = Path.GetDirectoryName(Library.Location);
            var assemblyName = Library.GetName().Name;
            var resourcesDlls = Directory.GetFiles(assemblyDir, assemblyName + ".resources.dll", SearchOption.AllDirectories);

            if (resourcesDlls.Length > 0)
            {
                var cultureList = new List<CultureInfo>();
                cultureList.Add(CultureInfo.InvariantCulture);
                for (int i = 0; i < resourcesDlls.Length; i++)
                {
                    //Assembly.LoadFrom(resourcesDlls[i]);
                    try
                    {
                        var dirInfo = new DirectoryInfo(Path.GetDirectoryName(resourcesDlls[i]));
                        var culture = CultureInfo.GetCultureInfo(dirInfo.Name);
                        if (culture != null)
                            cultureList.Add(culture);
                    }
                    catch { }
                }
                _Cultures = cultureList.ToArray();
            }
        }

        public string[] GetManifestResourceNames()
        {
            return Library.GetManifestResourceNames();
        }

        public Stream GetManifestResourceStream(string resourceName)
        {
            var inDomainStream = Library.GetManifestResourceStream(resourceName);
            if (inDomainStream == null)
                return null;
            //transfert the stream into a memory stream so we can pass it accross app domains
            return StreamUtils.ToMemoryStream(inDomainStream, true);
        }

        public ResourceManagerProxy GetResourceManager(string name)
        {
            return TemporaryAppDomain.CreateRefObject<ResourceManagerProxy>(AppDomain.CurrentDomain, name, this);
        }
    }
}
