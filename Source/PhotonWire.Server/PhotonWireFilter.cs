using System;
using System.Threading.Tasks;

namespace PhotonWire.Server
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public abstract class PhotonWireFilterAttribute : Attribute
    {
        int order = int.MaxValue;
        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        public abstract Task<object> Invoke(OperationContext context, Func<Task<object>> next);
    }
}
