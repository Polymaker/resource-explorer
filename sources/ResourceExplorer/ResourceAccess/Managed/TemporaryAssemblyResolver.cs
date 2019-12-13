using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace ResourceExplorer.ResourceAccess.Managed
{
    internal class TemporaryAssemblyResolver : MarshalByRefObject
    {
        internal static List<string> AssemblyDirectories { get; } = new List<string>();
        internal static List<string> NonManagedAssemblies { get; set; } = new List<string>();

        public void Attach()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public void Dettach()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }

        public string[] GetNonManagedAssemblies()
        {
            return NonManagedAssemblies.ToArray();
        }

        public void SetNonManagedAssemblies(string[] nonManagedAssemblies)
        {
            AddNonManagedAssemblies(nonManagedAssemblies);
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (var asssemblyDir in AssemblyDirectories)
            {
                foreach (var assemFile in Directory.EnumerateFiles(asssemblyDir, "*.dll"))
                {
                    if (NonManagedAssemblies.Contains(assemFile))
                        continue;

                    try
                    {
                        var name = AssemblyName.GetAssemblyName(assemFile);
                        if (name.FullName == args.Name)
                            return Assembly.LoadFrom(assemFile);
                    }
                    catch (BadImageFormatException)
                    {
                        NonManagedAssemblies.Add(assemFile);
                    }
                    catch 
                    { 
                    
                    }

                }
            }
            return null;
        }
    
        public static void AddNonManagedAssemblies(IEnumerable<string> nonManagedAssemblies)
        {
            var allFiles = NonManagedAssemblies.Concat(nonManagedAssemblies).Distinct();
            NonManagedAssemblies.Clear();
            NonManagedAssemblies.AddRange(allFiles);
        }
    }
}
