using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PhotonWire.HubInvoker
{
    internal static class ReservedParameterNo
    {
        /// <summary>Key for Byte[] Response Result</summary>
        public const byte ResponseId = 253;

        /// <summary>Key for short Request HubId</summary>
        public const byte RequestHubId = 254;

        /// <summary>Key for Int MessageId</summary>
        public const byte MessageId = 255;
    }

    public static class JsonPhotonSerializer
    {
        static readonly UTF8Encoding UTF8 = new UTF8Encoding(false);

        public static object Serialize(string typeName, string obj)
        {
            if (obj == null) return null;

            switch (typeName)
            {
                case "Byte":
                    return byte.Parse(obj);
                case "Boolean":
                    return bool.Parse(obj);
                case "Int16":
                    return short.Parse(obj);
                case "Int32":
                    return int.Parse(obj);
                case "Int64":
                    return long.Parse(obj);
                case "Single":
                    return Single.Parse(obj);
                case "Double":
                    return double.Parse(obj);
                case "String":
                    return obj;
                case "Int32[]": // parse "," separated value...
                    return obj.Trim('[', ']').Split(',').Select(x => int.Parse(x.Trim())).ToArray();
                case "Byte[]":
                    return obj.Trim('[', ']').Split(',').Select(x => byte.Parse(x.Trim())).ToArray();
            }

            // others, write JSON
            return UTF8.GetBytes(obj);
        }

        // deserialize with human readable string
        public static object Deserialize(object value)
        {
            if (value == null) return null;
            var t = value.GetType();
            if (t == typeof(int[])) return "[" + string.Join(", ", (int[])value) + "]";
            if (t != typeof(byte[])) return value;

            // to human readable dump...
            return Encoding.UTF8.GetString((byte[])value);
        }
    }
}