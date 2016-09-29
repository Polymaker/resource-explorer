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
    internal class CrossDomainDeserializer : MarshalByRefObject
    {
        public Stream GetImageStream(Image image)
        {
            var ms = new MemoryStream();
            try
            {
                image.Save(ms, image.RawFormat);
            }
            catch
            {
                image.Save(ms, ImageFormat.Bmp);
            }
            return ms;
        }

        public Stream GetIconStream(Icon icon)
        {
            var ms = new MemoryStream();
            icon.Save(ms);
            return ms;
        }

        public object DeserializeObject(object value, Type objectType)
        {
            if (value == null)
                return value;
            if (!objectType.IsClass)
                return value;//struct are safe, probably...

            if (typeof(Stream).IsAssignableFrom(objectType))
            {
                var originalStream = (Stream)value;
                var ms = new MemoryStream();
                originalStream.CopyTo(ms);
                //ms.Seek(0, SeekOrigin.Begin);
                originalStream.Dispose();
                return ms;
            }
            else if (typeof(Image).IsAssignableFrom(objectType))
            {
                Trace.WriteLine("You probably want to use TemporaryAppDomain.DeserializeImage");
                return GetImageStream((Image)value);
            }
            else if (typeof(ISerializable).IsAssignableFrom(objectType) ||
                objectType.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
            {
                try
                {
                    var binaryFormatter = new BinaryFormatter();
                    using (var ms = new MemoryStream())
                    {
                        binaryFormatter.Serialize(ms, value);
                        return binaryFormatter.Deserialize(ms);
                    }
                }
                catch { }
            }
            return value;
        }
    }
}
