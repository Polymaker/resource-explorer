using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    public enum ManagedResourceType
    {
        Embedded,
        ResourceEntry,
        ResourceManager,
        //this type will be used for resources (file handles) locked when analysing resources from a running process (if I code that...)
        Content
    }
}
