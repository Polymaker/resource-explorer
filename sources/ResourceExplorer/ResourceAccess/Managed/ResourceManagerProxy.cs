using ResourceExplorer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public class ResourceManagerProxy : MarshalByRefObject, IDisposable
    {
        private readonly System.Resources.ResourceManager manager;
        private readonly Dictionary<string, Type> _Resources;

        public Dictionary<string, Type> Resources
        {
            get { return _Resources; }
        }

        public IList<string> ResourceNames
        {
            get { return Resources.Keys.ToList(); }
        }

        public IList<Type> ResourceTypes
        {
            get { return Resources.Values.ToList(); }
        }

        public ResourceManagerProxy(string baseName, Assembly assembly)
        {
            var typeName = baseName.Replace(".resources", string.Empty);
            manager = new System.Resources.ResourceManager(typeName, assembly);

            manager.GetObject(string.Empty);//force load ResourceSet

            var baseResSet = manager.GetResourceSet(CultureInfo.InvariantCulture, false, false);

            _Resources = new Dictionary<string, Type>();

            foreach (DictionaryEntry resEntry in baseResSet)
            {
                _Resources.Add((string)resEntry.Key, resEntry.Value.GetType());
            }
        }

        #region Resources access functions

        public object GetObject(string name)
        {
            return manager.GetObject(name);
        }

        public object GetObject(string name, CultureInfo culture)
        {
            return manager.GetObject(name, culture);
        }

        public string GetString(string name)
        {
            return manager.GetString(name);
        }

        public string GetString(string name, CultureInfo culture)
        {
            return manager.GetString(name, culture);
        }

        public Stream GetStream(string name)
        {
            return manager.GetStream(name);
        }

        public Stream GetStream(string name, CultureInfo culture)
        {
            return manager.GetStream(name, culture);
        }

        #endregion


        ~ResourceManagerProxy()
        {
            Dispose();
        }

        public void Dispose()
        {
            manager.ReleaseAllResources();
            GC.SuppressFinalize(this);
        }
    }
}
