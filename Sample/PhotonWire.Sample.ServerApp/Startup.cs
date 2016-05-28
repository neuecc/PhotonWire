using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Photon.SocketServer;
using PhotonWire.Server;

namespace PhotonWire.Sample.ServerApp
{
    public class Startup : PhotonWireApplicationBase
    {
        public override bool IsDebugMode
        {
            get
            {
                return true;
            }
        }

        protected override bool IsServerToServerPeer(InitRequest initRequest)
        {
            return (initRequest.ApplicationId == "MyMaster");
        }

        protected override void SetupCore()
        {
            var _ = ConnectToOutboundServerAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4530), "MyMaster");
        }
    }
}