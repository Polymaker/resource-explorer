using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResourceExplorer.ResourceAccess.Native
{
    public struct NativeResourceType
    {
        public int ID { get; }
        public string Name { get; }

        public bool IsKnownType => ID > 0;
        public bool IsCustom => !IsKnownType;

        public KnownResourceType KnownType => (KnownResourceType)ID;

        public NativeResourceType(int id)
        {
            ID = id;
            Name = null;// Enum.GetName(typeof(KnownResourceType), (KnownResourceType)id);
        }

        public NativeResourceType(string name)
        {
            ID = 0;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj is NativeResourceType type)
                return ID > 0 ? ID == type.ID : Name == type.Name;
            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 1479869798;
            if (ID > 0)
                hashCode = hashCode * -1521134295 + ID.GetHashCode();
            else
                hashCode = hashCode * -1521134295 + Name.GetHashCode();
            return hashCode;
        }

        public IntPtr GetLPSZ()
        {
            if (IsKnownType)
                return new IntPtr(ID);
            return Marshal.StringToHGlobalAnsi(Name);
        }

        public override string ToString()
        {
            return IsKnownType ? KnownType.ToString() : Name;
        }
    }
}
