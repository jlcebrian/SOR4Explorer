using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SOR4Explorer
{
    /// <summary>
    /// Provides a convenient view into the game's packed data files
    /// </summary>
    public class PackedData
    {
        public enum Type
        {
            String,             // string
            Integer,            // int
            Float,              // float
            Object,             // Property
            StringArray,        // string[]
            IntegerArray,       // int[]
            FloatArray,         // float[]
            ObjectArray,        // Property[]
        }

        public class Descriptor
        {
            public int id;
            public Descriptor parent;

            public override string ToString()
            {
                return parent == null ? $"#{id}" : $"{parent}.{id}";
            }
        }

        public class Property
        {
            public Descriptor Descriptor;
            public Type Type;
            public object Value;
        }

        public string ClassName;
        public string ObjectName;
        public Property[] Properties;
    }

    /// <summary>
    /// This class contains methods to unpack serialized
    /// data from the game's 'bigfile' storage
    /// </summary>
    static class DataLoader
    {
        /// <summary>
        /// Unpack a packed binary stream into a set of description objects contained
        /// inside the 'PackedData' class. This should be called only with valid streams
        /// (top level are always valid; if the stream is inside a property, 
        /// IsValidContainer should be called to ensure no errors).
        /// </summary>
        /// <returns>An array of Property[] objects</returns>
        public static PackedData.Property[] Unpack(ReadOnlySpan<byte> data, PackedData.Descriptor descriptor)
        {
            var children = new Dictionary<int, PackedData.Property>();

            int index = 0;
            while (index < data.Length)
            {
                int chunk = ReadInt(data, ref index);
                int fieldID = (chunk >> 3);
                int encoding = (chunk & 0x7);
                var childDescriptor = new PackedData.Descriptor { id = fieldID, parent = descriptor };
                switch (encoding)
                {
                    case 0:     // Integer
                        {
                            int value = ReadInt(data, ref index);
                            AddItem(children, fieldID, PackedData.Type.Integer, childDescriptor, value);
                            break;
                        }
                    case 2:     // Struct or string
                        {
                            var length = ReadInt(data, ref index);
                            var slice = data.Slice(index, length);
                            index += length;

                            // Force string interpretation if we already have objets
                            bool isString = IsString(slice) || !IsValidContainer(slice) || length == 0;
                            if (children.ContainsKey(fieldID) && children[fieldID].Type == PackedData.Type.StringArray)
                                isString = true;

                            if (isString)        // String
                            {
                                // Detect internal string encoding. Actually, this could be a case of
                                // trying to deserialize a string[] instead of a string. Further tests needed!
                                if (slice.Length > 1 && slice[0] == 10)
                                {
                                    int subindex = 1;
                                    while (ReadInt(slice, ref subindex) == slice.Length - subindex && subindex <= slice.Length)
                                    {
                                        slice = slice.Slice(subindex);
                                        if (slice.Length < 2 || slice[0] != 10)
                                            break;
                                        subindex = 1;
                                    }
                                }
                                var value = Encoding.UTF8.GetString(slice);
                                AddItem(children, fieldID, PackedData.Type.String, childDescriptor, value);
                            }
                            else
                            {
                                var value = Unpack(slice, childDescriptor);
                                AddItem(children, fieldID, PackedData.Type.Object, childDescriptor, value);
                            }
                            break;
                        }
                    case 5:     // Float
                        {
                            var value = BitConverter.ToSingle(data.Slice(index, 4));
                            index += 4;

                            AddItem(children, fieldID, PackedData.Type.Float, childDescriptor, value);
                            break;
                        }
                }
            }
            return children.Values.ToArray();

            static void AddItem(Dictionary<int, PackedData.Property> children, int fieldID,
                PackedData.Type type, PackedData.Descriptor childDescriptor, object value)
            {
                if (children.ContainsKey(fieldID) == false)
                {
                    children[fieldID] = new PackedData.Property()
                    {
                        Descriptor = childDescriptor,
                        Type = type,
                        Value = value
                    };
                }
                else if (children[fieldID].Type == type)
                {
                    var array = Array.CreateInstance(ArrayType(type), 2);
                    array.SetValue(children[fieldID].Value, 0);
                    array.SetValue(value, 1);
                    children[fieldID] = new PackedData.Property()
                    {
                        Descriptor = childDescriptor,
                        Type = ConvertToArray(type),
                        Value = array
                    };
                }
                else if (children[fieldID].Type == ConvertToArray(type))
                {
                    // TODO: This is expensive. We should preprocess the data stream
                    // beforehand to know how many array elements must be allocated in advance.

                    var previous = (Array)children[fieldID].Value;
                    var next = Array.CreateInstance(ArrayType(type), previous.Length + 1);
                    previous.CopyTo(next, 0);
                    next.SetValue(value, next.Length - 1);
                    children[fieldID] = new PackedData.Property()
                    {
                        Descriptor = childDescriptor,
                        Type = ConvertToArray(type),
                        Value = next
                    };
                }
                else
                {
                    throw new Exception($"Inconsistent data types ({type} in a {children[fieldID].Type}) for {childDescriptor}");
                }
            }
        }

        /// <summary>
        /// Deserialization helper. Given a packed data stream, produces
        /// an object by filling its public properties. Use the
        /// SerializationID attribute to assign proper IDs to the
        /// public fields (the declaration order will be used otherwise).
        /// </summary>
        public static object Unserialize(Type T, ReadOnlySpan<byte> data)
        {
            var fields = T.GetFields();

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

            object result = Activator.CreateInstance(T);

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
        }

        /// <summary>
        /// A simple heuristic to identify strings which could otherwise be
        /// considered valid binary containers. Unfortunately, the storage
        /// format does not discriminate between strings and containers.
        /// </summary>
        /// <returns>True if the data looks like a string</returns>
        static bool IsString(ReadOnlySpan<byte> data)
        {
            bool containsBinary = false;
            foreach (var c in data)
                if (c > 127 || (c < 32 && c != 10 && c != 13))
                    containsBinary = true;
            return !containsBinary;
        }

        /// <summary>
        /// Check if the given data stream contains a valid packed container.
        /// This only helps to discriminate: if this returns false, the container
        /// *is* a string. If it returns true, it still *may* be a string.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>True if the data can be parsed as a packed container</returns>
        static bool IsValidContainer(ReadOnlySpan<byte> data)
        {
            int index = 0;
            while (index < data.Length)
            {
                int chunk = ReadInt(data, ref index);
                int fieldID = (chunk >> 3);
                int encoding = (chunk & 0x7);
                if (fieldID > 256 || (encoding != 0 && encoding != 2 && encoding != 5))
                    return false;
                if (encoding == 0)          // Integer
                {
                    ReadInt(data, ref index);
                }
                else if (encoding == 2)     // Container
                {
                    int length = ReadInt(data, ref index);
                    if (length < 0)
                        return false;
                    
                    // Note that the container data may not be valid (strings)
                    index += length;
                }
                else                        // Float
                {
                    index += 4;
                }
            }
            return (index == data.Length);
        }

        static int ReadInt(ReadOnlySpan<byte> data, ref int index)
        {
            int rot = 0;
            int total = 0;
            int value;
            if (index >= data.Length || index < 0)
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

        static PackedData.Type ConvertToArray(PackedData.Type type)
        {
            return type switch
            {
                PackedData.Type.Float => PackedData.Type.FloatArray,
                PackedData.Type.Integer => PackedData.Type.IntegerArray,
                PackedData.Type.String => PackedData.Type.StringArray,
                PackedData.Type.Object => PackedData.Type.ObjectArray,
                _ => throw new Exception("Invalid type")
            };
        }

        static System.Type ArrayType(PackedData.Type type)
        {
            return type switch
            {
                PackedData.Type.Float => typeof(float),
                PackedData.Type.Integer => typeof(int),
                PackedData.Type.String => typeof(string),
                PackedData.Type.Object => typeof(PackedData.Property[]),
                _ => throw new Exception("Invalid type")
            };
        }
    }
}
