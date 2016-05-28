#pragma warning disable CS1998

using System.Threading.Tasks;
using PhotonWire.Server;
using PhotonWire.Server.ServerToServer;
using PhotonWire.Sample.ServerApp.GameServer.ReceiveServerHubs;
using PhotonWire.Sample.ServerApp.Filters;

namespace PhotonWire.Sample.ServerApp.MasterServer.ServerHubs
{
    [Hub(3)]
    public class MasterTest : ServerHub
    {
        [Operation(5)]
        [TestFilter]

        public virtual async Task<string> HogeAsync()
        {
            Context.Peer.AddGroup("hugahuga");

            //var huga = await GetHubProxy<TestReceive>().All.Invoke(x => x.TakoChop("hogehoge!!!"));
            var ai = GetReceiveServerHubProxy<TestReceive>().Caller.TakoChop("nanone");
            return "zzz"; // huga[0].Result;
        }
    }
}

#pragma warning restore CS1998