using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class TemporaryAppDomain : IDisposable
    {
        private AppDomain Domain;
        private static ConcurrentDictionary<int, TemporaryAppDomain> ActiveDomains;
        private ConcurrentDictionary<string, TemporaryAssembly> _LoadedAssemblies;

        private CrossDomainSerializer Deserializer;
        private TemporaryAssemblyResolver AssemblyResolver;
        internal static List<string> AssemblyDirectories { get; } = new List<string>();

        public int Id { get; }

        public string Name => Domain.FriendlyName;

        public bool IsDisposed { get; private set; }

        public IEnumerable<TemporaryAssembly> LoadedAssemblies
        {
            get { return _LoadedAssemblies.Values; }
        }

        #region Constructors...

        static TemporaryAppDomain()
        {
            ActiveDomains = new ConcurrentDictionary<int, TemporaryAppDomain>();
        }

        public TemporaryAppDomain(string name/*, string directory = null*/)
        {
            _LoadedAssemblies = new ConcurrentDictionary<string, TemporaryAssembly>();

            //if (!string.IsNullOrEmpty(directory))
            //{
            //    AppDomainSetup domaininfo = new AppDomainSetup
            //    {
            //        ApplicationBase = directory
            //    };
            //    var adEvidence = AppDomain.CurrentDomain.Evidence;
            //    Domain = AppDomain.CreateDomain(name, adEvidence, domaininfo);
            //}
            //else
                Domain = AppDomain.CreateDomain(name);

            Id = Domain.Id;
            Register(this);

            //if (!string.IsNullOrEmpty(directory))
            //{
            //    var curAsm = Assembly.GetExecutingAssembly().Location;
            //    Assembly.LoadFrom(curAsm);
            //    //LoadFrom(curAsm);
            //}

            Deserializer = CreateRefObject<CrossDomainSerializer>();
            AssemblyResolver = CreateRefObject<TemporaryAssemblyResolver>();
            AssemblyResolver.Attach();
            AssemblyResolver.SetNonManagedAssemblies(TemporaryAssemblyResolver.NonManagedAssemblies.ToArray());


        }

        public TemporaryAppDomain()
            : this(Guid.NewGuid().ToString()) { }



        #endregion

        #region Deconstructors...

        ~TemporaryAppDomain()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsDisposed = true;
            Unregister(this);
            if (Domain != null)
            {
                TemporaryAssemblyResolver.AddNonManagedAssemblies(AssemblyResolver.GetNonManagedAssemblies());
                AssemblyResolver.Dettach();
                AssemblyResolver = null;
                Deserializer = null;
                try { AppDomain.Unload(Domain); }
                catch (AppDomainUnloadedException)
                {
                    Trace.WriteLine("It looks like an object obtained from a temporary AppDomain is still referenced in the main AppDomain.");
                }
                catch (CannotUnloadAppDomainException)
                {
                    Trace.WriteLine("It looks like an object obtained from a temporary AppDomain is still referenced in the main AppDomain.");
                }
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

        private static bool Unregister(TemporaryAppDomain domain)
        {
            int ctr = 0;
            while (ctr++ < 10)
            {
                if (ActiveDomains.TryRemove(domain.Id, out _))
                    return true;
            }
            return false;
        }

        public static TemporaryAppDomain Current
        {
            get
            {
                var curAppDom = AppDomain.CurrentDomain;
                if (ActiveDomains.TryGetValue(curAppDom.Id, out TemporaryAppDomain value) && !value.IsDisposed)
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

            var tmpAssembly = CreateRefObject<TemporaryAssembly>();

            if (!tmpAssembly.LoadFrom(assemblyFile))
                return null;

            _LoadedAssemblies.TryAdd(tmpAssembly.FullName, tmpAssembly);
            return tmpAssembly;
        }

        public TemporaryAssembly Load(string assemFullname)
        {
            if (_LoadedAssemblies.ContainsKey(assemFullname))
                return _LoadedAssemblies[assemFullname];

            var tmpAssembly = CreateRefObject<TemporaryAssembly>();

            if (!tmpAssembly.Load(assemFullname))
                return null;

            _LoadedAssemblies.TryAdd(tmpAssembly.FullName, tmpAssembly);
            return tmpAssembly;
        }

        public string GetAssemblyLocation(AssemblyName assemblyName)
        {
            try
            {
                var tmpAssembly = CreateRefObject<TemporaryAssembly>();
                if (tmpAssembly.Load(assemblyName.FullName))
                {
                    return tmpAssembly.Location;
                }
            }
            catch { }
            return string.Empty;
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

                objectStream.Seek(0, SeekOrigin.Begin);

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
