using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SOR4Explorer
{
    static class ContextMenu
    {
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
