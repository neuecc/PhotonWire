using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PhotonWire.Server;

namespace PhotonWire.Sample.ServerApp.Filters
{
    public class TestFilter : PhotonWireFilterAttribute
    {
        public override async Task<object> Invoke(OperationContext context, Func<Task<object>> next)
        {
            var path = context.Hub.HubName + "/" + context.Method.MethodName;
            try
            {
                Debug.WriteLine("Before:" + path + " - " + context.Peer.PeerKind);
                var result = await next();
                Debug.WriteLine("After:" + path);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ex " + path + " :" + ex.ToString());
                throw;
            }
            finally
            {
                Debug.WriteLine("Finally:" + path);
            }
        }
    }
}