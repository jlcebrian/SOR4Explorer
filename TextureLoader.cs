using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace SOR4Explorer
{
    /// <summary>
    /// This class contains the logic needed to ucompress textures from the data file.
    /// Textures are compressed by a headerless ZLib stream (using CLR) and then stored
    /// in XNB format. Although the XNB format is complex, fortunately all the SOR4 textures
    /// are stored very simply: they are all an uncompressed canonical ARGB32 format
    /// with no mipmaps or any other fancy features.
    /// </summary>
    static class TextureLoader
    {
        public static Bitmap Load(TextureInfo textureInfo, byte[] data)
        {
            var filename = Path.GetFileName(textureInfo.name);
            var output = new MemoryStream();
            output.Seek(0, SeekOrigin.Begin);
            output.SetLength(0);
            var stream = new DeflateStream(new MemoryStream(data, false), CompressionMode.Decompress);
            stream.CopyTo(output);
            var uncompressedSize = output.Position;
            output.Seek(0, SeekOrigin.Begin);
            var reader = new BinaryReader(output);

            // Check signature
            var signature = Encoding.UTF8.GetString(reader.ReadBytes(4));
            if (signature != "XNBw")
            {
                Console.WriteLine($"{filename} has an unknown signature: {signature}");
                return null;
            }

            // Check version number
            int versionHi = reader.ReadByte();
            int versionLo = reader.ReadByte();
            if (versionHi != 5 || versionLo != 0)
            {
                Console.WriteLine($"{filename} has an unknown version code: {versionHi}.{versionLo}");
                return null;
            }

            // Check file size
            uint fileSize = reader.ReadUInt32();
            if (fileSize != uncompressedSize)
            {
                Console.WriteLine($"{filename} has the wrong length {fileSize} encoded, should be {uncompressedSize}");
                return null;
            }

            // Check items count
            byte itemsCount = reader.ReadByte();
            if (itemsCount == 0)
            {
                Console.WriteLine($"{filename} is empty");
                return null;
            }
            if (itemsCount != 1)
            {
                Console.WriteLine($"{filename} contains more than one item");
            }

            // Check serialization class name
            string classname = reader.ReadString();
            if (classname != "Microsoft.Xna.Framework.Content.Texture2DReader")
            {
                Console.WriteLine($"{filename} uses an unknown class {classname}");
                return null;
            }
            _ = reader.ReadUInt32();            // Always 0
            _ = reader.ReadUInt16();            // Always 256

            // Read texture header, check format
            uint format = reader.ReadUInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            _ = reader.ReadUInt32();            // Level count (for mipmaps)
            _ = reader.ReadUInt32();            // Compression type (not supported)
            if (format != 0)
            {
                Console.WriteLine($"{filename} uses an unsupported format {format}");
                return null;
            }

            // Load texture data, convert Argb to Abgr for Bitmap()
            var image = new Bitmap(width, height);
            var bmpData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            IntPtr ptr = bmpData.Scan0;
            var line = new byte[width * 4];
            for (int y = 0; y < height; y++)
            {
                reader.Read(line, 0, width * 4);
                for (int x = 0; x < width * 4; x += 4)
                {
                    byte b = line[x + 0];
                    line[x + 0] = line[x + 2];
                    line[x + 2] = b;
                }
                Marshal.Copy(line, 0, ptr, width * 4);
                ptr += bmpData.Stride;
            }
            image.UnlockBits(bmpData);
            return image;
        }

        public static Bitmap ScaledImage(Bitmap image)
        {
            var scaled = new Bitmap(200, 200);
            using (Graphics g = Graphics.FromImage(scaled))
            {
                if (image.Width <= 200 && image.Height <= 200)
                {
                    g.DrawImage(image, 100 - image.Width / 2, 100 - image.Height / 2);
                }
                else if (image.Width >= image.Height)
                {
                    float scaledHeight = 200 * image.Height / image.Width;
                    g.DrawImage(image, 0, 100 - scaledHeight / 2, 200, scaledHeight);
                }
                else
                {
                    float scaledWidth = 200 * image.Width / image.Height;
                    g.DrawImage(image, 100 - scaledWidth / 2, 0, scaledWidth, 200);
                }
            }
            return scaled;
        }
    }
}
