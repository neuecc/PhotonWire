using System;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhotonWire.Client;

namespace PhotonWire.Server.Test
{
    // note:Needs server, run -> Sample.ServerApp

    [TestClass]
    public class Startup
    {
        public static ObservablePhotonPeer Peer { get; private set; }

        [AssemblyInitialize]
        public static void Initialize(TestContext cx)
        {
            Peer = new ObservablePhotonPeer(ExitGames.Client.Photon.ConnectionProtocol.Tcp)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            var task = Peer.ConnectAsync("127.0.0.1:4530", "ServerApp");

            task.Wait(); // wait for timeout seconds...
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            if (Peer != null) Peer.Disconnect();
        }
    }
}