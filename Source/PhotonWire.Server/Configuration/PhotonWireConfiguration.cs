using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace PhotonWire.Server.Configuration
{
    public class PhotonWireConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("connection", IsDefaultCollection =true)]
        public ConnectionElementCollection Connections
        {
            get
            {
                return (ConnectionElementCollection)base["connection"];
            }
        }

        public static PhotonWireConfigurationSection GetSection()
        {
            return ConfigurationManager.GetSection("photonWire") as PhotonWireConfigurationSection;
        }

        public IEnumerable<ConnectionElement> GetConnectionList()
        {
            return Connections.AsEnumerable();
        }
    }

    public class ConnectionElementCollection : ConfigurationElementCollection, IEnumerable<ConnectionElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConnectionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var e = (ConnectionElement)element;
            return e.IPAddress + ":" + e.Port + "." + e.ApplicationName;
        }

        IEnumerator<ConnectionElement> IEnumerable<ConnectionElement>.GetEnumerator()
        {
            var e = base.GetEnumerator();
            while (e.MoveNext())
            {
                yield return (ConnectionElement)e.Current;
            }
        }
    }

    public class ConnectionElement : ConfigurationElement
    {
        [ConfigurationProperty("ipAddress", IsRequired = true)]
        public string IPAddress { get { return (string)base["ipAddress"]; } }

        [ConfigurationProperty("port", IsRequired = true, DefaultValue = 0)]
        public int Port { get { return (int)base["port"]; } }

        [ConfigurationProperty("applicationName", IsRequired = true)]
        public string ApplicationName { get { return (string)base["applicationName"]; } }
    }
}
