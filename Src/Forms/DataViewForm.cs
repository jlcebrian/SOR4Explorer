using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace SOR4Explorer
{
    class DataViewForm : Form
    {
        private readonly DataLibrary data;
        private SplitContainer splitContainer;

        class TreeSorterByImageIndex : System.Collections.IComparer
        {
            public int Compare(object ay, object ax)
            {
                return ax is TreeNode x && ay is TreeNode y ? 
                    x.ImageIndex == y.ImageIndex ? x.Text.CompareTo(y.Text) : x.ImageIndex.CompareTo(y.ImageIndex) : 0;
            }
        }

        public static Dictionary<string, string> PropertyCuts = new Dictionary<string, string>()
        {
            { "GuiNodeData.6.2.6", "GuiNodeData.6" },
            { "BtNodeData.7.1.5.1", "BtNodeData.7.1" },
            { "BtNodeData.7.1.10.1", "BtNodeData.7.1" },
            { "BtNodeData.7.1.25.1", "BtNodeData.7.1" },
            { "BtNodeData.7.1.29.1", "BtNodeData.7.1" },
        };

        public static Dictionary<string, string> PropertyNames = new Dictionary<string, string>()
        {
            { "MetaFont.1", "Base Font" },
            { "MetaFont.1.1", "Name" },
            { "MetaFont.7", "Font Variants" },
            { "MetaFont.7.1", "Languages" },
            { "MetaFont.7.2", "Texture" },
            { "SpriteData.1", "Size" },
            { "SpriteData.1.1", "Width" },
            { "SpriteData.1.2", "Height" },
            { "SpriteData.2", "Sprite" },
            { "SpriteData.2.1", "Texture" },
            { "SpriteData.2.2", "OuterBounds" },
            { "SpriteData.2.2.1", "X" },
            { "SpriteData.2.2.2", "Y" },
            { "SpriteData.2.2.3", "Width" },
            { "SpriteData.2.2.4", "Height" },
            { "SpriteData.2.3", "InnerBounds" },
            { "SpriteData.2.3.1", "X" },
            { "SpriteData.2.3.2", "Y" },
            { "SpriteData.2.3.3", "Width" },
            { "SpriteData.2.3.4", "Height" },
            { "LocalizationData.1", "Localization" },
            { "LocalizationData.1.1", "Language" },
            { "LocalizationData.1.2", "Strings" },
            { "LocalizationData.1.2.1", "Key" },
            { "LocalizationData.1.2.2", "Value" },
            { "GuiNodeData.6", "Contents" },
            { "GuiNodeData.6.1", "Size" },
            { "GuiNodeData.6.1.1", "Width" },
            { "GuiNodeData.6.1.2", "Height" },
            { "GuiNodeData.6.2", "Contents" },
            { "GuiNodeData.6.2.1", "Position" },
            { "GuiNodeData.6.2.1.1", "X" },
            { "GuiNodeData.6.2.1.2", "Y" },
            { "GuiNodeData.6.2.2", "Name" },
            { "GuiNodeData.6.2.5", "Color" },
            { "GuiNodeData.6.2.5.1", "R" },
            { "GuiNodeData.6.2.5.2", "G" },
            { "GuiNodeData.6.2.5.3", "B" },
            { "GuiNodeData.6.2.5.4", "A" },
            { "GuiNodeData.6.2.7", "Image" },
            { "GuiNodeData.6.2.7.1", "Texture" },
            { "GuiNodeData.6.2.8", "Label" },
            { "GuiNodeData.6.2.8.1", "Text" },
            { "GuiNodeData.6.2.8.2", "Font" },
            { "GuiNodeData.6.2.14", "Rectangle" },
            { "GuiNodeData.6.2.14.2", "Size" },
            { "GuiNodeData.6.2.14.2.1", "Width" },
            { "GuiNodeData.6.2.14.2.2", "Height" },
            { "ExtrasScreenData.1", "Galleries" },
            { "ExtrasScreenData.1.1", "Name" },
            { "ExtrasScreenData.1.2", "Info" },
            { "ExtrasScreenData.1.3", "Textures" },
            { "ExtrasScreenData.2", "Bios" },
            { "ExtrasScreenData.2.1", "Sprites" },
            { "ExtrasScreenData.2.2", "Portrait" },
            { "ExtrasScreenData.2.3", "Icon" },
            { "ExtrasScreenData.2.4", "IconUnselected" },
            { "ExtrasScreenData.2.5", "TextName" },
            { "ExtrasScreenData.2.6", "Occupation" },
            { "ExtrasScreenData.2.7", "Style" },
            { "ExtrasScreenData.2.8", "Hobby" },
            { "ExtrasScreenData.2.9", "Moves" },
            { "ExtrasScreenData.2.10", "Text" },
            { "ExtrasScreenData.2.11", "Power" },
            { "ExtrasScreenData.2.12", "Technique" },
            { "ExtrasScreenData.2.13", "Speed" },
            { "ExtrasScreenData.2.14", "Jump" },
            { "ExtrasScreenData.2.15", "Stamina" },
            { "AnimatedSpriteData.1", "Animations" },
            { "AnimatedSpriteData.1.1", "Name" },
            { "AnimatedSpriteData.1.3", "Frames" },
            { "AnimatedSpriteData.1.3.1", "Sprites" },
            { "AnimatedSpriteData.1.3.1.1", "Texture" },
        };

        #region Initialization & components

        private TreeView objectsTreeView;
        private BlackToolStrip toolStrip;
        private TreeView infoTreeView;

        public DataViewForm(DataLibrary data)
        {
            this.data = data;
            InitializeComponents();
        }

        void InitializeComponents()
        {
            SuspendLayout();

            //
            // toolStrip
            //
            toolStrip = new BlackToolStrip();
            toolStrip.AddButton(Program.BarsImage);
            toolStrip.NextAlignment = ToolStripItemAlignment.Right;

            //
            // splitterContainer
            //
            splitContainer = new SplitContainer()
            {
                BackColor = SystemColors.Control,
                Dock = DockStyle.Fill,
                Location = new Point(0, 0),
                Name = "splitContainer",
                FixedPanel = FixedPanel.Panel1,
                Size = new Size(1422, 1006),
                SplitterDistance = 650,
                TabIndex = 0,
            };

            //
            // objectsList
            //
            objectsTreeView = new BufferedTreeView()
            {
                BackColor = SystemColors.Control,
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 9.0F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0))),
                ItemHeight = 40,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                HideSelection = false,
                Sorted = true,
                TreeViewNodeSorter = new TreeSorterByImageIndex(),
                ImageList = new ImageList
                {
                    ImageSize = new Size(32, 20),
                    ColorDepth = ColorDepth.Depth32Bit
                }
            };
            objectsTreeView.AfterSelect += ObjectsTreeView_AfterSelect;
            objectsTreeView.ImageList.Images.Add(Program.FolderIconSmall);
            objectsTreeView.ImageList.Images.Add(Program.SheetIconSmall);
            splitContainer.Panel1.Controls.Add(objectsTreeView);

            //
            // infoTreeView
            //
            infoTreeView = new BufferedTreeView()
            {
                BackColor = SystemColors.ControlDark,
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 9.0F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0))),
                ItemHeight = 40,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                HideSelection = false,
                DrawMode = TreeViewDrawMode.OwnerDrawText,
                ImageList = new ImageList
                {
                    ImageSize = new Size(32, 20),
                    ColorDepth = ColorDepth.Depth32Bit
                }
            };
            infoTreeView.DrawNode += InfoTreeView_DrawNode;
            infoTreeView.ImageList.Images.Add(Program.FolderIconSmall);
            infoTreeView.ImageList.Images.Add(Program.SheetIconSmall);
            splitContainer.Panel2.Controls.Add(infoTreeView);

            //
            // Finish
            //
            Size = new Size(1800, 1000);
            Icon = Program.Icon;
            Text = "Bigfile contents";
            Controls.Add(splitContainer);

            ResumeLayout();
        }

        private void InfoTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Bounds.Height == 0)
                return;

            var node = e.Node;
            var bounds = e.Bounds;
            var treeView = e.Node.TreeView;
            Font font = node.NodeFont ?? treeView.Font;
            var color = treeView.SelectedNode == node ? SystemColors.HighlightText : treeView.ForeColor;
            if (treeView.SelectedNode == node)
                e.Graphics.FillRectangle(treeView.Focused ? SystemBrushes.Highlight : Brushes.Gray, e.Bounds);

            bool isArrayMember = node.Nodes.Count > 0 && node.Text.All(n => Char.IsDigit(n));
            if (node.Tag != null && (node.IsExpanded == false || !isArrayMember))
            {
                var valueBounds = e.Bounds;
                valueBounds.X = Math.Max(400, e.Bounds.Right + 20);
                valueBounds.Width = (int)e.Graphics.ClipBounds.Right - valueBounds.X;
                TextRenderer.DrawText(e.Graphics, node.Tag.ToString(), font, valueBounds, 
                    treeView.ForeColor, TextFormatFlags.VerticalCenter);
            }
            if (node.Text.Contains('.'))
            {
                bounds.Width = (int)e.Graphics.ClipBounds.Right - bounds.X;
                TextRenderer.DrawText(e.Graphics, "?", new Font(font, FontStyle.Bold), 
                    new Rectangle(e.Bounds.Right, bounds.Y, bounds.Width, bounds.Height), treeView.ForeColor,
                    TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.VerticalCenter);
                color = Color.FromArgb(64, 64, 64);
            }
            TextRenderer.DrawText(e.Graphics, node.Text, font, bounds, color,
                TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.VerticalCenter);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
                Hide();

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Filling object lists

        struct ObjectRef
        {
            public string Key;
            public string Value;
        }

        public void FillObjects()
        {
            objectsTreeView.Nodes.Clear();

            List<ObjectRef> objects = new List<ObjectRef>();
            foreach (var className in data.ClassNames.OrderBy(a => a))
            {
                if (className.EndsWith("CacheData"))
                    continue;
                objects.AddRange(data.ObjectNames(className).Select(n => new ObjectRef { Key = className, Value = n }));
            }

            AddItems(null, objects);

            void AddItems(TreeNode node, IEnumerable<ObjectRef> items, int prefix = 0)
            {
                var children = (node?.Nodes ?? objectsTreeView.Nodes);

                var folders = items
                    .Where(n => n.Value.AsSpan(prefix).Contains(Path.DirectorySeparatorChar))
                    .Select(n => n.Value[prefix..n.Value.IndexOf(Path.DirectorySeparatorChar, prefix)])
                    .Distinct()
                    .OrderBy(n => n);
                foreach (var folder in folders)
                {
                    var child = children.Add(folder);
                    var subprefix = items.First().Value.Substring(0, prefix) + folder + Path.DirectorySeparatorChar;
                    AddItems(child, items.Where(n => n.Value.StartsWith(subprefix)).ToArray(), subprefix.Length);
                }

                var local = items
                    .Where(n => n.Value.AsSpan(prefix + 1)
                    .Contains(Path.DirectorySeparatorChar) == false);
                foreach (var item in local)
                {
                    var child = children.Add($"{item.Key}.{item.Value}", Path.GetFileName(item.Value));
                    child.ImageIndex = child.SelectedImageIndex = 1;
                }

                if (node != null)
                    node.ImageIndex = node.SelectedImageIndex = 0;
            }
        }

        private void ObjectsTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            infoTreeView.BeginUpdate();
            infoTreeView.Nodes.Clear();
            var node = e.Node;
            string prefix;

            if (node != null && node.Name.Contains('.'))
            {
                var dotIndex = node.Name.IndexOf('.');
                var className = node.Name.Substring(0, dotIndex);
                var objectName = node.Name.Substring(dotIndex + 1);
                var view = data.UnPack(className, objectName);
                if (view != null)
                {
                    prefix = $"{className}.";
                    AddItems(null, view.Properties);
                }
            }

            string PropertyName(PackedData.Descriptor descriptor)
            {
                var name = descriptor.ToString().Replace("#", prefix);
                if (PropertyNames.TryGetValue(name, out string fullName))
                    return fullName;

                int replacements = 0;
                while (replacements < 100)
                {
                    bool any = false;
                    foreach (var pair in PropertyCuts)
                    {
                        if (name.StartsWith(pair.Key))
                        {
                            name = pair.Value + name.Substring(pair.Key.Length);
                            any = true;
                            break;
                        }
                    }
                    if (!any)
                        break;
                    if (PropertyNames.TryGetValue(name, out string fullName2))
                        return fullName2;
                    replacements++;
                }
                return name;
            }

            string AddItems(TreeNode node, PackedData.Property[] items)
            {
                string lastValue = null;
                string keyValue = null;
                string altValue = null;

                foreach (var item in items)
                {
                    var parent = (node?.Nodes ?? infoTreeView.Nodes).Add(PropertyName(item.Descriptor));
                    if (item.Value is PackedData.Property[][] array)
                    {
                        for (int n = 0; n < array.Length; n++)
                        {
                            var child = parent.Nodes.Add($"{n+1:N0}");
                            child.Tag = AddItems(child, array[n]);
                        }
                    }
                    else if (item.Value is PackedData.Property[] children)
                    {
                        lastValue = AddItems(parent, children);
                    }
                    else if (item.Value is PackedData.Property prop)
                    {
                        lastValue = AddItems(parent, new PackedData.Property[] { prop });
                    }
                    else if (item.Value is Array valueArray)
                    {
                        for (int n = 0; n < valueArray.Length; n++)
                        {
                            var child = parent.Nodes.Add($"{valueArray.GetValue(n)}");
                            child.ImageIndex = child.SelectedImageIndex = 1;
                        }
                    }
                    else
                    {
                        parent.ImageIndex = parent.SelectedImageIndex = 1;
                        
                        var value = item.Value.ToString();
                        if (item.Value is string str && str == "")
                            value = "(empty)";
                        
                        parent.Tag = value;
                        lastValue = value;
                        if (parent.Text == "Key" || parent.Text == "Name")
                            keyValue = value;
                        if (parent.Text == "Text" || parent.Text == "Texture")
                            altValue = value;
                    }
                }

                if (items.Length == 1 && node != null && lastValue != null)
                {
                    node.ImageIndex = node.SelectedImageIndex = 1;
                    node.Nodes.Clear();
                    node.Tag = lastValue;
                    return lastValue;
                }
                return keyValue ?? altValue;
            }

            infoTreeView.EndUpdate();
            Cursor.Current = Cursors.Default;
        }

        #endregion
    }
}
