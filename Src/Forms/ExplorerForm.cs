using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly DataLibrary data = new DataLibrary();
        private readonly Timer timer;
        private readonly DataViewForm dataViewForm;
        private LocalizationData localization;

        #region Initialization and components

        private Panel appContainer;
        private SplitContainer splitContainer;
        private BufferedTreeView folderTreeView;
        private BufferedListView imageListView;
        private Panel dragInstructions;
        private Label instructionsLabel;
        private StatusStrip statusBar;
        private BlackToolStrip toolStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        private ToolStripLabel changesLabel;
        private ToolStripButton applyButton;
        private ToolStripButton discardButton;

        public ExplorerForm()
        {
            dataViewForm = new DataViewForm(data);
            InitializeComponents();

            library.OnTextureChangeDiscarded += Library_OnTextureChangeDiscarded;
            library.OnTextureChanged += Library_OnTextureChangesAdded;

            if (Settings.InstallationPath != null)
                LoadTextureLists(Settings.InstallationPath);

            timer = new Timer { Interval = 100 };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        void InitializeComponents()
        {
            SuspendLayout();

            //
            // appContainer
            //
            appContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Visible = false,
            };
            appContainer.SuspendLayout();

            //
            // statusBar
            //
            statusBar = new StatusStrip()
            {
                Dock = DockStyle.Bottom,
                Size = new Size(400, 32),
                SizingGrip = true,
                Stretch = true,
                AutoSize = false
            };
            statusBar.Items.Add(statusLabel = new ToolStripStatusLabel() { 
                Spring = true, 
                Text = "Ready", 
                TextAlign = ContentAlignment.MiddleLeft 
            });

            //
            // toolStrip
            //
            toolStrip = new BlackToolStrip();
            var mainMenuButton = toolStrip.AddMenuItem(Program.BarsImage);
            mainMenuButton.DropDownOpening += (sender, args) =>
            {
                mainMenuButton.DropDownItems.Clear();
                mainMenuButton.DropDownItems.AddRange(MainMenu());
            };
            toolStrip.NextAlignment = ToolStripItemAlignment.Right;
            progressBar = toolStrip.AddProgressBar();
            discardButton = toolStrip.AddButton(Program.TrashImage, "Discard", DiscardChanges);
            applyButton = toolStrip.AddButton(Program.SaveImage, "Apply", ApplyChanges);
            changesLabel = toolStrip.AddLabel();
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
            folderTreeView.ImageList.Images.Add(Program.FolderIconSmall);
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
            appContainer.Controls.Add(splitContainer);
            appContainer.Controls.Add(statusBar);
            appContainer.Controls.Add(toolStrip);
            Controls.Add(appContainer);
            Controls.Add(dragInstructions);
            Name = "ExplorerForm";
            Text = "SOR4 Explorer";
            DoubleBuffered = true;
            AllowDrop = true;
            Icon = Program.Icon;

            FormClosing += ExplorerForm_FormClosing;
            DragDrop += ExplorerForm_DragDrop;
            DragEnter += ExplorerForm_DragEnter;

            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            splitContainer.EndInit();
            splitContainer.ResumeLayout(false);
            appContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        #region Events

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
                ApplyChanges();

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
                    appContainer.Visible = false;
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

        private void ExplorerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (library.ImageChanges.Count > 0)
            {
                if (MessageBox.Show("Unsaved changes will be lost.\nAre you sure?", "Close",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    e.Cancel = true;
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
                else
                    e.Effect = DragDropEffects.Move;
            }
        }

        private void ExplorerForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = ((string[])e.Data.GetData(DataFormats.FileDrop));
                if (paths.Length == 1 && Directory.Exists(paths[0]) && IsInstallationFolder(paths[0]))
                {
                    imageListView.Clear();
                    folderTreeView.Nodes.Clear();
                    LoadTextureLists(paths[0]);
                    return;
                }
                if (instructionsLabel.Visible)
                {
                    BringToFront();
                    MessageBox.Show(
                        "Please select a valid Streets of Rage 4 installation folder",
                        "Data folder not found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                
                var root = folderTreeView.SelectedNode.Name;
                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        string name = Path.GetFileNameWithoutExtension(path);
                        string filename = Path.Combine(root, name);
                        if (library.Contains(filename) == false)
                        {
                            MessageBox.Show(
                                this,
                                $"{filename}\ndoes not exist in the textures library",
                                "Invalid texture replacement",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                MessageBoxDefaultButton.Button1);
                            break;
                        }

                        Bitmap image;
                        try
                        {
                            image = new Bitmap(path);
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(
                                this,
                                $"Unable to open image {path}\n{exception.Message}",
                                "Invalid image",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                MessageBoxDefaultButton.Button1);
                            return;
                        }
                        library.AddChange(filename, image);
                        UpdateChangesLabel();

                        foreach (ListViewItem item in imageListView.Items)
                        {
                            if (item.Tag is TextureInfo info && info.name == filename)
                            {
                                imageListView.LargeImageList.Images.Add(TextureLoader.ScaledImage(image));
                                item.ImageIndex = imageListView.LargeImageList.Images.Count - 1;
                                item.Font = new Font(item.Font, FontStyle.Bold);
                            }
                        }
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
                            var images = library.GetTextures(path);
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

        private void FolderTreeView_MouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Node != null)
            {
                folderTreeView.SelectedNode = e.Node;
                var folder = e.Node.Name;
                var images = library.GetTextures(folder);
                progressBar.Maximum = images.Count;
                progressBar.Value = 0;
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
            statusLabel.Text = $"{(path == "" ? "Root":path)} folder ({library.CountTextures(path)} images in tree)";
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

        public bool IsInstallationFolder(string installationPath)
        {
            string dataFolder = Path.Combine(installationPath, "data");
            string texturesFile = Path.Combine(dataFolder, "textures");
            string tableFile = Path.Combine(dataFolder, "texture_table");
            return Directory.Exists(dataFolder) && File.Exists(texturesFile) && File.Exists(tableFile);
        }

        public void LoadTextureLists(string installationPath)
        {
            if (library.Load(installationPath) == false)
            {
                UpdateChangesLabel();
                MessageBox.Show(
                    "Please select a valid Streets of Rage 4 installation folder",
                    "Data folder invalid",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
                dragInstructions.Visible = true;
                appContainer.Visible = false;
                return;
            }

            data.Load(installationPath);
            localization = data.Unserialize<LocalizationData>("localization");

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
                appContainer.Visible = true;
                Settings.InstallationPath = installationPath;
            }
            else
            {
                dragInstructions.Visible = true;
                appContainer.Visible = false;
            }

            Activate();
            UpdateChangesLabel();

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
            imageListView.BeginUpdate();
            imageListView.LargeImageList.Images.Clear();
            imageListView.LargeImageList.Images.Add(Program.FolderIcon);
            imageListView.Clear();
            loadOps.Clear();

            foreach (var subfolder in library.GetSubfolders(path))
            {
                var subpath = Path.Combine(path, subfolder);
                var listItem = imageListView.Items.Add($"{subfolder}\n({library.CountTextures(subpath)} textures)");
                listItem.Tag = subpath;
                listItem.ImageIndex = 0;
            }

            List<TextureInfo> images = new List<TextureInfo>();
            foreach (var list in library.Lists.Values)
                images.AddRange(list.Where(n => n.changed == false && Path.GetDirectoryName(n.name) == path));
            images.Sort((a, b) => a.name.CompareTo(b.name));

            foreach (var info in images)
            {
                if (Path.GetDirectoryName(info.name) == path)
                {
                    var filename = Path.GetFileName(info.name);
                    var listItem = imageListView.Items.Add(filename);
                    listItem.Tag = info;
                    if (info.datafile == null)
                        listItem.Font = new Font(listItem.Font, FontStyle.Bold);
                    loadOps.Enqueue(new ImageToLoad { listItem = listItem, info = info });
                }
            }
            imageListView.EndUpdate();
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
                var image = data == null ? library.LoadTexture(op.info) : TextureLoader.Load(op.info, data);
                Console.WriteLine($"Image {op.info.name} loaded");
                return new Result { image = image, listItem = op.listItem };
            });
            currentTask.Add(task);
        }
        
        private void Library_OnTextureChangesAdded(TextureInfo obj, Bitmap image)
        {
            imageListView.LargeImageList.Images.Add(TextureLoader.ScaledImage(image));

            foreach (ListViewItem item in imageListView.Items)
            {
                if (item.Tag is TextureInfo info && obj.name == info.name)
                {
                    item.Font = new Font(item.Font, FontStyle.Bold);
                    item.ImageIndex = imageListView.LargeImageList.Images.Count - 1;
                }
            }
            UpdateChangesLabel();
        }

        private void Library_OnTextureChangeDiscarded(TextureInfo obj)
        {
            foreach (ListViewItem item in imageListView.Items)
            {
                if (item.Tag is TextureInfo info && obj.name == info.name)
                {
                    item.Font = new Font(item.Font, FontStyle.Regular);
                    loadOps.Enqueue(new ImageToLoad() { listItem = item, info = obj });
                }
            }
            UpdateChangesLabel();
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
                            var suffix = $"\n({image.Width}x{image.Height})";
                            if (listItem.Text.EndsWith(suffix) == false)
                                listItem.Text += suffix;
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

        #region Control logic

        void ApplyChanges()
        {
            if (library.ImageChanges.Count > 0)
            {
                changesLabel.Text = "Saving changes...";
                Cursor.Current = Cursors.WaitCursor;
                Refresh();
                library.SaveChanges();
                Cursor.Current = Cursors.Default;
            }
            UpdateChangesLabel();
        }

        void DiscardChanges()
        {
            if (library.ImageChanges.Count > 0)
            {
                if (dragInstructions.Visible == false && MessageBox.Show(
                    this,
                    $"You are going to discard {library.ImageChanges.Count:N0} changes.\nAre you sure?",
                    "Close library",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                    ) == DialogResult.Yes)
                {
                    library.DiscardChanges();
                    UpdateChangesLabel();
                }
            }
        }

        void UpdateChangesLabel()
        {
            if (progressBar.Visible)
            {
                applyButton.Visible = false;
                discardButton.Visible = false;
                return;
            }

            var count = library.ImageChanges.Count;
            if (count == 0)
            {
                applyButton.Visible = false;
                discardButton.Visible = false;
                changesLabel.Text = "Drag replacements over existing textures \u2935";
            }
            else
            {
                changesLabel.Text = $"{count:N0} texture{(count > 1 ? "s":"")} changed";
                applyButton.Visible = true;
                discardButton.Visible = true;
            }
        }

        public ToolStripItem[] MainMenu()
        {
            return new ToolStripItem[] {
                new ToolStripMenuItem("Update game files", null, (sender, ev) => ApplyChanges()) { Enabled = applyButton.Visible },
                new ToolStripMenuItem("Discard changed textures", null, (sender, ev) => DiscardChanges()) { Enabled = applyButton.Visible },
                new ToolStripSeparator(),
                new ToolStripMenuItem("View game data", null, (sender, ev) => {
                    dataViewForm.FillObjects();
                    dataViewForm.Show();
                }) 
            };
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
                    progressBar.Visible = false;
                    FillImageList(folderTreeView.SelectedNode.Name);
                    UpdateChangesLabel();
                }
                else
                {
                    changesLabel.Text = $"Saving image {t.processed}/{t.count}";
                    progressBar.Visible = true;
                }
            });
        }
        #endregion
    }
}