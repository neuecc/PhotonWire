#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotonWire.Sample.ServerApp.Filters;
using PhotonWire.Server;
using PhotonWire.Server.ServerToServer;
using PhotonWire.Sample.ServerApp.GameServer.Hubs;

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


        [Operation(20)]
        public virtual async Task Broadcast(string group, string msg)
        {
            // Send to clients.
            this.GetClientsProxy<Tutorial, ITutorialClient>()
                .Group(group)
                .GroupBroadcastMessage(msg);
        }
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously