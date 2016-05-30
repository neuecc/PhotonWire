using PhotonWire.Sample.ServerApp.MasterServer.ServerHubs;
using PhotonWire.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PhotonWire.Sample.ServerApp.GameServer.Hubs
{
    public interface ITutorialClient
    {
        [Operation(0)]
        void GroupBroadcastMessage(string message);
    }

    // 1. Inherit Hub<T>, T is client method.
    // 2. Add HubAttribute to Class
    [Hub(100)]
    public class Tutorial : PhotonWire.Server.Hub<ITutorialClient>
    {
        // 3. Add OperationAttribute to Method
        [Operation(0)]
        public int Sum(int x, int y)
        {
            return x + y;
        }

        // 4. async + API
        [Operation(1)]
        public async Task<string> GetHtml(string url)
        {
            var httpClient = new HttpClient();
            var result = await httpClient.GetStringAsync(url);

            // Photon's String deserialize size limitation
            var cut = result.Substring(0, Math.Min(result.Length, short.MaxValue - 5000));

            return cut;
        }

        // 5. Group and Broadcast to Client

        [Operation(2)]
        public void BroadcastAll(string message)
        {
            // Get ClientProxy from Clients property, choose target and Invoke.
            this.Clients.All.GroupBroadcastMessage(message);
        }

        [Operation(3)]
        public void RegisterGroup(string groupName)
        {
            // Group is registered by per connection(peer)
            this.Context.Peer.AddGroup(groupName);
        }

        [Operation(4)]
        public void BroadcastTo(string groupName, string message)
        {
            // Get ITutorialClient -> Invoke method
            this.Clients.Group(groupName).GroupBroadcastMessage(message);
        }

        // 6. Call Sever to Server

        [Operation(5)]
        public async Task<int> ServerToServer(int x, int y)
        {
            var mul = await GetServerHubProxy<MasterTutorial>().Single.Multiply(x, y);

            return mul;
        }
    }


}
