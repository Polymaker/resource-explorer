﻿using ResourceExplorer.ResourceAccess;
using ResourceExplorer.ResourceAccess.Managed;
using ResourceExplorer.ResourceAccess.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ResourceExplorer.UI
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            BuildRecentFilesMenu();
            treeImageList.Images.Add("blank", Properties.Resources.blank_16x16);
            treeImageList.Images.Add("exe", Properties.Resources.executable_16x16);
            treeImageList.Images.Add("dll", Properties.Resources.dll_file_16x16);
            treeImageList.Images.Add("folder", Properties.Resources.folder_16x16);
            treeImageList.Images.Add("resx", Properties.Resources.resx_file_16x16);

            treeViewResources.CanExpandGetter = delegate (object x)
            {
                if (x is BaseTreeNode)
                    return ((BaseTreeNode)x).HasChilds();
                return false;
            };
            treeViewResources.ChildrenGetter = delegate (object x)
            {
                if (x is BaseTreeNode)
                    return ((BaseTreeNode)x).GetChildrens();
                return new ArrayList();
            };
            olvColumn1.ImageGetter = delegate (object x)
            {
                if (x is BaseTreeNode)
                    return ((BaseTreeNode)x).ImageKey;
                return null;
            };
        }

        #region Menu & Toolbar

        private void BuildRecentFilesMenu()
        {
            //clear the menu
            if (tsmiRecentFiles.HasDropDownItems)
            {
                for (int i = tsmiRecentFiles.DropDownItems.Count - 1; i >= 0; i--)
                {
                    var subItem = tsmiRecentFiles.DropDownItems[i];
                    tsmiRecentFiles.DropDownItems.Remove(subItem);
                    subItem.Dispose();
                }
            }
            //TODO: implement config system & include recent files
            tsmiRecentFiles.DropDownItems.Add("C:\\Foo\\Bar.exe");
        }

        private void OpenExecutableOrAssembly()
        {
            using (var fileDlg = new OpenFileDialog())
            {
                fileDlg.Filter = "Executables Files|*.exe;*.dll|All Files|*.*";
                fileDlg.CheckFileExists = true;
                if (fileDlg.ShowDialog() == DialogResult.OK)
                {
                    if (!ModuleInfo.IsValid(fileDlg.FileName))
                    {
                        MessageBox.Show("The specified file does not appear to be a valid executable or assembly.");
                        return;
                    }
                    if (!ModuleInfo.CanOpen(fileDlg.FileName))
                    {
                        MessageBox.Show("The specified executable (or assembly) cannot be opened.");
                        return;
                    }
                    AddModuleResources(new ModuleInfo(fileDlg.FileName), true);
                }
            }
        }

        private void tsbOpenAssembly_Click(object sender, EventArgs e)
        {
            OpenExecutableOrAssembly();
        }

        private void tsmiOpenFile_Click(object sender, EventArgs e)
        {
            OpenExecutableOrAssembly();
        }

        #endregion

        #region Resource TreeView

        private void treeViewResources_CellRightClick(object sender, BrightIdeasSoftware.CellRightClickEventArgs e)
        {
            if (treeViewResources.SelectedObjects.Count == 1)
            {
                if (treeViewResources.SelectedObjects[0] is ModuleRefNode)
                {
                    var moduleNode = (ModuleRefNode)treeViewResources.SelectedObjects[0];
                    loadModuleToolStripMenuItem.Enabled = !string.IsNullOrEmpty(moduleNode.Module.Location);
                    ShowTreeViewMenu(TreeViewMenuOption.LoadModule | TreeViewMenuOption.OpenLocation,
                        string.IsNullOrEmpty(moduleNode.Module.Location) ? TreeViewMenuOption.LoadModule | TreeViewMenuOption.OpenLocation : TreeViewMenuOption.None);
                }
                else if (treeViewResources.SelectedObjects[0] is ResourceNode)
                {
                    var resourceNode = (ResourceNode)treeViewResources.SelectedObjects[0];
                    if(resourceNode.CanExport())
                        ShowTreeViewMenu(TreeViewMenuOption.ExportResource);
                }
                else if (treeViewResources.SelectedObjects[0] is ModuleNode)
                {
                    ShowTreeViewMenu(TreeViewMenuOption.RemoveModule);
                }
            }
            else if (treeViewResources.SelectedObjects.Count > 1)
            {

            }
        }

        private void AddModuleResources(ModuleInfo module, bool expand = false)
        {
            module.FindReferencedModules();
            module.FindSatelliteAssemblies();
            IList selectedObjects = treeViewResources.SelectedObjects;
            var treeNode = new ModuleNode(module);

            var curScrollPos = treeViewResources.LowLevelScrollPosition;
            var expandedObjs = treeViewResources.ExpandedObjects.OfType<BaseTreeNode>().ToArray();

            treeViewResources.AddObject(treeNode);

            if (expand)
                treeViewResources.Expand(treeNode);
            //module.LoadResources();
            //FillImageResourcesList(module);
        }

        #region Context Menu

        [Flags]
        private enum TreeViewMenuOption
        {
            None = 0,
            LoadModule = 1,
            RemoveModule = 2,
            ExportResource = 4,
            OpenLocation = 8,
            All = LoadModule | RemoveModule | ExportResource | OpenLocation
        }

        private void ShowTreeViewMenu(TreeViewMenuOption visibleOptions, TreeViewMenuOption disabledOptions = TreeViewMenuOption.None)
        {
            loadModuleToolStripMenuItem.Visible = visibleOptions.HasFlag(TreeViewMenuOption.LoadModule);
            loadModuleToolStripMenuItem.Enabled = !disabledOptions.HasFlag(TreeViewMenuOption.LoadModule);

            removeToolStripMenuItem.Visible = visibleOptions.HasFlag(TreeViewMenuOption.RemoveModule);
            removeToolStripMenuItem.Enabled = !disabledOptions.HasFlag(TreeViewMenuOption.RemoveModule);

            exportToolStripMenuItem.Visible = visibleOptions.HasFlag(TreeViewMenuOption.ExportResource);
            exportToolStripMenuItem.Enabled = !disabledOptions.HasFlag(TreeViewMenuOption.ExportResource);

            openFileLocationToolStripMenuItem.Visible = visibleOptions.HasFlag(TreeViewMenuOption.OpenLocation);
            openFileLocationToolStripMenuItem.Enabled = !disabledOptions.HasFlag(TreeViewMenuOption.OpenLocation);

            treeViewMenu.Show(treeViewResources, treeViewResources.PointToClient(Cursor.Position));

        }

        private void loadModuleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewResources.SelectedObjects.Count == 1
                && treeViewResources.SelectedObjects[0] is ModuleRefNode)
            {
                var moduleNode = (ModuleRefNode)treeViewResources.SelectedObjects[0];
                if (!String.IsNullOrEmpty(moduleNode.Module.Location))
                {
                    AddModuleResources(new ModuleInfo(moduleNode.Module.Location));
                }
            }
        }

        private void openFileLocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewResources.SelectedObjects.Count == 1
                && treeViewResources.SelectedObjects[0] is ModuleRefNode)
            {
                var moduleNode = (ModuleRefNode)treeViewResources.SelectedObjects[0];
                if (!String.IsNullOrEmpty(moduleNode.Module.Location) && File.Exists(moduleNode.Module.Location))
                {
                    Process.Start("explorer.exe", "/select, \"" + moduleNode.Module.Location + "\"");
                }
            }
        }

        #endregion

        #region  TreeView Model

        class BaseTreeNode
        {
            static int curID = 0;
            public int ID = ++curID;
            public string Text { get; set; }
            public string Description { get; set; }
            public string ImageKey { get; set; }
            private ArrayList childs;

            public virtual bool HasChilds()
            {
                return false;
            }

            protected virtual ArrayList FetchChildrens()
            {
                return new ArrayList();
            }

            public ArrayList GetChildrens()
            {
                if (childs == null)
                    childs = FetchChildrens();
                return childs;
            }
        }

        class NodeGroup : BaseTreeNode
        {
            public IEnumerable<BaseTreeNode> Collection { get; set; }
            public NodeGroup(string name, IEnumerable<BaseTreeNode> collection)
            {
                Text = name;
                Collection = collection;
                ImageKey = "folder";
            }
            public override bool HasChilds()
            {
                return Collection != null && Collection.Count() > 0;
            }
            protected override ArrayList FetchChildrens()
            {
                return new ArrayList(Collection.ToArray());
            }
        }

        class ModuleNode : BaseTreeNode
        {
            public ModuleInfo Module { get; set; }

            public ModuleNode(ModuleInfo module)
            {
                Module = module;
                Text = module.Name;
                ImageKey = module.FileName.Contains("dll") ? "dll" : "exe";
                Description = string.Format("{0} ({1})", module.IsManaged ? ".Net" : "Native", module.Architecture);
            }

            public override bool HasChilds()
            {
                if (!Module.ResourcesLoaded)
                    return true;
                return Module.Resources.Count > 0;
            }

            protected override ArrayList FetchChildrens()
            {
                var nodes = new ArrayList();
                if (Module.ReferencedModules.Count > 0)
                {
                    nodes.Add(new NodeGroup("Referenced Modules",
                        Module.ReferencedModules
                            .OrderByDescending(m => m.Type == ModuleType.Native)
                            .ThenByDescending(m => m.IsSystemModule())
                            .ThenBy(m => m.ModuleName)
                            .Select(x => new ModuleRefNode(x))));
                }
                if (Module.SatelliteAssemblies.Count > 0)
                {
                    nodes.Add(new NodeGroup("Satellite Assemblies",
                        Module.SatelliteAssemblies.Select(x => new SatelliteAssemblyNode(x))));
                }

                nodes.Add(new ResourceTypeNode(Module, true));
                if (Module.IsManaged)
                    nodes.Add(new ResourceTypeNode(Module, false));
                return nodes;
            }
        }

        class ModuleRefNode : BaseTreeNode
        {
            public ModuleRef Module { get; set; }
            public ModuleRefNode(ModuleRef module)
            {
                Module = module;
                Text = module.ModuleName;
                Description = module.Type.ToString();
                ImageKey = "dll";
            }
        }

        class SatelliteAssemblyNode : BaseTreeNode
        {
            public SatelliteAssemblyInfo Module { get; set; }
            public SatelliteAssemblyNode(SatelliteAssemblyInfo module)
            {
                Module = module;
                Text = Path.GetFileName(module.Location);
                Description = string.Format("{0} ({1})", module.Culture.DisplayName, module.Culture.Name);
                ImageKey = "dll";
            }
        }

        class ResourceTypeNode : BaseTreeNode
        {
            public ModuleInfo Module { get; set; }
            public bool IsNative { get; set; }

            public ResourceTypeNode(ModuleInfo module, bool isNative)
            {
                Module = module;
                IsNative = isNative;
                Text = isNative ? "Native Resources" : "Managed Resources";
                ImageKey = "folder";
            }

            public override bool HasChilds()
            {
                if (!Module.ResourcesLoaded)
                    return true;
                if (IsNative)
                    return Module.NativeResources.Count() > 0;
                else
                    return Module.ManagedResources.Count() > 0;
            }

            protected override ArrayList FetchChildrens()
            {
                if (!Module.ResourcesLoaded)
                    Module.LoadResources();

                if (IsNative)
                {
                    return new ArrayList(Module.NativeResourceTypes
                        .Select(x => new NativeResourceTypeNode(Module, x)).ToArray());
                    //return new ArrayList(Module.NativeResources.Select(x => new ResourceNode(x)).ToArray());
                }
                else
                {
                    return new ArrayList(Module.ManagedResources
                        .Where(x => string.IsNullOrEmpty(x.ResourceManagerName))
                        .Select(x => new ResourceNode(x)).OrderBy(x => x.Text).ToArray());
                }
            }
        }

        class NativeResourceTypeNode : BaseTreeNode
        {
            public ModuleInfo Module { get; set; }
            public NativeResourceType ResourceType { get; set; }

            public NativeResourceTypeNode(ModuleInfo module, NativeResourceType resourceType)
            {
                Module = module;
                ResourceType = resourceType;
                Text = resourceType.ToString();
                ImageKey = "folder";
            }

            public override bool HasChilds()
            {
                if (!Module.ResourcesLoaded)
                    return true;
                return Module.NativeResources.Any(x => x.ResourceType == ResourceType);
            }

            protected override ArrayList FetchChildrens()
            {
                return new ArrayList(Module.NativeResources
                    .Where(x=>x.ResourceType == ResourceType)
                    .Select(x => new ResourceNode(x)).ToArray());
            }
        }

        class ResourceNode : BaseTreeNode
        {
            public ResourceInfo Resource { get; set; }
            public ResourceNode(ResourceInfo resource)
            {
                Resource = resource;
                Text = resource.Name;
                ImageKey = "blank";
                if (resource is ManagedResourceInfo)
                {
                    var manRes = (ManagedResourceInfo)resource;
                    Description = manRes.Kind.ToString();
                    if (resource is ResourceManagerInfo)
                    {
                        var resManInfo = (ResourceManagerInfo)resource;
                        Text = resManInfo.ShortName;
                        ImageKey = "resx";
                        switch (resManInfo.DesignerType)
                        {
                            case ResourceManagerType.Form:
                            case ResourceManagerType.Control:
                            case ResourceManagerType.Component:
                                Description = resManInfo.DesignerType + " Designer";
                                break;
                            case ResourceManagerType.Project:
                                Description = "Project's Resources";
                                break;
                            default:
                                Description = resManInfo.DesignerType.ToString();
                                break;
                        }
                    }
                    else if (manRes.Kind == ManagedResourceType.ResourceEntry)
                    {
                        Description = manRes.SystemType.Name;
                    }
                }
                else if (resource is NativeResourceInfo)
                {
                    Description = ((NativeResourceInfo)resource).ResourceType.ToString();
                }
            }
            public override bool HasChilds()
            {
                if (Resource is ResourceManagerInfo)
                    return ((ResourceManagerInfo)Resource).Entries.Count() > 0;
                return base.HasChilds();
            }
            protected override ArrayList FetchChildrens()
            {
                if (Resource is ResourceManagerInfo)
                    return new ArrayList(((ResourceManagerInfo)Resource).Entries
                        .Select(x => new ResourceNode(x))
                        .OrderBy(x => x.Text).ToArray());
                return base.FetchChildrens();
            }
            public bool CanExport()
            {
                if (Resource.IsNative)
                    return true;
                else
                {
                    var manRes = (ManagedResourceInfo)Resource;
                    return manRes.Kind != ManagedResourceType.ResourceManager;
                }
            }
        }

        #endregion

        #endregion

        private void FillImageResourcesList(ModuleInfo module)
        {
            ResourcesImages.Images.Clear();
            ImagesListView.Items.Clear();
            int imageIndex = 0;
            using (var accessor = module.GetAccessor())
            {
                foreach (var resource in module.Resources)
                {
                    try
                    {
                        if (resource.ContentType == ContentType.Image)
                        {
                            var image = accessor.GetImage(resource);
                            if (image == null)
                                continue;

                            if (image.Width != image.Height || image.Width < 64)
                            {
                                var bmp2 = new Bitmap(64, 64);
                                float scale = (image.Width > image.Height ? 64f / (float)image.Width : 64f / (float)image.Height);

                                using (var g = Graphics.FromImage(bmp2))
                                {
                                    g.InterpolationMode = scale < 1 ?
                                        System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic :
                                        System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                    g.DrawImage(image, 0, 0, (int)(image.Width * scale), (int)(image.Height * scale));
                                }
                                image.Dispose();
                                image = bmp2;
                            }

                            string resName = $"Res_{imageIndex++}";
                            ResourcesImages.Images.Add(resName, image);
                            var lvi = new ListViewItem(resource.Name);
                            lvi.ImageKey = resName;

                            ImagesListView.Items.Add(lvi);
                        }
                        else if (resource.ContentType == ContentType.Icon)
                        {
                            var image = accessor.GetIcon(resource);
                            if (image == null)
                                continue;
                            string resName = $"Res_{imageIndex++}";
                            ResourcesImages.Images.Add(resName, image);
                            var lvi = new ListViewItem(resource.Name);
                            lvi.ImageKey = resName;
                            ImagesListView.Items.Add(lvi);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    
                }
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewResources.SelectedObjects.Count == 1
                && treeViewResources.SelectedObjects[0] is ResourceNode)
            {
                var resourceNode = (ResourceNode)treeViewResources.SelectedObjects[0];
                if (resourceNode.Resource.ContentType == ContentType.Image)
                {
                    Image resImage = null;
                    using (var resAccess = resourceNode.Resource.Module.GetAccessor())
                        resImage = resAccess.GetImage(resourceNode.Resource);


                    if (resImage != null)
                    {
                        using (var sfd = new SaveFileDialog())
                        {
                            var imageFormat = resImage.RawFormat;
                            if (resImage.PixelFormat == PixelFormat.Format32bppArgb)
                                imageFormat = ImageFormat.Png;

                            if (imageFormat.Equals(ImageFormat.Png))
                                sfd.FileName = $"{resourceNode.Resource.Name}.png";
                            else
                                sfd.FileName = $"{resourceNode.Resource.Name}.bmp";

                            if (sfd.ShowDialog() == DialogResult.OK)
                            {
                                resImage.Save(sfd.FileName, imageFormat);
                            }
                        }
                    }

                    pictureBox1.Image = resImage;
                }
                else if (resourceNode.Resource.ContentType == ContentType.Icon)
                {
                    using (var resAccess = resourceNode.Resource.Module.GetAccessor())
                    {
                        var icon = resAccess.GetIcon(resourceNode.Resource);
                        if (icon != null)
                            pictureBox1.Image = icon.ToBitmap();
                        else
                            pictureBox1.Image = null;
                    }
                }
            }
        }
    }
}
