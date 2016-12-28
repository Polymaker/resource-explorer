using ResourceExplorer.ResourceAccess;
using ResourceExplorer.ResourceAccess.Managed;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
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
            //"C:\Windows\System32\user32.dll"
            //"D:\Development\github\ldd-modder\LDDModder\Application\bin\Debug\LDD Modder.exe"
            string executable = @"E:\ResourceExplorer.exe";
            if (!ModuleInfo.CanOpen(executable))
            {
                return;
            }
            var module = new ModuleInfo(executable);

            module.FindReferencedModules();
            //ModuleInfo.LoadReference(module.ReferencedModules.First(m => m.Type == ModuleType.Managed));
            module.LoadResources();
            
            module.FindSatelliteAssemblies();

            //Stream testStream = null;
            //Image resourceImage = null;
            var resCulture = CultureInfo.GetCultureInfo("fr-CA");//module.Cultures.Count > 0 ? module.Cultures.ElementAt(0) : CultureInfo.InvariantCulture;
            using (var accessor = module.GetAccessor())
            {
                foreach (var managedRes in module.Resources.OfType<ManagedResourceInfo>())
                {
                    var lvi = new ListViewItem(managedRes.Name);
                    lvi.SubItems.Add(managedRes.Kind.ToString());
                    if (managedRes is ResourceManagerInfo)
                        lvi.SubItems.Add(((ResourceManagerInfo)managedRes).DesignerType.ToString());
                    else
                        lvi.SubItems.Add(managedRes.SystemType.Name);

                    if (managedRes.IsResourceEntry)
                    {
                        var typeConv = TypeDescriptor.GetConverter(managedRes.SystemType);
                        if (typeConv != null && typeConv.CanConvertTo(typeof(string)))
                        {
                            var resValue = accessor.GetObject(managedRes, resCulture);
                            lvi.SubItems.Add((string)typeConv.ConvertTo(resValue, typeof(string)));
                        }

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

        private void button2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            GC.Collect();
        }
    }
}
