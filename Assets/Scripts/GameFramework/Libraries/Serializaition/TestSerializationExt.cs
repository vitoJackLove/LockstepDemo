using System;
using System.Collections.Generic;

namespace Ase.Serialization
{
    public interface ICompatible
    {
    }

    public interface ICompatibleExt : ICompatible
    {
        void SerializeExt(Serializer writer);
        void DeserializeExt(Deserializer reader, int dataOffset, int vTblOffset, int vTableLen, byte slotSize);
    }

    public static class SerExt
    {

        //public static void WriteExt<T>(this Serializer writer, ref T[] value) where T : ICompatibleExt{
        //    writer.Write(value == null);
        //    if (value == null) { }
//
        //    value.SerializeExt(writer);
        //}
        public static void WriteExt<T>(this Serializer writer, ref T value) where T : ICompatibleExt
        {
            writer.Write(value == null);
            if (value == null)
            {
            }

            value.SerializeExt(writer);
        }

        public static T ReadExt<T>(this Deserializer reader, ref T _) where T : ICompatibleExt, new()
        {
            var isDefault = reader.ReadBoolean();
            if (isDefault) return default(T);
            var typeId = reader.ReadUInt16();
            var slotSize = reader.ReadByte();
            int vTableLen = 0;
            int dataLen = 0;
            dataLen = reader.ReadInt32();
            vTableLen = reader.ReadInt32();

            var hasType = SerializationExtHelper.HasType(typeId);
            long allDataLen = vTableLen + dataLen;
            if (allDataLen > int.MaxValue)
            {
                throw new Exception($"ReadExt failed! typeId{typeId} allDataLen{allDataLen}");
            }

            var dataOffset = reader.Position;
            if (hasType)
            {
                reader.SetPosition((int)(dataOffset + allDataLen));
                return default(T);
            }
            else
            {
                var obj = SerializationExtHelper.CreateType<T>(typeId);
                obj.DeserializeExt(reader, dataOffset, dataOffset + dataLen, vTableLen, slotSize);
                reader.SkipLen(allDataLen);
                return obj;
            }
        }

        public static void BeforeWriteData(this Serializer writer, ushort id)
        {
            writer.Write(id);
            writer.Write(4);
            writer.Write(0); //dataLen
            writer.Write(0); //vTableLen
        }

        public static void AfterWriteData(this Serializer writer, int tableLen, int[] offsetTbl, int initPos)
        {
            var endPos = writer.Position;
            int maxIdx = tableLen - 1;
            for (; maxIdx >= 0 && offsetTbl[maxIdx] == 0; maxIdx--)
            {
            }

            byte slotSize = 4;
            int dataLen = endPos - initPos;
            if (dataLen < ushort.MaxValue)
            {
                slotSize = 2;
            }

            if (dataLen < ushort.MaxValue)
            {
                slotSize = 1;
            }

            if (slotSize == 1)
                for (int i = 0; i <= maxIdx; i++)
                {
                    writer.Write((byte)offsetTbl[i]);
                }
            else if (slotSize == 2)
                for (int i = 0; i <= maxIdx; i++)
                {
                    writer.Write((ushort)offsetTbl[i]);
                }
            else
                for (int i = 0; i <= maxIdx; i++)
                {
                    writer.Write((uint)offsetTbl[i]);
                }

            int vTableLen = (maxIdx + 1) * slotSize;
            writer.SetPosition(initPos - 8 - 1);
            writer.Write(slotSize);
            writer.Write(dataLen);
            writer.Write(vTableLen);
        }

    }

    public class SerializationExtHelper
    {
        private static HashSet<ushort> _typeidHashset = new HashSet<ushort>();
        private static HashSet<ushort> _isStruct = new HashSet<ushort>();

        private static Dictionary<ushort, FuncCreateObject> _typeid2FuncCreate =
            new Dictionary<ushort, FuncCreateObject>();

        public delegate object FuncCreateObject();

        public static T CreateType<T>(ushort id) where T : ICompatibleExt
        {
            if (!IsStruct(id))
            {
                var val = _typeid2FuncCreate[id]();
                return (T)val;
            }

            return default(T);
        }

        public static bool IsStruct(ushort id)
        {
            return _isStruct.Contains(id);
        }

        public static bool HasType(ushort id)
        {
            return _typeidHashset.Contains(id);
        }

        public static bool RegisterType<T>(ushort id, FuncCreateObject func)
        {
            if (func != null)
            {
                _typeid2FuncCreate[id] = func;
            }

            var isClass = typeof(T).IsClass;
            if (!isClass)
            {
                _isStruct.Add(id);
            }
            else
            {
                if (func == null)
                {
                    throw new Exception("Class must have a Factory Function" + id);
                }
            }

            return _typeidHashset.Add(id);
        }
    }
}