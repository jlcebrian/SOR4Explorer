using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SOR4Explorer
{
    struct ImageOpProgress
    {
        public int processed;
        public int count;
    };

    class TextureLibrary
    {
        public string RootPath { get; set; }
        public readonly Dictionary<string, TextureList> Lists = new Dictionary<string, TextureList>();
        public readonly Dictionary<string, FileStream> DataFiles = new Dictionary<string, FileStream>();
        public readonly HashSet<string> Folders = new HashSet<string>();

        private readonly Dictionary<string, int> ImageCountCache = new Dictionary<string, int>();

        public bool Load(string installationPath)
        {
            Clear();
            RootPath = installationPath;

            try
            {
                int fileIndex = 1;
                string dataFolder = Path.Combine(installationPath, "data");
                string texturesFile = Path.Combine(dataFolder, "textures");
                string tableFile = Path.Combine(dataFolder, "texture_table");
                while (File.Exists(texturesFile) && File.Exists(tableFile))
                {
                    DataFiles[texturesFile] = File.OpenRead(texturesFile);
                    Lists[texturesFile] = new TextureList(tableFile, texturesFile);

                    foreach (var file in Lists[texturesFile])
                        AddFoldersInPath(file.name);

                    fileIndex++;
                    texturesFile = Path.Combine(dataFolder, $"textures{fileIndex:D2}");
                    tableFile = Path.Combine(dataFolder, $"texture_table{fileIndex:D2}");
                }
            }
            catch (Exception)
            {
                Lists.Clear();
                DataFiles.Clear();
                return false;
            }
            return true;

            void AddFoldersInPath(string path)
            {
                var directory = Path.GetDirectoryName(path);
                if (directory != "")
                {
                    Folders.Add(directory);
                    AddFoldersInPath(directory);
                }
            }
        }

        public byte[] LoadTextureData(TextureInfo textureInfo)
        {
            byte[] data = new byte[textureInfo.length];

            var datafile = DataFiles[textureInfo.datafile];
            lock (datafile)    // Not needed?
            {
                datafile.Seek(textureInfo.offset, SeekOrigin.Begin);
                datafile.Read(data, 0, (int)textureInfo.length);
            }
            return data;
        }

        public Bitmap LoadTexture(TextureInfo textureInfo)
        {
            return TextureLoader.Load(textureInfo, LoadTextureData(textureInfo));
        }

        public void SaveTextures(string destination, IEnumerable<TextureInfo> files, bool useBaseFolder, IProgress<ImageOpProgress> progress)
        {
            string basePath = GetCommonPath(files.Select(n => n.name));
            if (useBaseFolder && basePath != null)
                basePath = Path.GetDirectoryName(basePath);
            Task.Run(() =>
            {
                var op = new ImageOpProgress() { count = files.Count() };
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                Parallel.ForEach(files,
                new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount - 1 },
                info =>
                {
                    try
                    {
                        var image = LoadTexture(info);
                        if (image != null)
                        {
                            var path = basePath?.Length > 0 ? Path.GetRelativePath(basePath, info.name) : info.name;
                            var destinationPath = Path.Combine(destination, path);
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            image.Save(Path.ChangeExtension(destinationPath, ".png"), ImageFormat.Png);
                        }
                        Interlocked.Increment(ref op.processed);
                        if (progress != null)
                            progress.Report(op);
                    }
                    catch (Exception)
                    {
                    }
                });
            });

            static string GetCommonPath(IEnumerable<string> paths)
            {
                string path = paths.Aggregate((a, b) => a.Length > b.Length ? a : b);
                while (path != "" && paths.All(n => n.StartsWith(path)) == false)
                    path = Path.GetDirectoryName(path);
                return path;
            }
        }

        public List<TextureInfo> GetAllTextures(string folder)
        {
            List<TextureInfo> result = new List<TextureInfo>();
            folder = NormalizePath(folder);
            foreach (var list in Lists.Values)
                result.AddRange(list.Where(item => item.name.StartsWith(folder)));
            return result;
        }

        public int Count()
        {
            if (ImageCountCache.ContainsKey(""))
                return ImageCountCache[""];

            return ImageCountCache[""] = Lists.Values.Aggregate(0, (result, item) => result += item.Length);
        }

        public int Count(string folder)
        {
            folder = NormalizePath(folder);
            if (ImageCountCache.ContainsKey(folder))
                return ImageCountCache[folder];
            return ImageCountCache[folder] = Lists.Values.Aggregate(0, (result, list) => result += list.Count(item => item.name.StartsWith(folder)));
        }

        private static string NormalizePath(string folder)
        {
            if (folder != "")
                folder = Path.TrimEndingDirectorySeparator(folder) + Path.DirectorySeparatorChar;
            return folder;
        }

        public void Clear()
        {
            Lists.Clear();
            DataFiles.Clear();
            Folders.Clear();
            ImageCountCache.Clear();
            RootPath = "";
        }

        public IEnumerable<string> GetSubfolders(string folder)
        {
            folder = NormalizePath(folder);
            return Folders.Where(n => n.StartsWith(folder))
                          .Select(n => n.Substring(folder.Length))
                          .Where(n => !n.Contains('\\'));
        }
    }
}
