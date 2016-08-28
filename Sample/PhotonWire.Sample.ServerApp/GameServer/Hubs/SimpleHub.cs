using PhotonWire.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotonWire.Sample.ServerApp.GameServer.Hubs
{
    public interface ISimpleHubClient
    {
        [Operation(0)]
        void ToClient(int x, int y);

        [Operation(1)]
        void Blank();

        [Operation(2)]
        void Single(int z);
    }

    [Hub(9932)]
    public class SimpleHub : Hub<ISimpleHubClient>
    {
        [Operation(0)]
        public string Hoge(int x)
        {
            Clients.All.Blank();
            Clients.All.Single(x);
            Clients.All.ToClient(x, x * x);

            return x.ToString();
        }
    }
}
