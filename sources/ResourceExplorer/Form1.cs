using ResourceExplorer.ResourceAccess;
using ResourceExplorer.ResourceAccess.Managed;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResourceExplorer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var module = new ModuleInfo(@"D:\Development\github\resource-explorer\sources\ResourceExplorer\bin\Release\ResourceExplorer.exe");
            module.LoadResources();
            module.FindSatelliteAssemblies();

            Stream testStream = null;
            Image resourceImage = null;
            using (var accessor = module.GetAccessor())
            {
                var myRes = module.Resources.OfType<ManagedResourceInfo>().FirstOrDefault(r => typeof(Image).IsAssignableFrom(r.SystemType));
                if (myRes != null)
                {
                    resourceImage = accessor.GetImage(myRes);
                }
            }
            pictureBox1.Image = resourceImage;
            //var ms = new MemoryStream();
            //testStream.CopyTo(ms);
            //var data = ms.ToArray();
            //Console.WriteLine(data.Length);
        }
    }
}
