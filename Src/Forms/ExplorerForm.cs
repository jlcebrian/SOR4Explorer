using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SOR4Explorer
{
    public partial class ExplorerForm : Form
    {
        private readonly TextureLibrary library = new TextureLibrary();
        private readonly Timer timer;
        
        private Image folderIcon;
        private Image folderIconSmall;

        #region Initialization and components

        private SplitContainer splitContainer;
        private BufferedTreeView folderTreeView;
        private BufferedListView imageListView;
        private Panel dragInstructions;
        private Label instructionsLabel;
        private StatusStrip statusBar;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;

        public ExplorerForm()
        {
            InitializeComponents();

            if (Settings.InstallationPath != null)
                LoadTextureLists(Settings.InstallationPath);

            timer = new Timer { Interval = 100 };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        void InitializeComponents()
        {
            SuspendLayout();

            // Load program icon and other images
            var assembly = typeof(Program).Assembly;
            Icon = new Icon(assembly.GetManifestResourceStream("SOR4Explorer.Images.SOR4Explorer.ico"));
            folderIcon = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.FolderIcon.png"));
            folderIconSmall = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.FolderIconSmall.png"));

            //
            // statusBar
            //
            statusBar = new StatusStrip()
            {
                Dock = DockStyle.Bottom,
                Size = new Size(400, 40),
                SizingGrip = true,
                Stretch = true
            };
            statusBar.Items.Add(statusLabel = new ToolStripStatusLabel() { Spring = true, Text = "Ready", TextAlign = ContentAlignment.MiddleLeft });
            statusBar.Items.Add(progressBar = new ToolStripProgressBar());
            progressBar.Visible = false;

            // 
            // splitContainer
            // 
            splitContainer = new SplitContainer
            {
                BackColor = SystemColors.Control,
                Dock = DockStyle.Fill,
                Location = new Point(0, 0),
                Name = "splitContainer",
                FixedPanel = FixedPanel.Panel1,
                Size = new Size(1422, 1006),
                SplitterDistance = 450,
                TabIndex = 0,
                Visible = false,
            };
            splitContainer.BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            splitContainer.Panel1.DockPadding.All = 9;

            // 
            // folderTreeView
            // 
            folderTreeView = new BufferedTreeView
            {
                BackColor = SystemColors.Control,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 9.0F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0))),
                Location = new Point(0, 0),
                Name = "folderTreeView",
                Size = new Size(474, 1006),
                TabIndex = 0,
                ItemHeight = 40,
                FullRowSelect = true,
                HideSelection = false,
                DrawMode = TreeViewDrawMode.OwnerDrawText,
                ImageList = new ImageList
                {
                    ImageSize = new Size(32, 20),
                    ColorDepth = ColorDepth.Depth32Bit
                }
            };
            folderTreeView.ImageList.Images.Add(folderIconSmall);
            folderTreeView.AfterSelect += FolderTreeView_AfterSelect;
            folderTreeView.NodeMouseClick += FolderTreeView_MouseClick;
            folderTreeView.DrawNode += FolderTreeView_DrawNode;
            splitContainer.Panel1.Controls.Add(folderTreeView);

            // 
            // imageListView
            // 
            imageListView = new BufferedListView
            {
                BackColor = SystemColors.ControlDark,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Location = new Point(0, 0),
                Name = "imageListView",
                Size = new Size(944, 1006),
                TabIndex = 0,
                View = View.LargeIcon,
                LargeImageList = new ImageList
                {
                    ImageSize = new Size(200, 200),
                    ColorDepth = ColorDepth.Depth32Bit
                }
            };
            imageListView.SelectedIndexChanged += ImageListView_SelectedIndexChanged;
            imageListView.DoubleClick += ImageListView_DoubleClick;
            imageListView.MouseClick += ImageListView_MouseClick;
            imageListView.ItemDrag += ImageListView_ItemDrag;
            splitContainer.Panel2.Controls.Add(imageListView);

            //
            // DragInstructions
            //
            dragInstructions = new Panel() { Dock = DockStyle.Fill };
            instructionsLabel = new Label()
            {
                Dock = DockStyle.Fill,
                Font = new Font("Tahoma", 20),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            instructionsLabel.Paint += InstructionsLabel_Paint;
            dragInstructions.Controls.Add(instructionsLabel);

            // 
            // ExplorerForm
            // 
            AutoScaleDimensions = new SizeF(12F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1522, 1006);
            Controls.Add(splitContainer);
            Controls.Add(statusBar);
            Controls.Add(dragInstructions);
            Name = "ExplorerForm";
            Text = "SOR4 Explorer";
            DoubleBuffered = true;
            AllowDrop = true;

            DragDrop += ExplorerForm_DragDrop;
            DragEnter += ExplorerForm_DragEnter;

            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            splitContainer.EndInit();
            splitContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        #region Events

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.E) || keyData  == (Keys.Control | Keys.A))
            {
                foreach (ListViewItem item in imageListView.Items)
                    item.Selected = true;
                imageListView.Focus();
            }
            if (keyData == Keys.Escape)
                Close();
            if (keyData == (Keys.Control | Keys.W))
            {
                if (dragInstructions.Visible == false && MessageBox.Show(
                    this,
                    "You are going to close the current library.\nAre you sure?",
                    "Close library",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                    ) == DialogResult.Yes)
                {
                    library.Clear();
                    imageListView.Clear();
                    folderTreeView.Nodes.Clear();
                    splitContainer.Visible = false;
                    statusBar.Visible = false;
                    dragInstructions.Visible = true;
                    Settings.InstallationPath = null;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InstructionsLabel_Paint(object sender, PaintEventArgs e)
        {
            var text = "Drag here your\nStreets of Rage 4\ninstallation folder";
            var lines = text.Split('\n');
            var font = instructionsLabel.Font;
            var boldFont = new Font(font, FontStyle.Bold);
            Graphics g = e.Graphics;
            Brush brush = new SolidBrush(Color.Black);
            float lineSpacing = 1.25f;

            SizeF size = g.MeasureString(text, font);
            var lineHeight = g.MeasureString(lines[0], font).Height;
            size = new SizeF(size.Width, size.Height + lineHeight * (lineSpacing - 1) * (lines.Length - 1));

            var greyBrush = new SolidBrush(Color.FromArgb(128, 128, 128));
            var greyPen = new Pen(greyBrush, 8) { DashPattern = new float[] { 4.0f, 2.0f } };
            var innerRect = new Rectangle(
                new Point((int)(instructionsLabel.Width / 2 - size.Width / 2), (int)(instructionsLabel.Height / 2 - size.Height / 2)),
                new Size((int)size.Width, (int)size.Height));
            innerRect.Inflate(80, 40);
            innerRect.Offset(0, 10);
            g.DrawRoundedRectangle(greyPen, innerRect, 30);

            float y = instructionsLabel.Height / 2 - size.Height / 2;
            for (int i = 0; i < lines.Length; ++i)
            {
                var lineFont = i == 1 ? boldFont : font;
                SizeF lineSize = g.MeasureString(lines[i], lineFont);
                g.DrawString(lines[i], lineFont, brush, new PointF(instructionsLabel.Width / 2 - lineSize.Width / 2, y));
                y += lineSize.Height * lineSpacing;
            }
        }

        private void ExplorerForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("SOR4Explorer"))
                return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = ((string[])e.Data.GetData(DataFormats.FileDrop));
                if (paths.Length == 1 && Directory.Exists(paths[0]))
                    e.Effect = DragDropEffects.Link;
            }
        }

        private void ExplorerForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = ((string[])e.Data.GetData(DataFormats.FileDrop));
                if (paths.Length == 1 && Directory.Exists(paths[0]))
                {
                    if (CheckInstallation(paths[0]))
                    {
                        imageListView.Clear();
                        folderTreeView.Nodes.Clear();
                        LoadTextureLists(paths[0]);
                    }
                }
            }
        }

        private void ImageListView_ItemDrag(object sender, ItemDragEventArgs ev)
        {
            if (ev.Button == MouseButtons.Left)
            {
                var images = imageListView.SelectedItems.Cast<ListViewItem>().Where(n => n.Tag is TextureInfo).Select(n => ((TextureInfo)n.Tag)).ToArray();
                if (images.Length > 0)
                    imageListView.DoDragDrop(new DraggableImages(library, images), DragDropEffects.Copy);
            }
        }

        private void ImageListView_DoubleClick(object sender, EventArgs e)
        {
            if (imageListView.SelectedItems.Count == 1)
            {
                var item = imageListView.SelectedItems[0];
                if (item.Tag is string path)
                {
                    var node = folderTreeView.SelectedNode;
                    var child = node.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Name == path);
                    if (child != null)
                        folderTreeView.SelectedNode = child;
                }
                else if (item.Tag is TextureInfo info)
                {
                    var image = library.LoadTexture(info);
                    new ImagePreviewForm(image, info.name).Show();
                }
            }
        }

        private void ImageListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (imageListView.SelectedItems.Count == 1)
                {
                    switch (imageListView.SelectedItems[0].Tag)
                    {
                        case TextureInfo info:
                            ContextMenu.FromImage(library, info).Show(imageListView, e.Location);
                            break;
                        case string path:
                            var images = library.GetAllTextures(path);
                            ContextMenu.FromImages(library, images, true, SaveProgress()).Show(imageListView, e.Location);
                            break;
                    }
                }
                else if (imageListView.SelectedItems.Count > 1)
                {
                    var images = imageListView.SelectedItems.Cast<ListViewItem>().Select(n => (TextureInfo)n.Tag).ToList();
                    var contextMenu = ContextMenu.FromImages(library, images, progress: SaveProgress());
                    contextMenu.Show(imageListView, e.Location);
                }
            }
        }

        private Progress<ImageOpProgress> SaveProgress()
        {
            return new Progress<ImageOpProgress>(t =>
            {
                if (statusBar == null || statusBar.Items == null || statusBar.Items.Count < 1)
                    return;

                progressBar.Maximum = t.count;
                progressBar.Value = t.processed;
                
                if (t.processed == t.count)
                {
                    statusLabel.Text = $"{t.count} images saved";
                    progressBar.Visible = false;
                }
                else
                {
                    statusLabel.Text = $"Saving image {t.processed}/{t.count}";
                }
            });
        }

        private void FolderTreeView_MouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Node != null)
            {
                folderTreeView.SelectedNode = e.Node;
                var folder = e.Node.Name;
                var images = library.GetAllTextures(folder);
                progressBar.Maximum = images.Count;
                progressBar.Value = 0;
                progressBar.Visible = true;
                ContextMenu.FromImages(library, images, true, SaveProgress()).Show(folderTreeView, e.Location);
            }
        }

        private void FolderTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            var node = e.Node;
            var treeView = e.Node.TreeView;
            Font treeFont = node.NodeFont ?? treeView.Font;
            var color = treeView.SelectedNode == node ? SystemColors.HighlightText : treeView.ForeColor;
            if (treeView.SelectedNode == node)
                e.Graphics.FillRectangle(treeView.Focused ? SystemBrushes.Highlight: Brushes.Gray, e.Bounds);
            TextRenderer.DrawText(e.Graphics, node.Text, treeFont, e.Bounds, color, 
                TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.VerticalCenter);
        }

        private void FolderTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var path = folderTreeView.SelectedNode.Name;
            FillImageList(path);
            statusLabel.Text = $"{(path == "" ? "Root":path)} folder ({library.Count(path)} images in tree)";
        }

        private void ImageListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            int count = imageListView.SelectedItems.Count;
            if (count > 0)
                statusLabel.Text = $"{count} images selected";
            else
                statusLabel.Text = " ";
        }

        #endregion

        #region Filling the folder tree

        public bool CheckInstallation(string installationPath)
        {
            string dataFolder = Path.Combine(installationPath, "data");
            string texturesFile = Path.Combine(dataFolder, "textures");
            string tableFile = Path.Combine(dataFolder, "texture_table");
            if (Directory.Exists(dataFolder) == false || File.Exists(texturesFile) == false || File.Exists(tableFile) == false)
            {
                BringToFront();
                MessageBox.Show(
                    "Please select a valid Streets of Rage 4 installation folder",
                    "Data folder not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
                return false;
            }
            return true;
        }

        public void LoadTextureLists(string installationPath)
        {
            if (library.Load(installationPath) == false)
            {
                MessageBox.Show(
                    "Please select a valid Streets of Rage 4 installation folder",
                    "Data folder invalid",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
                dragInstructions.Visible = true;
                splitContainer.Visible = false;
                statusBar.Visible = false;
                return;
            }

            var folderNodes = new Dictionary<string, TreeNode> {
                [""] = folderTreeView.Nodes.Add("", "/")
            };
            foreach (var list in library.Lists.Values)
            {
                foreach (var item in list)
                {
                    var path = Path.GetDirectoryName(item.name);
                    if (!folderNodes.ContainsKey(path) && path != "")
                        AddPathToTree(path);
                }
            }
            if (folderNodes.Count > 1)
            {
                folderTreeView.Nodes[0].Expand();
                foreach (TreeNode node in folderTreeView.Nodes[0].Nodes)
                    node.Expand();
                dragInstructions.Visible = false;
                splitContainer.Visible = true;
                statusBar.Visible = true;
                Settings.InstallationPath = installationPath;
            }
            else
            {
                dragInstructions.Visible = true;
                splitContainer.Visible = false;
                statusBar.Visible = false;
            }
            Activate();

            TreeNode AddPathToTree(string folder)
            {
                var path = Path.GetDirectoryName(folder);
                var name = Path.GetFileName(folder);
                var node = folderNodes.TryGetValue(path, out TreeNode parent) ?
                    parent.Nodes.Add(folder, name) :
                    AddPathToTree(path).Nodes.Add(folder, name);
                node.ImageIndex = 0;
                folderNodes[folder] = node;
                return node;
            }
        }

        #endregion

        #region Filling the folder view

        private void FillImageList(string path)
        {
            imageListView.LargeImageList.Images.Clear();
            imageListView.LargeImageList.Images.Add(folderIcon);
            imageListView.Clear();
            loadOps.Clear();

            foreach (var subfolder in library.GetSubfolders(path))
            {
                var subpath = Path.Combine(path, subfolder);
                var listItem = imageListView.Items.Add($"{subfolder}\n({library.Count(subpath)} textures)");
                listItem.Tag = subpath;
                listItem.ImageIndex = 0;
            }

            foreach (var list in library.Lists.Values)
            {
                foreach (var info in list)
                {
                    if (Path.GetDirectoryName(info.name) == path)
                    {
                        var filename = Path.GetFileName(info.name);
                        var listItem = imageListView.Items.Add(filename);
                        listItem.Tag = info;
                        loadOps.Enqueue(new ImageToLoad { listItem = listItem, info = info });
                    }
                }
            }
        }

        #endregion

        #region Image loading

        private struct ImageToLoad
        {
            public ListViewItem listItem;
            public TextureInfo info;
        }
        private readonly Queue<ImageToLoad> loadOps = new Queue<ImageToLoad>();

        struct Result
        {
            public Bitmap image;
            public ListViewItem listItem;
        }
        private readonly List<Task<Result>> currentTask = new List<Task<Result>>();

        private void AddBackgroundLoadTask(ImageToLoad op)
        {
            var data = library.LoadTextureData(op.info);
            var task = Task.Run(() => {
                var image = TextureLoader.Load(op.info, data);
                Console.WriteLine($"Image {op.info.name} loaded");
                return new Result { image = image, listItem = op.listItem };
            });
            currentTask.Add(task);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            int emptySlots = Environment.ProcessorCount;

            if (currentTask.Count > 0)
            {
                for (int n = 0; n < currentTask.Count; n++)
                {
                    var task = currentTask[n];
                    if (task.IsCompleted)
                    {
                        var result = task.Result;
                        var image = result.image;
                        var listItem = result.listItem;
                        if (image != null && listItem != null && listItem.ImageList != null)
                        {
                            listItem.Text += $"\n({image.Width}x{image.Height})";
                            listItem.ImageList.Images.Add(TextureLoader.ScaledImage(image));
                            listItem.ImageIndex = listItem.ImageList.Images.Count - 1;
                        }
                        currentTask.RemoveAt(n--);
                    }
                    else
                    {
                        emptySlots--;
                    }
                }
            }

            for (int n = 0; n < emptySlots && loadOps.Count > 0; n++)
                AddBackgroundLoadTask(loadOps.Dequeue());
        }

        #endregion
    }
}