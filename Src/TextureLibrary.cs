using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SOR4Explorer
{
    class TextureLibrary
    {

        public string RootPath { get; set; }

        public event Action<TextureInfo> OnTextureChangeDiscarded;
        public event Action<TextureInfo, Bitmap> OnTextureChanged;

        public readonly Dictionary<string, TextureList> Lists = new Dictionary<string, TextureList>();
        public readonly Dictionary<string, FileStream> DataFiles = new Dictionary<string, FileStream>();
        public readonly HashSet<string> Folders = new HashSet<string>();
        public readonly Dictionary<string, ImageChange> ImageChanges = new Dictionary<string, ImageChange>();

        private readonly Dictionary<string, int> ImageCountCache = new Dictionary<string, int>();

        private string dataFolder;
        private int nonOriginalCount;

        public int NonOriginalCount => nonOriginalCount;

        public void AddChange (string path, Bitmap image)
        {
            foreach (var list in Lists.Values)
            {
                var item = list.FirstOrDefault(n => n.name == path);
                if (item != null)
                {
                    ImageChanges[path] = new ImageChange()
                    {
                        datafile = item.datafile,
                        image = image,
                        path = path
                    };
                    item.changed = true;
                    OnTextureChanged?.Invoke(item, image);
                    return;
                }
            }

            Console.Write($"Warning: attempt to change file {path} which is not in the library");
        }

        public void DiscardChange(TextureInfo info)
        {
            if (ImageChanges.ContainsKey(info.name))
            {
                ImageChanges.Remove(info.name);
                info.changed = false;
                OnTextureChangeDiscarded?.Invoke(info);
            }
        }

        public void DiscardChanges()
        {
            foreach (var list in Lists.Values)
            {
                foreach (var item in list)
                {
                    item.changed = false;
                    OnTextureChangeDiscarded?.Invoke(item);
                }
            }
            ImageChanges.Clear();
        }

        public void Clear()
        {
            Lists.Clear();
            DataFiles.Clear();
            Folders.Clear();
            ImageCountCache.Clear();
            nonOriginalCount = 0;
            RootPath = "";
        }

        public void CloseDatafiles()
        {
            foreach (var key in DataFiles.Keys.ToArray())
            {
                if (DataFiles[key] != null)
                {
                    DataFiles[key].Close();
                    DataFiles[key] = null;
                }
            }
        }

        public bool GameFilesChanged()
        {
            foreach (var texturesFilePath in DataFiles.Keys)
            {
                long size = Settings.GetFileSize(texturesFilePath);

                bool changed;
                if (DataFiles[texturesFilePath] != null)
                    changed = DataFiles[texturesFilePath].Length != size;
                else
                    changed = new FileInfo(texturesFilePath).Length != size;

                if (changed)
                    return true;
            }
            return false;                   
        }

        public void RestoreFromBackups()
        {
            CloseDatafiles();

            foreach (var texturesFilePath in DataFiles.Keys)
            {
                string tableFilePath = Path.Combine(Path.GetDirectoryName(texturesFilePath), Path.GetFileName(texturesFilePath).Replace("textures", "texture_table"));
                string backupTablePath = Path.Combine(Settings.FileName(Path.ChangeExtension(Path.GetFileName(tableFilePath), ".bak")));
                if (File.Exists(backupTablePath))
                {
                    File.Copy(backupTablePath, tableFilePath, true);
                    long size = Settings.GetFileSize(texturesFilePath);
                    if (size != 0)
                    {
                        using FileStream file = File.OpenWrite(texturesFilePath);
                        file.SetLength(size);

                        var list = Lists[texturesFilePath];
                        var originalList = new TextureList(tableFilePath, texturesFilePath);
                        foreach (var info in list)
                        {
                            if (info.offset >= size)
                            {
                                var previous = originalList.First(n => n.name == info.name);
                                info.offset = previous.offset;
                                info.length = previous.length;
                                OnTextureChangeDiscarded?.Invoke(info);
                            }
                        }
                    }
                }
            }

            nonOriginalCount = 0;
        }

        private void SaveBackups()
        {
            foreach (var dataFilePath in DataFiles.Keys)
            {
                string tableFilePath = Path.Combine(Path.GetDirectoryName(dataFilePath), Path.GetFileName(dataFilePath).Replace("textures", "texture_table"));
                string backupTablePath = Path.Combine(Settings.FileName(Path.ChangeExtension(Path.GetFileName(tableFilePath), ".bak")));
                if (!File.Exists(backupTablePath))
                {
                    File.Copy(tableFilePath, backupTablePath);
                    long size = new FileInfo(dataFilePath).Length;
                    Settings.SetFileSize(dataFilePath, size);
                }
            }
        }

        public void SaveChanges()
        {
            CloseDatafiles();

            // Replaces textures by adding them to the end of the original file. This
            // should make it possible to go back to the original library by truncating
            // the data file and restoring the original texture list.

            foreach (var entry in Lists)
            {
                var fileName = entry.Key.Replace("textures", "texture_table");
                var list = entry.Value;

                FileStream tableFile = File.Open(fileName, FileMode.Create);
                var tableWriter = new BinaryWriter(tableFile, Encoding.Unicode);
                foreach (var item in list)
                {
                    if (item.changed && ImageChanges.TryGetValue(item.name, out ImageChange change))
                    {
                        var compressedData = TextureLoader.Compress(change.image);
                        FileStream dataFileStream = File.OpenWrite(entry.Key);
                        dataFileStream.Seek(0, SeekOrigin.End);
                        item.offset = (uint)dataFileStream.Position;
                        item.length = (uint)compressedData.Length;
                        item.changed = false;
                        if (item.original)
                        {
                            item.original = false;
                            nonOriginalCount++;
                        }
                        dataFileStream.Write(compressedData);
                        dataFileStream.Close();
                    }

                    tableWriter.Write(item.name.Replace(Path.DirectorySeparatorChar, '/'));
                    tableWriter.Write((UInt32)item.offset);
                    tableWriter.Write((UInt32)0);
                    tableWriter.Write((UInt32)item.length);
                }
                tableWriter.Close();
                tableFile.Close();
            }

            ImageChanges.Clear();
        }

        public bool Load(string installationPath)
        {
            Clear();

            RootPath = installationPath;

            try
            {
                int fileIndex = 1;
                dataFolder = Path.Combine(installationPath, "data");

                string texturesFile = Path.Combine(dataFolder, "textures");
                string tableFile = Path.Combine(dataFolder, "texture_table");
                while (File.Exists(texturesFile) && File.Exists(tableFile))
                {
                    DataFiles[texturesFile] = File.OpenRead(texturesFile);
                    Lists[texturesFile] = new TextureList(tableFile, texturesFile);

                    foreach (var file in Lists[texturesFile])
                    {
                        AddFoldersInPath(file.name);
                        if (file.original == false)
                            nonOriginalCount++;
                    }

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

            SaveBackups();
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
            if (textureInfo.datafile == null)
                return null;

            byte[] data = new byte[textureInfo.length];
            var datafile = DataFiles[textureInfo.datafile];
            if (datafile == null)
                datafile = DataFiles[textureInfo.datafile] = File.OpenRead(textureInfo.datafile);
            lock (datafile)    // Not needed?
            {
                datafile.Seek(textureInfo.offset, SeekOrigin.Begin);
                datafile.Read(data, 0, (int)textureInfo.length);
            }
            return data;
        }

        public Bitmap LoadTexture(TextureInfo textureInfo)
        {
            if (textureInfo.datafile == null)
                return ImageChanges.TryGetValue(textureInfo.name, out ImageChange change) ? change.image : null;

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

        public List<TextureInfo> GetTextures(string folder)
        {
            List<TextureInfo> result = new List<TextureInfo>();
            folder = NormalizePath(folder);
            foreach (var list in Lists.Values)
                result.AddRange(list.Where(item => item.name.StartsWith(folder) && !item.changed));
            return result;
        }

        public bool Contains(string path)
        {
            foreach (var list in Lists.Values)
            {
                if (list.Any(n => n.name == path))
                    return true;
            }
            return false;
        }

        public int CountTextures()
        {
            if (ImageCountCache.ContainsKey(""))
                return ImageCountCache[""];

            return ImageCountCache[""] = Lists.Values.Aggregate(0, (result, item) => result += item.Length);
        }

        public int CountTextures(string folder)
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

        public IEnumerable<string> GetSubfolders(string folder)
        {
            folder = NormalizePath(folder);
            return Folders.Where(n => n.StartsWith(folder))
                          .Select(n => n.Substring(folder.Length))
                          .Where(n => !n.Contains('\\'));
        }
    }
}
