using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace ResourceExplorer.Utilities
{
    public class UserAccount
    {
        // Fields...
        private string _SID;
        private string _DomainName;
        private string _Name;

        public string SID
        {
            get { return _SID; }
        }

        public string Name
        {
            get { return _Name; }
        }

        public string DomainName
        {
            get { return _DomainName; }
        }

        public static UserAccount Parse(string fullname)
        {
            if (fullname.IndexOf('\\') >= 0)
            {
                return new UserAccount() { _Name = fullname.Split('\\')[1], _DomainName = fullname.Split('\\')[0] };
            }
            return new UserAccount() { _Name = fullname/*, DomainName = "."*/ };
        }

        private static UserAccount _CurrentUser;

        public static UserAccount CurrentUser
        {
            get
            {
                if (_CurrentUser == null)
                {
                    try
                    {
                        var currentId = WindowsIdentity.GetCurrent();
                        if (currentId.User != null)
                        {
                            _CurrentUser = Parse(currentId.Name);
                            _CurrentUser._SID = currentId.User.Value;
                        }
                    }
                    catch { }
                }
                return _CurrentUser;
            }
        }
    }
}
