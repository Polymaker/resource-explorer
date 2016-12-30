using ResourceExplorer.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            get { return _Library != null ? Library.GetName().Name : string.Empty; }
        }

        public string FullName
        {
            get { return _Library != null ? Library.FullName : string.Empty; }
        }

        public string Location
        {
            get { return _Library != null ? Library.Location : string.Empty; }
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
                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("Couldn't load assembly '{0}':\r\n\t{1}", assemblyLocation, ex));
                }
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
                catch(Exception ex)
                {
                    Trace.WriteLine(string.Format("Couldn't load assembly '{0}':\r\n\t{1}", assemblyString, ex));
                }
            }
            return _Library != null;
        }

        public string[] GetManifestResourceNames()
        {
            return Library.GetManifestResourceNames();
        }

        public string FindDefaultNamespace()
        {
            var refCount = new Dictionary<string, int>();
            var libTypes = Library.GetTypes();
            //if(libTypes.Any(t=>t.Name == "MyApplication" || t.Name == "Application"))

            foreach (var libType in libTypes)
            {
                var typeNamespace = libType.Namespace+".";
                var dotCount = typeNamespace.Count(c => c == '.');
                
                for (int i = 1; i <= dotCount; i++)
                {
                    var namespaceSegment = typeNamespace.Substring(0, typeNamespace.IndexOfOccurrence('.', i));
                    if (!refCount.ContainsKey(namespaceSegment))
                        refCount.Add(namespaceSegment, 1);
                    else
                        refCount[namespaceSegment]++;
                }
            }
            var commonName = refCount.OrderByDescending(kv => kv.Value).First().Key;

            return commonName;
        }

        public Type GetType(string typeName)
        {
            return Library.GetType(typeName);
        }

        public bool ContainsType(string typeName)
        {
            return Library.GetType(typeName) != null;
        }

        public bool TypeIsForm(string typeName)
        {
            var type = Library.GetType(typeName);
            return type != null && typeof(System.Windows.Forms.Form).IsAssignableFrom(type);
        }

        public bool TypeIsControl(string typeName)
        {
            var type = Library.GetType(typeName);
            return type != null && typeof(System.Windows.Forms.Control).IsAssignableFrom(type);
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

        public ResourceManagerType GetResourceManagerType(string resourceManagerName)
        {
            var className = resourceManagerName.Substring(0, resourceManagerName.Length - 10); //strip .resources
            var classInfo = Library.GetType(className);
            if (classInfo == null)
            {
                var classNameOnly = className;
                if (className.IndexOf('.') > 0)
                    classNameOnly = className.Substring(className.LastIndexOf('.') + 1);
                classInfo = Library.GetTypes().FirstOrDefault(t => t.Name.Contains(classNameOnly));
            }
            if (classInfo != null)
            {
                if (typeof(System.Windows.Forms.Form).IsAssignableFrom(classInfo))
                    return ResourceManagerType.Form;
                else if (typeof(System.Windows.Forms.Control).IsAssignableFrom(classInfo))
                    return ResourceManagerType.Control;
                else if (typeof(System.ComponentModel.Component).IsAssignableFrom(classInfo))
                    return ResourceManagerType.Component;
            }
            if (className.EndsWith(".Resources"))
                return ResourceManagerType.Project;
            return ResourceManagerType.Other;
        }

        public override string ToString()
        {
            return _Library != null ? Name : "Invalid Module";
        }
    }
}
