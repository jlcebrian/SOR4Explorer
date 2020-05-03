using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SOR4Explorer
{
    class DraggableImages : IDataObject
    {
        private readonly TextureLibrary library;
        private readonly TextureInfo[] images;
        private string[] temporaryFiles;

        public DraggableImages(TextureLibrary library, TextureInfo[] images)
        {
            this.library = library;
            this.images = images;
        }

        public object GetData(string format, bool autoConvert) => GetData(format);
        public object GetData(Type format) => GetData(format.ToString());
        public object GetData(string format)
        {
            if (temporaryFiles == null)
            {
                temporaryFiles = new string[images.Length];
                for (int n = 0; n < images.Length; n++)
                {
                    var name = Path.ChangeExtension(Path.GetFileName(images[n].name), ".png");
                    var path = Path.Combine(Path.GetTempPath(), name);
                    var image = library.LoadTexture(images[n]);
                    image.Save(path, ImageFormat.Png);
                    temporaryFiles[n] = path;
                }
            }
            return temporaryFiles;
        }

        public bool GetDataPresent(string format, bool autoConvert) => GetDataPresent(format);
        public bool GetDataPresent(string format) => format == DataFormats.FileDrop || format == "SOR4Explorer";
        public bool GetDataPresent(Type format) => false;

        public string[] GetFormats(bool autoConvert) => GetFormats();
        public string[] GetFormats() => new string[] { DataFormats.FileDrop };

        public void SetData(string format, bool autoConvert, object data) { }
        public void SetData(string format, object data) { }
        public void SetData(Type format, object data) { }
        public void SetData(object data) { }
    }
}
