using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace NPythonCore
{
    public static class NpyFormat
    {
        public static T Load<T>(byte[] bytes) where T : class, ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable
        {
            return ToGenericType<T>(LoadMatrix(bytes));
        }

        public static T Load<T>(byte[] bytes, out T value) where T : class, ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable
        {
            return value = Load<T>(bytes);
        }

        public static T Load<T>(string path, out T value) where T : class, ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable
        {
            return value = Load<T>(path);
        }

        public static T Load<T>(Stream stream, out T value) where T : class, ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable
        {
            return value = Load<T>(stream);
        }

        public static T Load<T>(string path) where T : class, ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                return Load<T>(stream);
            }
        }

        public static T Load<T>(Stream stream) where T : class, ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable
        {
            return ToGenericType<T>(LoadMatrix(stream));
        }

        public static Array LoadMatrix(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return LoadMatrix(stream);
            }
        }

        public static Array LoadMatrix(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                return LoadMatrix(stream);
            }
        }

        public static Array LoadMatrix(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.GetEncoding("iso-8859-1")))
            {
                int bytes;
                Type type;
                int[] shape;

                if (!ParseReader(reader, out bytes, out type, out shape))
                {
                    throw new FormatException();
                }

                Array matrix = Array.CreateInstance(type, shape);

                if (type == typeof(String))
                {
                    throw new NotSupportedException();
                }

                return ReadValueMatrix(reader, matrix, bytes, type, shape);
            }
        }

        private static Array ReadValueMatrix(BinaryReader reader, Array matrix, int bytes, Type type, int[] shape)
        {
            int total = 1;

            for (int i = 0; i < shape.Length; i++)
            {
                total *= shape[i];
            }

            var buffer = new byte[bytes * total];
            reader.Read(buffer, 0, buffer.Length);
            Buffer.BlockCopy(buffer, 0, matrix, 0, buffer.Length);

            return matrix;
        }

        private static bool ParseReader(BinaryReader reader, out int bytes, out Type t, out int[] shape)
        {
            bytes = 0;
            t = null;
            shape = null;

            if (reader.ReadChar() != 147) return false;
            if (reader.ReadChar() != 'N') return false;
            if (reader.ReadChar() != 'U') return false;
            if (reader.ReadChar() != 'M') return false;
            if (reader.ReadChar() != 'P') return false;
            if (reader.ReadChar() != 'Y') return false;

            byte major = reader.ReadByte();
            byte minor = reader.ReadByte();

            if (major != 1 || minor != 0)
            {
                throw new NotSupportedException();
            }

            ushort len = reader.ReadUInt16();
            string header = new String(reader.ReadChars(len));
            string mark = "'descr': '";
            int s = header.IndexOf(mark) + mark.Length;
            int e = header.IndexOf("'", s + 1);
            string type = header.Substring(s, e - s);
            bool? isLittleEndian;
            t = GetType(type, out bytes, out isLittleEndian);

            if (isLittleEndian.HasValue && isLittleEndian.Value == false)
            {
                throw new Exception();
            }

            mark = "'fortran_order': ";
            s = header.IndexOf(mark) + mark.Length;
            e = header.IndexOf(",", s + 1);
            bool fortran = bool.Parse(header.Substring(s, e - s));

            if (fortran)
            {
                throw new Exception();
            }

            mark = "'shape': (";
            s = header.IndexOf(mark) + mark.Length;
            e = header.IndexOf(")", s + 1);
            shape = header.Substring(s, e - s).Split(',').Select(Int32.Parse).ToArray();

            return true;
        }

        private static Type GetType(string dtype, out int bytes, out bool? isLittleEndian)
        {
            isLittleEndian = IsLittleEndian(dtype);
            bytes = Int32.Parse(dtype.Substring(2));

            string typeCode = dtype.Substring(1);

            if (typeCode == "b1")
            {
                return typeof(bool);
            }

            if (typeCode == "i1")
            {
                return typeof(SByte);
            }

            if (typeCode == "i2")
            {
                return typeof(Int16);
            }

            if (typeCode == "i4")
            {
                return typeof(Int32);
            }

            if (typeCode == "i8")
            {
                return typeof(Int64);
            }

            if (typeCode == "u1")
            {
                return typeof(Byte);
            }

            if (typeCode == "u2")
            {
                return typeof(UInt16);
            }

            if (typeCode == "u4")
            {
                return typeof(UInt32);
            }

            if (typeCode == "u8")
            {
                return typeof(UInt64);
            }

            if (typeCode == "f4")
            {
                return typeof(Single);
            }

            if (typeCode == "f8")
            {
                return typeof(Double);
            }

            if (typeCode.StartsWith("S"))
            {
                return typeof(String);
            }

            throw new NotSupportedException();
        }

        private static bool? IsLittleEndian(string type)
        {
            bool? littleEndian = null;

            switch (type[0])
            {
                case '<':
                    littleEndian = true;
                    break;
                case '>':
                    littleEndian = false;
                    break;
                case '|':
                    littleEndian = null;
                    break;
                default:
                    throw new Exception();
            }

            return littleEndian;
        }


        public static T ToGenericType<T>(object value)
        {
            Type type = typeof(T);

            if (value == null)
            {
                return (T)Convert.ChangeType(null, type);
            }

            if (type.IsInstanceOfType(value))
            {
                return (T)value;
            }

            if (type.IsEnum)
            {
                return (T)Enum.ToObject(type, (int)Convert.ChangeType(value, typeof(int)));
            }

            Type inputType = value.GetType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                MethodInfo setter = type.GetMethod("op_Implicit", new[] { inputType });

                return (T)setter.Invoke(null, new object[] { value });
            }

            var methods = new List<MethodInfo>();
            methods.AddRange(inputType.GetMethods(BindingFlags.Public | BindingFlags.Static));
            methods.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.Static));

            foreach (MethodInfo m in methods)
            {
                if (m.IsPublic && m.IsStatic)
                {
                    if ((m.Name == "op_Implicit" || m.Name == "op_Explicit") && m.ReturnType == type)
                    {
                        ParameterInfo[] p = m.GetParameters();

                        if (p.Length == 1 && p[0].ParameterType.IsInstanceOfType(value))
                        {
                            return (T)m.Invoke(null, new[] { value });
                        }
                    }
                }
            }

            return (T)value;
        }

        public static byte[] Save(Array array)
        {
            using (var stream = new MemoryStream())
            {
                Save(array, stream);

                return stream.ToArray();
            }
        }

        public static ulong Save(Array array, string path)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                return Save(array, stream);
            }
        }

        public static ulong Save(Array array, Stream stream)
        {
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true))
            {
                Type type;
                int maxLength;
                string dtype = GetDtypeFromType(array, out type, out maxLength);

                int[] shape = new int[array.Rank];

                for (int i = 0; i < array.Rank; i++)
                {
                    shape[i] = array.GetLength(i);
                }

                ulong bytesWritten = (ulong)WriteHeader(writer, dtype, shape);

                if (type == typeof(String))
                {
                    throw new NotSupportedException();
                }

                return bytesWritten + WriteValueMatrix(writer, array, maxLength, shape);
            }
        }

        private static ulong WriteValueMatrix(BinaryWriter reader, Array matrix, int bytes, int[] shape)
        {
            int total = 1;

            for (int i = 0; i < shape.Length; i++)
            {
                total *= shape[i];
            }

            var buffer = new byte[bytes * total];

            Buffer.BlockCopy(matrix, 0, buffer, 0, buffer.Length);
            reader.Write(buffer, 0, buffer.Length);

            return (ulong)buffer.LongLength;
        }

        private static int WriteHeader(BinaryWriter writer, string dtype, int[] shape)
        {
            char[] magic = { 'N', 'U', 'M', 'P', 'Y' };
            writer.Write((byte)147);
            writer.Write(magic);
            writer.Write((byte)1);
            writer.Write((byte)0);

            string tuple = String.Join(", ", shape.Select(i => i.ToString()).ToArray());
            string header = "{{'descr': '{0}', 'fortran_order': False, 'shape': ({1}), }}";
            header = String.Format(header, dtype, tuple);
            int preamble = 10;

            int len = header.Length + 1;
            int headerSize = len + preamble;

            int pad = 16 - (headerSize % 16);
            header = header.PadRight(header.Length + pad);
            header += "\n";
            headerSize = header.Length + preamble;

            if (headerSize % 16 != 0)
            {
                throw new Exception();
            }

            writer.Write((ushort)header.Length);

            for (int i = 0; i < header.Length; i++)
            {
                writer.Write((byte)header[i]);
            }

            return headerSize;
        }

        private static string GetDtypeFromType(Array array, out Type type, out int bytes)
        {
            type = array.GetType().GetElementType();

            if (type == typeof(String))
            {
                bytes = 1;

                foreach (String s in array)
                {
                    if (s.Length > bytes)
                    {
                        bytes = s.Length;
                    }
                }

                return "|S" + bytes;
            }

            if (type == typeof(bool))
            {
                bytes = 1;

                return "|b1";
            }

            bytes = Marshal.SizeOf(type);

            if (type == typeof(SByte))
            {
                return "|i1";
            }

            if (type == typeof(Int16))
            {
                return "<i2";
            }

            if (type == typeof(Int32))
            {
                return "<i4";
            }

            if (type == typeof(Int64))
            {
                return "<i8";
            }

            if (type == typeof(UInt16))
            {
                return "<u2";
            }

            if (type == typeof(UInt32))
            {
                return "<u4";
            }

            if (type == typeof(UInt64))
            {
                return "<u8";
            }

            if (type == typeof(Single))
            {
                return "<f4";
            }

            if (type == typeof(Double))
            {
                return "<f8";
            }

            throw new NotSupportedException();
        }

    }
}
