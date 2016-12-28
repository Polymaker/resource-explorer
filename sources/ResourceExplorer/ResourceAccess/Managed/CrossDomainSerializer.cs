using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ResourceExplorer.ResourceAccess.Managed
{
    internal class CrossDomainSerializer : MarshalByRefObject
    {

        public Stream SerializeObjectToStream(object value)
        {
            if (value == null)
                return null;

            var objectType = value.GetType();
            if (!objectType.IsClass)
                return null;

            if (typeof(Stream).IsAssignableFrom(objectType))
            {
                return value as Stream;
            }
            else if (typeof(Image).IsAssignableFrom(objectType))
            {
                var ms = new MemoryStream();
                var image = (Image)value;
                try { image.Save(ms, image.RawFormat); }
                catch { image.Save(ms, ImageFormat.Bmp); }
                return ms;
            }
            else if (typeof(Icon).IsAssignableFrom(objectType))
            {
                var ms = new MemoryStream();
                var icon = (Icon)value;
                icon.Save(ms);
                return ms;
            }
            else if (typeof(ISerializable).IsAssignableFrom(objectType) ||
                objectType.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
            {
                try
                {
                    var binaryFormatter = new BinaryFormatter();
                    var ms = new MemoryStream();
                    binaryFormatter.Serialize(ms, value);
                    return ms;
                }
                catch(Exception ex)
                {
                    Trace.WriteLine("Could not serialize object to stream:\r\n" + ex.ToString());
                }
            }

            return null;
        }

        public void DisposeOriginal(object value)
        {
            if (value == null)
                return;

            var objectType = value.GetType();
            if (!objectType.IsClass)
                return;

            if (value is IDisposable)
                ((IDisposable)value).Dispose();
        }
    }
}
