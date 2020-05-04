using System;
using System.Collections.Generic;
using System.Text;

namespace SOR4Explorer
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SerializationID : Attribute
    {
        public int id;
        public SerializationID(int id) => this.id = id;
    }

    // TODO: Use a dictionary for fast access
    public class LocalizationData
    {
        public struct Translation
        {
            public string key;
            public string text;
        }

        public struct Language
        {
            public string code;
            public Translation[] translations;
        }

        public Language[] languages;
    }

    public class SpriteData
    {
        public struct Size
        {
            public int width;
            public int height;
        }

        public struct Rect
        {
            public int x;
            public int y;
            public int width;
            public int height;
        }

        public class Part
        {
            public string name;
            public Rect bounds;
            public Rect frame;
        }

        public Size bounds;
        public Part[] parts;
        public bool unused;
    }
}
