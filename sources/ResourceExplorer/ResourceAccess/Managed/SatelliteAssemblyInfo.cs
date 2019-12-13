using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public class SatelliteAssemblyInfo
    {
        public string Location { get; }

        public CultureInfo Culture { get; }

        //public bool IsLoaded
        //{
        //    get { return _IsLoaded; }
        //}

        public SatelliteAssemblyInfo(string location, CultureInfo culture)
        {
            Location = location;
            Culture = culture;
        }

        //public void LoadIntoAppDomain()
        //{
        //    if (IsLoaded)
        //        return;
        //    Assembly.Load(Location);
        //    _IsLoaded = true;
        //}
    }
}
