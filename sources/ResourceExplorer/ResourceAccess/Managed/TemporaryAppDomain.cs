using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ResourceExplorer.ResourceAccess.Managed
{
    [Serializable]
    public class TemporaryAppDomain : IDisposable
    {
        private readonly int _Id;
        private bool _IsDisposed;
        private AppDomain Domain;
        private static ConcurrentDictionary<int, TemporaryAppDomain> ActiveDomains;
        private ConcurrentDictionary<string, TemporaryAssembly> _LoadedAssemblies;

        private CrossDomainSerializer Deserializer;

        public int Id
        {
            get { return _Id; }
        }
        
        public string Name
        {
            get { return Domain.FriendlyName; }
        }

        public bool IsDisposed
        {
            get { return _IsDisposed; }
        }

        public IEnumerable<TemporaryAssembly> LoadedAssemblies
        {
            get { return _LoadedAssemblies.Values; }
        }

        #region constructors...

        static TemporaryAppDomain()
        {
            ActiveDomains = new ConcurrentDictionary<int, TemporaryAppDomain>();
        }

        public TemporaryAppDomain(string name)
        {
            Domain = AppDomain.CreateDomain(name);
            _Id = Domain.Id;
            Register(this);
            Deserializer = CreateRefObject<CrossDomainSerializer>();
            _LoadedAssemblies = new ConcurrentDictionary<string, TemporaryAssembly>();
        }

        public TemporaryAppDomain()
            : this(Guid.NewGuid().ToString()) { }

        #endregion

        #region deconstructors...

        ~TemporaryAppDomain()
        {
            Dispose();
        }

        public void Dispose()
        {
            _IsDisposed = true;
            Unregister(this);
            if (Domain != null)
            {
                try { AppDomain.Unload(Domain); }
                catch (AppDomainUnloadedException) { }
                Domain = null;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Temp domains register

        private static void Register(TemporaryAppDomain domain)
        {
            ActiveDomains.TryAdd(domain.Id, domain);
        }

        private static void Unregister(TemporaryAppDomain domain)
        {
            TemporaryAppDomain dummy;
            ActiveDomains.TryRemove(domain.Id, out dummy);
        }

        //private static void 

        public static TemporaryAppDomain Current
        {
            get
            {
                var curAppDom = AppDomain.CurrentDomain;
                TemporaryAppDomain value;
                if (ActiveDomains.TryGetValue(curAppDom.Id, out value) && !value.IsDisposed)
                    return value;
                return null;
            }
        }

        #endregion

        public static T CreateRefObject<T>(AppDomain appDom, params object[] args) where T : MarshalByRefObject
        {
            return (T)appDom.CreateInstanceAndUnwrap(
                typeof(T).Assembly.FullName,
                typeof(T).FullName,
                false,
                BindingFlags.Default,
                null,
                args,
                null,
                new object[0]
             );
        }

        public static T CreateRefObject<T>(AppDomain appDom) where T : MarshalByRefObject
        {
            return (T)appDom.CreateInstanceAndUnwrap(
                typeof(T).Assembly.FullName,
                typeof(T).FullName);
        }

        public T CreateRefObject<T>(params object[] args) where T : MarshalByRefObject
        {
            return CreateRefObject<T>(Domain, args);
        }

        public T CreateRefObject<T>() where T : MarshalByRefObject
        {
            return CreateRefObject<T>(Domain);
        }

        public TemporaryAssembly LoadFrom(string assemblyFile)
        {
            var assemFullname = AssemblyName.GetAssemblyName(assemblyFile).FullName;
            if (_LoadedAssemblies.ContainsKey(assemFullname))
                return _LoadedAssemblies[assemFullname];

            var tmpAssembly = CreateRefObject<TemporaryAssembly>(assemblyFile);
            _LoadedAssemblies.TryAdd(tmpAssembly.FullName, tmpAssembly);
            return tmpAssembly;
        }

        public object ReleaseObject(object value, Type objectType)
        {
            if (value == null)
                return null;

            if (!objectType.IsClass)
                return value;

            if (!RemotingServices.IsObjectOutOfAppDomain(value))
                return value;
            try
            {
                var objectStream = Deserializer.SerializeObjectToStream(value);

                if (objectStream == null)
                    return null;

                if (typeof(Stream).IsAssignableFrom(objectType))
                {
                    var ms = new MemoryStream();
                    objectStream.CopyTo(ms);
                    return ms;
                }

                if (typeof(Image).IsAssignableFrom(objectType))
                    return Image.FromStream(objectStream);

                if (typeof(Icon).IsAssignableFrom(objectType))
                    return new Icon(objectStream);

                if (typeof(ISerializable).IsAssignableFrom(objectType) ||
                    objectType.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
                {
                    var binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(objectStream);
                }
            }
            finally
            {
                Deserializer.DisposeOriginal(value);
            }
            return null;
        }

        public T ReleaseObject<T>(T value)
        {
            return (T)ReleaseObject(value, typeof(T));
        }
    }
}
