using System;

namespace PhotonWire.Server
{
    internal static class Utils
    {
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        [ThreadStatic]
        private static Random random;

        public static Random ThreadSafeRandom
        {
            get
            {
                if (random == null)
                {
                    using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
                    {
                        var buffer = new byte[sizeof(int)];
                        rng.GetBytes(buffer);
                        var seed = BitConverter.ToInt32(buffer, 0);
                        random = new Random(seed);
                    }
                }

                return random;
            }
        }
    }
}