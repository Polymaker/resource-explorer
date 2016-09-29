using ResourceExplorer.ResourceAccess;
using ResourceExplorer.ResourceAccess.Managed;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
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

            var module = new ModuleInfo(@"D:\Development\github\ldd-modder\LDDModder\Application\bin\Debug\LDD Modder.exe");
            module.LoadResources();
            module.FindSatelliteAssemblies();

            //Stream testStream = null;
            //Image resourceImage = null;
            using (var accessor = module.GetAccessor())
            {
                foreach (var managedRes in module.Resources.OfType<ManagedResourceInfo>())
                {
                    var lvi = new ListViewItem(managedRes.Name);
                    lvi.SubItems.Add(managedRes.Kind.ToString());
                    lvi.SubItems.Add(managedRes.SystemType.Name);
                    var typeConv = TypeDescriptor.GetConverter(managedRes.SystemType);
                    if (typeConv != null && typeConv.CanConvertTo(typeof(string)))
                    {
                        var resValue = accessor.GetObject(managedRes);
                        lvi.SubItems.Add((string)typeConv.ConvertTo(resValue, typeof(string)));
                    }
                    listView1.Items.Add(lvi);
                }
                //var myRes = module.Resources.OfType<ManagedResourceInfo>().FirstOrDefault(r => typeof(Image).IsAssignableFrom(r.SystemType));
                //if (myRes != null)
                //{
                //    resourceImage = accessor.GetImage(myRes);
                //}
            }
        }
    }
}
