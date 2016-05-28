using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotonWire.Server
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class HubTag : Attribute
    {
        public IReadOnlyCollection<string> Tags { get; }

        public HubTag(params string[] tag)
        {
            this.Tags = tag;
        }
    } 

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HubAttribute : Attribute
    {
        public short HubId { get; }

        public HubAttribute(short hubId)
        {
            this.HubId = hubId;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class OperationAttribute : Attribute
    {
        public byte OperationCode { get; }
        public OperationAttribute(byte operationCode)
        {
            this.OperationCode = operationCode;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class IgnoreClientGenerateAttribute : Attribute
    {

    }
}