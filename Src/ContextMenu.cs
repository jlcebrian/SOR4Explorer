using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SOR4Explorer
{
    static class ContextMenu
    {
        private static void LoadTexture(TextureLibrary library, TextureInfo info)
        {
            OpenFileDialog ofd = new OpenFileDialog
            { 
                Filter = "Images|*.png;*.bmp;*.jpg",
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Bitmap bitmap;
                try
                {
                    bitmap = new Bitmap(ofd.FileName);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        $"Unable to open image {ofd.FileName}\n{exception.Message}",
                        "Invalid image",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1);
                    return;
                }
                library.AddChange(info.name, bitmap);
            }
        }

        private static void SaveTexture(string name, Bitmap image)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                FileName = Path.GetFileName(name) + ".png",
                Filter = "Images|*.png;*.bmp;*.jpg",
                OverwritePrompt = true
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ImageFormat format = Path.GetExtension(sfd.FileName) switch
                {
                    ".jpg" => ImageFormat.Jpeg,
                    ".bmp" => ImageFormat.Bmp,
                    _ => ImageFormat.Png
                };
                image.Save(sfd.FileName, format);
            }
        }

        public static void SaveTextures(TextureLibrary library, IEnumerable<TextureInfo> files, bool useBaseFolder = false, IProgress<ImageOpProgress> progress = null)
        {
            var fbd = new FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.Desktop,
                UseDescriptionForTitle = true,
                Description = "Destination folder"
            };
            if (fbd.ShowDialog() == DialogResult.OK)
                library.SaveTextures(fbd.SelectedPath, files, useBaseFolder, progress);
        }

        public static ContextMenuStrip FromImage(TextureLibrary library, TextureInfo info)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("&Replace with...", null, (sender, ev) => LoadTexture(library, info));
            if (library.ImageChanges.Any(n => n.Key == info.name))
                menu.Items.Add("Discard changes", null, (sender, ev) => library.DiscardChange(info));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("&Copy", null, (sender, ev) => Clipboard.SetImage(library.LoadTexture(info)));
            menu.Items.Add("&Save as...", null, (sender, ev) => SaveTexture(info.name, library.LoadTexture(info)));
            return menu;
        }

        public static ContextMenuStrip FromImage(string name, Bitmap image)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("&Copy", null, (sender, ev) => Clipboard.SetImage(image));
            menu.Items.Add("&Save as...", null, (sender, ev) => SaveTexture(name, image));
            return menu;
        }

        public static ContextMenuStrip FromImages(TextureLibrary library, IEnumerable<TextureInfo> info, 
            bool useBaseFolder = false, IProgress<ImageOpProgress> progress = null)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add($"&Save {info.Count()} images as...", null, 
                (sender, ev) => SaveTextures(library, info, useBaseFolder, progress));
            return menu;
        }
    }
}
