using ExitGames.Client.Photon;
using PhotonWire.Client;

namespace PhotonWire.HubInvoker
{
    public class UseJsonObservablePhotonPeer : ObservablePhotonPeer
    {
        public UseJsonObservablePhotonPeer(ConnectionProtocol protocolType, int serviceCallRate = 20)
            : base(protocolType, "HubInvoker", serviceCallRate)
        {
        }

        public override bool Connect(string serverAddress, string applicationName)
        {
            // Use Custom
            return base.Connect(serverAddress, applicationName, "UseJsonSerializer");
        }
    }
}