using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                catch (AppDomainUnloadedException adue) { }
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
            var tmpAssem = CreateRefObject<TemporaryAssembly>();
            tmpAssem.Initialize(assemblyFile);
            return tmpAssem;
        }
    }
}
