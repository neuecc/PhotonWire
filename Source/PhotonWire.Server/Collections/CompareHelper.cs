using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotonWire.Server.Collections
{
    public static class ComparerHelper
    {
        public static IEqualityComparer<T> Create<T, TKey>(Func<T, TKey> compareKeySelector)
        {
            if (compareKeySelector == null) throw new ArgumentNullException("compareKeySelector");

            return new EqualityComparer<T>(
                (x, y) =>
                {
                    if (object.ReferenceEquals(x, y)) return true;
                    if (x == null || y == null) return false;
                    return compareKeySelector(x).Equals(compareKeySelector(y));
                },
                obj =>
                {
                    if (obj == null) return 0;
                    return compareKeySelector(obj).GetHashCode();
                });
        }

        private class EqualityComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> equals;
            private readonly Func<T, int> getHashCode;

            public EqualityComparer(Func<T, T, bool> equals, Func<T, int> getHashCode)
            {
                this.equals = equals;
                this.getHashCode = getHashCode;
            }

            public bool Equals(T x, T y)
            {
                return equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return getHashCode(obj);
            }
        }
    }
}