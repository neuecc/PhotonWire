using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

namespace PhotonWire.Server
{
    // Helper for Peer.Items
    public static class DictionaryItemsExtensions
    {
        /// <summary>
        /// Get from object key and cast result value.
        /// </summary>
        public static T GetValueOrDefault<T>(this IDictionary<object, object> items, object key, T defaultValue = default(T))
        {
            object value;
            if (items.TryGetValue(key, out value))
            {
                return (T)value;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}