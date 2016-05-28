using System.Collections.Generic;
using System.Linq;

namespace PhotonWire.Server.Collections
{
    public class ReadOnlyHashSet<T>
    {
        readonly HashSet<T> hashSet;

        public ReadOnlyHashSet(HashSet<T> hashSet)
        {
            this.hashSet = hashSet;
        }

        public bool Contains(T item)
        {
            return this.hashSet.Contains(item);
        }

        public IEnumerable<T> AsEnumerable()
        {
            return hashSet.AsEnumerable();
        }
    }
}