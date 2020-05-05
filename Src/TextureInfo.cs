using System;
using System.Drawing;

namespace SOR4Explorer
{
    public class TextureInfo
    {
        public string name;
        public UInt32 offset;
        public UInt32 flags;
        public UInt32 length;
        public string datafile;
        public bool changed;
        public bool original = true;
    }

    struct ImageOpProgress
    {
        public int processed;
        public int count;
    }

    struct ImageChange
    {
        public string datafile;
        public string path;
        public Bitmap image;
    }
}
