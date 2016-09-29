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
        //private bool _IsLoaded;
        private readonly string _Location;
        private readonly CultureInfo _Culture;

        public string Location
        {
            get { return _Location; }
        }

        public CultureInfo Culture
        {
            get { return _Culture; }
        }

        //public bool IsLoaded
        //{
        //    get { return _IsLoaded; }
        //}

        public SatelliteAssemblyInfo(string location, CultureInfo culture)
        {
            _Location = location;
            _Culture = culture;
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
