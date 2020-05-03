using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SOR4Explorer
{
    /// <summary>
    /// Parses a texture file list from the SOR4 folder. The texture list is a binary database with
    /// the full path, offset and compressed length of every texture inside the related data file.
    /// </summary>
    class TextureList : IEnumerable<TextureInfo>
    {
        public object this[int index] => items[index];
        public int Length => items.Count;
        public IEnumerator<TextureInfo> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

        private readonly List<TextureInfo> items = new List<TextureInfo>();

        public void Add(TextureInfo item)
        {
            items.Add(item);
        }

        public TextureList(string filename, string datafile)
        {
            var file = File.OpenRead(filename);
            var reader = new BinaryReader(file, Encoding.Unicode);
            try
            {
                while (true)
                {
                    items.Add(new TextureInfo()
                    {
                        name = reader.ReadString().Replace('/', Path.DirectorySeparatorChar),
                        offset = reader.ReadUInt32(),
                        flags = reader.ReadUInt32(),
                        length = reader.ReadUInt32(),
                        datafile = datafile
                    });
                }
            }
            catch (EndOfStreamException)
            {
            }
        }
    }
}
