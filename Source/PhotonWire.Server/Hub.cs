using System.Collections.Generic;
using System.Linq;
using Photon.SocketServer;
using PhotonWire.Server.ServerToServer;

namespace PhotonWire.Server
{
    /// <summary>
    /// Common interface for ClientHub(Hub), ServerHub, ReceiveServerHub
    /// </summary>
    public interface IPhotonWireHub
    {
        /// <summary>
        /// Context per operation.
        /// </summary>
        OperationContext Context { get; set; }
    }

    public interface IHub : IPhotonWireHub
    {
    }

    /// <summary>
    /// Marker of No Client.
    /// </summary>
    public interface INoClient
    {

    }

    /// <summary>
    /// Hub is operation proxy of ClientPeer.
    /// </summary>
    public abstract class Hub : Hub<INoClient>
    {

    }

    /// <summary>
    /// Hub is operation proxy of ClientPeer.
    /// </summary>
    public abstract class Hub<T> : IHub
    {
        // set from PhotonWireEngine
        public OperationContext Context { get; set; }

        HubCallerClientProxy<T> clientProxy = null;


        /// <summary>
        /// Client Proxy of peers.
        /// </summary>
        protected HubCallerClientProxy<T> Clients
        {
            get
            {
                return clientProxy ?? (clientProxy = new HubCallerClientProxy<T>(Context));
            }
        }

        [IgnoreOperation]
        protected ServerHubClientProxy<TServerHub> GetServerHubProxy<TServerHub>()
            where TServerHub : ServerHub
        {
            var hubContext = PeerManager.GetServerHubContext<TServerHub>();
            return hubContext.Peers;
        }
    }

    public class HubClientProxy<T>
    {
        protected readonly HubContext context;

        internal HubClientProxy(HubContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Broadcast to target group(in current gameserver) except exclude peers.
        /// </summary>
        public T Group(string groupName, params IPhotonWirePeer[] excludePeers)
        {
            var group = PeerManager.ClientConnections.GetGroup(groupName);
            if (excludePeers.Length == 0)
            {
                return TypedClientBuilder<T>.Build(context, group);
            }
            else
            {
                return TypedClientBuilder<T>.Build(context, group.Except(excludePeers));
            }
        }

        /// <summary>
        /// Broadcast to target groups(in current gameserver) except exclude peers.
        /// </summary>
        public T Groups(IList<string> groupNames, params IPhotonWirePeer[] excludePeers)
        {
            var group = groupNames.SelectMany(x => PeerManager.ClientConnections.GetGroup(x));
            if (excludePeers.Length == 0)
            {
                return TypedClientBuilder<T>.Build(context, group);
            }
            else
            {
                return TypedClientBuilder<T>.Build(context, group.Except(excludePeers));
            }
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except exclude group.
        /// </summary>
        public T OthersInGroup(string groupName)
        {
            var group = PeerManager.ClientConnections.GetGroup(groupName);
            return TypedClientBuilder<T>.Build(context, PeerManager.ClientConnections.GetAll().Except(group));
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except exclude groups.
        /// </summary>
        public T OthersInGroups(IEnumerable<string> groupNames)
        {
            var group = groupNames.SelectMany(x => PeerManager.ClientConnections.GetGroup(x));
            return TypedClientBuilder<T>.Build(context, PeerManager.ClientConnections.GetAll().Except(group));
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) client.
        /// </summary>
        public T All
        {
            get
            {
                return TypedClientBuilder<T>.Build(context, PeerManager.ClientConnections.GetAll());
            }
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except exclude peers.
        /// </summary>
        public T AllExcept(params IPhotonWirePeer[] excludePeers)
        {
            return TypedClientBuilder<T>.Build(context, PeerManager.ClientConnections.GetExceptPeers(excludePeers));
        }

        /// <summary>
        /// Broadcast to target(in current gameserver) client.
        /// </summary>
        public T Target(IPhotonWirePeer peer)
        {
            return TypedClientBuilder<T>.Build(context, new[] { peer });
        }

        /// <summary>
        /// Broadcast to target(in current gameserver) clients.
        /// </summary>
        public T Targets(IEnumerable<IPhotonWirePeer> peers)
        {
            return TypedClientBuilder<T>.Build(context, peers);
        }
    }


    public class HubCallerClientProxy<T> : HubClientProxy<T>
    {
        readonly IPhotonWirePeer peer;

        public HubCallerClientProxy(OperationContext context)
            : base(new HubContext<T>(context.Hub, context.SendParameters))
        {
            this.peer = context.Peer;
        }

        /// <summary>
        /// Broadcast to caller client.
        /// </summary>
        public T Caller
        {
            get
            {
                return TypedClientBuilder<T>.Build(context, new[] { peer });
            }
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except caller client.
        /// </summary>
        public T Others
        {
            get
            {
                return TypedClientBuilder<T>.Build(context, PeerManager.ClientConnections.GetExceptOne(peer));
            }
        }
    }
}