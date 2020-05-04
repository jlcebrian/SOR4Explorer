using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
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

        public object Unserialize(Type T, ReadOnlySpan<byte> data)
        {
            var fields = T.GetFields();
            object result;

            // Strings are a special case
            if (T == typeof(string))
                result = new string("");
            else
                result = Activator.CreateInstance(T);

            int index = 0;
            while (index < data.Length)
            {
                int chunk = ReadInt(data, ref index);
                int fieldID = (chunk >> 3);
                int encoding = (chunk & 0x7);

                if (encoding == 5)          // Float encoding
                {
                    var value = BitConverter.ToSingle(data.Slice(index, 4));
                    index += 4;
                    GetField(fieldID)?.SetValue(result, value);
                }
                else if (encoding == 0)     // Integer encoding (compressed)
                {
                    var value = ReadInt(data, ref index);
                    var field = GetField(fieldID);
                    if (field != null)
                    {
                        if (field.FieldType == typeof(bool))
                            field.SetValue(result, value != 0);
                        else
                            field.SetValue(result, value);
                    }
                }
                else if (encoding == 2)     // Object encoding
                {
                    var length = ReadInt(data, ref index);
                    var slice = data.Slice(index, length);
                    index += length;

                    if (T == typeof(string))
                        return Encoding.UTF8.GetString(slice);
                    
                    var field = GetField(fieldID);
                    if (field != null)
                    {
                        if (field.FieldType == typeof(string))
                        {
                            // Detect internal string encoding. Actually, this could be a case of
                            // trying to deserialize a string[] instead of a string. Further tests needed!
                            if (slice.Length > 0 && slice[0] == 10)
                            {
                                int subindex = 1;
                                if (ReadInt(slice, ref subindex) == slice.Length - subindex)
                                {
                                    field.SetValue(result, Encoding.UTF8.GetString(slice.Slice(subindex)));
                                    continue;
                                }
                            }
                            string value = Encoding.UTF8.GetString(slice);
                            field.SetValue(result, value);
                        }
                        else if (field.FieldType.IsArray)
                        {
                            var value = Unserialize(field.FieldType.GetElementType(), slice);
                            Array previousArray = (Array)field.GetValue(result);
                            if (previousArray == null)
                            {
                                previousArray = Array.CreateInstance(field.FieldType.GetElementType(), 1);
                                previousArray.SetValue(value, 0);
                                field.SetValue(result, previousArray);
                            }
                            else
                            {
                                Array newArray = Array.CreateInstance(field.FieldType.GetElementType(), previousArray.Length + 1);
                                Array.Copy(previousArray, newArray, previousArray.Length);
                                newArray.SetValue(value, previousArray.Length - 1);
                                field.SetValue(result, newArray);
                            }
                        }
                        else
                        {
                            field.SetValue(result, Unserialize(field.FieldType, slice));
                        }
                    }
                }
                else
                {
                    throw new Exception("Invalid encoding");
                }
            }
            return result;

            FieldInfo GetField(int id)
            {
                foreach (var field in fields)
                {
                    var idAttribute = field.GetCustomAttribute<SerializationID>();
                    if (idAttribute != null && idAttribute.id == id)
                        return field;
                }
                if (id >= 1 && id <= fields.Length)
                    return fields[id - 1];
                else
                    return null;
            }
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
            return Unserialize(typeof(T), data.AsSpan()) as T;
        }

        private static int ReadInt(ReadOnlySpan<byte> data, ref int index)
        {
            int rot = 0;
            int total = 0;
            int value;
            if (index >= data.Length)
                return 0;

            do
            {
                value = data[index++];
                total |= (value & 0x7F) << rot;
                rot += 7;
            }
            while ((value & 0x80) != 0 && index < data.Length);
            return total;
        }

    }
}
