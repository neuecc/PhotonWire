using Photon.SocketServer;

namespace PhotonWire.Server.ServerToServer
{
    public interface IServerHub : IPhotonWireHub
    {
    }


    public abstract class ServerHub : IServerHub
    {
        // set from PhotonWireEngine
        public OperationContext Context { get; set; }

        [IgnoreOperation]
        protected ServerHubCallerClientProxy<T> GetReceiveServerHubProxy<T>()
            where T : ReceiveServerHub
        {
            var hubContext = PeerManager.GetReceiveServerHubContext<T>();
            return new ServerHubCallerClientProxy<T>(this.Context, hubContext);
        }
    }

    public abstract class ReceiveServerHub : IServerHub
    {
        // set from PhotonWireEngine
        public OperationContext Context { get; set; }

        [IgnoreOperation]
        protected ServerHubCallerClientProxy<T> GetServerHubProxy<T>()
            where T : ServerHub
        {
            var hubContext = PeerManager.GetServerHubContext<T>();
            return new ServerHubCallerClientProxy<T>(this.Context, hubContext);
        }

        /// <summary>
        /// Get Client Proxy of peers.
        /// </summary>
        [IgnoreOperation]
        protected HubClientProxy<TClient> GetClientsProxy<THub, TClient>()
            where THub : IHub
            where TClient : class
        {
            return PeerManager.GetHubClient<THub, TClient>();
        }

        /// <summary>
        /// Get Client Proxy of peers.
        /// </summary>
        [IgnoreOperation]
        protected HubClientProxy<TClient> GetClientsProxy<THub, TClient>(SendParameters sendParameters)
            where THub : IHub
            where TClient : class
        {
            return PeerManager.GetHubClient<THub, TClient>(sendParameters);
        }
    }

    /// <summary>
    /// Generics Helper for use ClientPeers(set default pair of Hub and Client type proxy)
    /// </summary>
    public abstract class ReceiveServerHub<THub, TClient> : ReceiveServerHub
        where THub : IHub
        where TClient : class
    {

        HubClientProxy<TClient> clientProxy = null;

        /// <summary>
        /// Client Proxy of peers.
        /// </summary>
        protected HubClientProxy<TClient> Clients
        {
            get
            {
                return clientProxy ?? (clientProxy = GetClientsProxy<THub, TClient>());
            }
        }
    }
}