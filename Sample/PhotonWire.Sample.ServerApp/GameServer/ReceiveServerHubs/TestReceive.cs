using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotonWire.Sample.ServerApp.Filters;
using PhotonWire.Server;
using PhotonWire.Server.ServerToServer;

namespace PhotonWire.Sample.ServerApp.GameServer.ReceiveServerHubs
{
    [Hub(10)]
    [TestFilter]
    public class TestReceive : ReceiveServerHub
    {
        [Operation(15)]
        public virtual Task<string> TakoChop(string echo)
        {
            Context.Peer.AddGroup("hogemoge");

            return Task.FromResult(echo);
        }
    }
}
