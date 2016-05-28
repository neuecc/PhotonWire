using System;
using System.IO;
using System.Text;

namespace PhotonWire.Server
{
    // Serialization in Photon
    // http://doc.photonengine.com/en/onpremise/current/reference/serialization-in-photon

    public static class PhotonSerializers
    {
        public static IPhotonSerializer MsgPack { get; } = new PhotonMsgPackSerializer();
        public static IPhotonSerializer Json { get; } = new PhotonJsonNetSerializer();
    }

    public interface IPhotonSerializer
    {
        object Serialize(object obj);

        object Deserialize(Type type, object value);
    }

    public abstract class PhotonSerializerBase : IPhotonSerializer
    {
        static bool IsPhotonSupportedType(Type type)
        {
            if (type == typeof(int[])) return true;
            if (type == typeof(byte[])) return true;
            if (type.IsEnum) return false;

            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Byte:
                case TypeCode.Boolean:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.String:
                    return true;
                default:
                    return false;
            }
        }

        public object Serialize(object obj)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            if (IsPhotonSupportedType(type)) return obj;

            return SerializeCore(obj);
        }

        public abstract byte[] SerializeCore(object obj);

        public object Deserialize(Type type, object value)
        {
            if (value == null) return null;
            if (value.GetType() != typeof(byte[])) return value;

            if (type == typeof(byte[])) return value;

            return DeserializeCore(type, (byte[])value);
        }

        public abstract object DeserializeCore(Type type, byte[] value);
    }

    internal class PhotonMsgPackSerializer : PhotonSerializerBase
    {
        internal readonly MsgPack.Serialization.SerializationContext serializationContext = new MsgPack.Serialization.SerializationContext
        {
            EnumSerializationMethod = MsgPack.Serialization.EnumSerializationMethod.ByUnderlyingValue,
            GeneratorOption = MsgPack.Serialization.SerializationMethodGeneratorOption.Fast,
            SerializationMethod = MsgPack.Serialization.SerializationMethod.Array,
        };

        public override object DeserializeCore(Type type, byte[] value)
        {
            return serializationContext.GetSerializer(type).UnpackSingleObject(value);
        }

        public override byte[] SerializeCore(object obj)
        {
            return serializationContext.GetSerializer(obj.GetType()).PackSingleObject(obj);
        }
    }

    internal class PhotonJsonNetSerializer : PhotonSerializerBase
    {
        readonly Newtonsoft.Json.JsonSerializer serializer;

        public PhotonJsonNetSerializer()
        {
            this.serializer = new Newtonsoft.Json.JsonSerializer();
        }

        public PhotonJsonNetSerializer(Newtonsoft.Json.JsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public override object DeserializeCore(Type type, byte[] value)
        {
            if (type.IsEnum)
            {
                var str = Encoding.UTF8.GetString(value);
                var v = Enum.Parse(type, str);
                return v;
            }

            using (var ms = new MemoryStream(value))
            using (var sr = new StreamReader(ms, new UTF8Encoding(false)))
            {
                return serializer.Deserialize(sr, type);
            }
        }

        public override byte[] SerializeCore(object obj)
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms, new UTF8Encoding(false)))
            {
                serializer.Serialize(sw, obj);
                sw.Flush();
                return ms.ToArray();
            }
        }
    }
}