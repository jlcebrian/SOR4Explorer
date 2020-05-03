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
        private static string GetCommonPath(IEnumerable<string> paths)
        {
            string path = paths.Aggregate((a, b) => a.Length > b.Length ? a : b);
            while (path != "" && paths.All(n => n.StartsWith(path)) == false)
                path = Path.GetDirectoryName(path).Replace('\\', '/');
            return path;
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

        public static void SaveTextures(TextureLibrary library, IEnumerable<TextureInfo> files, bool useBaseFolder = false, IProgress<float> progress = null)
        {
            var fbd = new FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.Desktop,
                UseDescriptionForTitle = true,
                Description = "Destination folder"
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string basePath = GetCommonPath(files.Select(n => n.name));
                if (useBaseFolder && basePath != null)
                    basePath = Path.GetDirectoryName(basePath);
                int savedCount = 0;
                int count = files.Count();
                Task.Run(() =>
                {
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    Parallel.ForEach(files,
                    new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount-1 },
                    info =>
                    {
                        try
                        {
                            var image = library.LoadTexture(info);
                            if (image != null)
                            {
                                var path = basePath?.Length > 0 ? Path.GetRelativePath(basePath, info.name) : info.name;
                                var destinationPath = Path.Combine(fbd.SelectedPath, path);
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                                image.Save(Path.ChangeExtension(destinationPath, ".png"), ImageFormat.Png);
                                Interlocked.Increment(ref savedCount);
                                if (progress != null)
                                    progress.Report((float)savedCount / count);
                            }
                        } 
                        catch(Exception)
                        {
                        }
                    });
                    if (savedCount < count)
                        progress.Report(1.0f);
                });
            }
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
            bool useBaseFolder = false, IProgress<float> progress = null)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add($"&Save {info.Count()} images as...", null, 
                (sender, ev) => SaveTextures(library, info, useBaseFolder, progress));
            return menu;
        }
    }
}
