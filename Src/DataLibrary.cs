using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SOR4Explorer
{
    class DataLibrary
    {
        private readonly Dictionary<string, Dictionary<string, Object>> objects = new Dictionary<string, Dictionary<string, Object>>();

        public class Object
        {
            public string Name;
            public string ClassName;
            public byte[] Data;
        }

        public bool Load(string installationPath)
        {
            objects.Clear();

            try
            {
                using var file = File.OpenRead(Path.Combine(installationPath, "data/bigfile"));
                using var stream = new DeflateStream(file, CompressionMode.Decompress);
                using var reader = new BinaryReader(stream, Encoding.Unicode);

                var signature = reader.ReadInt32();
                while (true)
                {
                    var className = reader.ReadString();
                    var objectName = reader.ReadString().Replace('/', Path.DirectorySeparatorChar);
                    var length = reader.ReadInt32();
                    var data = reader.ReadBytes(length);
                    AddObject(new Object()
                    {
                        Name = objectName,
                        ClassName = className,
                        Data = data
                    });
                }
            }
            catch (EndOfStreamException)
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }

            void AddObject(Object obj)
            {
                if (objects.ContainsKey(obj.ClassName) == false)
                    objects[obj.ClassName] = new Dictionary<string, Object>() { { obj.Name, obj } };
                else
                    objects[obj.ClassName][obj.Name] = obj;
            }
        }

        public PackedData UnPack(string className, string objectName)
        {
            objectName = objectName.Replace('/', Path.DirectorySeparatorChar);
            if (objects.ContainsKey(className) == false)
                return default;
            if (objects[className].ContainsKey(objectName) == false)
                return default;

            var data = objects[className][objectName].Data;
            return new PackedData
            {
                ClassName = className,
                ObjectName = objectName,
                Properties = DataLoader.Unpack(data.AsSpan(), null),
            };
        }

        public T Unserialize<T>(string name) where T : class, new()
        {
            var className = typeof(T).Name;
            var objectName = name.Replace('/', Path.DirectorySeparatorChar);
            if (objects.ContainsKey(className) == false)
                return default;
            if (objects[className].ContainsKey(objectName) == false)
                return default;
            var data = objects[className][objectName].Data;
            return DataLoader.Unserialize(typeof(T), data.AsSpan()) as T;
        }

    }
}
