using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Photon.SocketServer;

namespace PhotonWire.Server.ServerToServer
{
    public interface IServerHubContext
    {
        HubDescriptor Hub { get; }
        SendParameters SendParameters { get; }
        Task SendOperationRequestAsync(IS2SPhotonWirePeer peer, byte methodOpCode, object[] arguments);
        Task<T> SendOperationRequestAsync<T>(IS2SPhotonWirePeer peer, byte methodOpCode, object[] arguments);
    }

    public class FailedSendOperationException : Exception
    {
        public OperationResponse OperationResponse { get; }

        public FailedSendOperationException(OperationResponse operationResponse)
        {
            this.OperationResponse = operationResponse;
        }
    }

    public class ServerHubContext<T> : IServerHubContext
        where T : IServerHub
    {
        public HubDescriptor Hub { get; }
        public SendParameters SendParameters { get; }
        public ServerHubClientProxy<T> Peers { get; }

        readonly IPhotonSerializer serializer;

        public ServerHubContext(HubDescriptor hub)
            : this(hub, default(SendParameters))
        {
        }

        public ServerHubContext(HubDescriptor hub, SendParameters sendParameters)
        {
            this.Hub = hub;
            this.SendParameters = sendParameters;
            this.Peers = new ServerHubClientProxy<T>(this);
            this.serializer = ((PhotonWireApplicationBase)PhotonWireApplicationBase.Instance).Serializer;
        }

        async Task IServerHubContext.SendOperationRequestAsync(IS2SPhotonWirePeer peer, byte methodOpCode, object[] arguments)
        {
            var parameters = new Dictionary<byte, object>();
            for (byte i = 0; i < arguments.Length; i++)
            {
                parameters.Add(i, serializer.Serialize(arguments[i]));
            }
            parameters[ReservedParameterNo.RequestHubId] = Hub.HubId;

            var response = await peer.SendOperationRequestAsync(methodOpCode, parameters, SendParameters);

            if (response.ReturnCode == 0)
            {
                // success
                return;
            }
            else
            {
                throw new FailedSendOperationException(response);
            }
        }

        async Task<T1> IServerHubContext.SendOperationRequestAsync<T1>(IS2SPhotonWirePeer peer, byte methodOpCode, object[] arguments)
        {
            var parameters = new Dictionary<byte, object>();
            for (byte i = 0; i < arguments.Length; i++)
            {
                parameters.Add(i, serializer.Serialize(arguments[i]));
            }
            parameters[ReservedParameterNo.RequestHubId] = Hub.HubId;

            var response = await peer.SendOperationRequestAsync(methodOpCode, parameters, SendParameters);

            if (response.ReturnCode == 0)
            {
                // success
                var responseResult = response[ReservedParameterNo.ResponseId];
                var deserialized = (T1)serializer.Deserialize(typeof(T1), responseResult);
                return deserialized;
            }
            else
            {
                throw new FailedSendOperationException(response);
            }
        }
    }

    public class GroupInvokeResponse
    {
        public IPhotonWirePeer Peer { get; }

        public GroupInvokeResponse(IPhotonWirePeer peer)
        {
            this.Peer = peer;
        }
    }

    public class GroupInvokeResponse<T>
    {
        public IPhotonWirePeer Peer { get; }
        public T Result { get; }

        public GroupInvokeResponse(IPhotonWirePeer peer, T result)
        {
            this.Peer = peer;
            this.Result = result;
        }
    }

    public class GroupInvoker<T>
        where T : IServerHub
    {
        readonly IEnumerable<Tuple<IPhotonWirePeer, T>> proxies;

        internal GroupInvoker(IServerHubContext context, IEnumerable<IPhotonWirePeer> peers)
        {
            this.proxies = peers.Select(x => Tuple.Create(x, TypedServerHubClientBuilder<T>.Build(context, (IS2SPhotonWirePeer)x)));
        }

        public async Task<GroupInvokeResponse<TR>[]> Invoke<TR>(Func<T, Task<TR>> invoke)
        {
            var results = await Task.WhenAll(proxies.Select(x => invoke(x.Item2)));
            return proxies.Zip(results, (x, y) => new GroupInvokeResponse<TR>(x.Item1, y)).ToArray();
        }

        public async Task<GroupInvokeResponse[]> Invoke(Func<T, Task> invoke)
        {
            await Task.WhenAll(proxies.Select(x => invoke(x.Item2)));
            return proxies.Select(x => new GroupInvokeResponse(x.Item1)).ToArray();
        }
    }

    public class ServerHubClientProxy<T>
        where T : IServerHub
    {
        protected readonly IServerHubContext context;

        internal ServerHubClientProxy(IServerHubContext context)
        {
            this.context = context;
        }

        protected PeerManager GetPeerManager()
        {
            switch (context.Hub.HubKind)
            {
                case HubKind.Client:
                    // To->Client use Client
                    return PeerManager.ClientConnections;
                case HubKind.Server:
                    // To->Master use Outbound
                    return PeerManager.OutboundServerConnections;
                case HubKind.ReceiveServer:
                    // To->GameServer use Inbound
                    return PeerManager.InboundServerConnections;
                default:
                    throw new Exception("Unknown HubKind, HubKind:" + context.Hub.HubKind);
            }
        }

        /// <summary>
        /// Broadcast to target group(in current gameserver) except exclude peers.
        /// </summary>
        public GroupInvoker<T> Group(string groupName, params IPhotonWirePeer[] excludePeers)
        {
            var group = GetPeerManager().GetGroup(groupName);
            if (excludePeers.Length == 0)
            {
                return new GroupInvoker<T>(context, group);
            }
            else
            {
                return new GroupInvoker<T>(context, group.Except(excludePeers));
            }
        }

        /// <summary>
        /// Broadcast to target groups(in current gameserver) except exclude peers.
        /// </summary>
        public GroupInvoker<T> Groups(IEnumerable<string> groupNames, params IPhotonWirePeer[] excludePeers)
        {
            var group = groupNames.SelectMany(x => GetPeerManager().GetGroup(x));
            if (excludePeers.Length == 0)
            {
                return new GroupInvoker<T>(context, group);
            }
            else
            {
                return new GroupInvoker<T>(context, group.Except(excludePeers));
            }
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except exclude group.
        /// </summary>
        public GroupInvoker<T> OthersInGroup(string groupName)
        {
            var group = GetPeerManager().GetGroup(groupName);
            return new GroupInvoker<T>(context, GetPeerManager().GetAll().Except(group));
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except exclude groups.
        /// </summary>
        public GroupInvoker<T> OthersInGroups(IEnumerable<string> groupNames)
        {
            var group = groupNames.SelectMany(x => GetPeerManager().GetGroup(x));
            return new GroupInvoker<T>(context, GetPeerManager().GetAll().Except(group));
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) client.
        /// </summary>
        public GroupInvoker<T> All
        {
            get
            {
                return new GroupInvoker<T>(context, GetPeerManager().GetAll());
            }
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except exclude peers.
        /// </summary>
        public GroupInvoker<T> AllExcept(params IPhotonWirePeer[] excludePeers)
        {
            return new GroupInvoker<T>(context, GetPeerManager().GetExceptPeers(excludePeers));
        }

        /// <summary>
        /// Broadcast to single(in current gameserver) client, target peer is All.First().
        /// </summary>
        public T Single
        {
            get
            {
                return TypedServerHubClientBuilder<T>.Build(context, (IS2SPhotonWirePeer)GetPeerManager().GetAll().First());
            }
        }

        /// <summary>
        /// Broadcast to single(in current gameserver) client, target peer is All[Random()].
        /// </summary>
        public T RandomOne
        {
            get
            {
                // note:ElementAt is slow, we needs the improvement
                var peers = GetPeerManager().GetAll();
                var random = Utils.ThreadSafeRandom;
                var i = random.Next(peers.Count);
                return TypedServerHubClientBuilder<T>.Build(context, (IS2SPhotonWirePeer)peers.ElementAt(i));
            }
        }

        /// <summary>
        /// Broadcast to target(in current gameserver) client.
        /// </summary>
        public T Target(IPhotonWirePeer peer)
        {
            return TypedServerHubClientBuilder<T>.Build(context, (IS2SPhotonWirePeer)peer);
        }

        /// <summary>
        /// Broadcast to target(in current gameserver) clients.
        /// </summary>
        public GroupInvoker<T> Targets(IEnumerable<IPhotonWirePeer> peers)
        {
            return new GroupInvoker<T>(context, peers);
        }
    }

    public class ServerHubCallerClientProxy<T> : ServerHubClientProxy<T>
        where T : IServerHub
    {
        readonly OperationContext operationContext;

        public ServerHubCallerClientProxy(OperationContext operationContext, IServerHubContext context)
            : base(context)
        {
            this.operationContext = operationContext;
        }

        /// <summary>
        /// Broadcast to caller client.
        /// </summary>
        public T Caller
        {
            get
            {
                return TypedServerHubClientBuilder<T>.Build(context, (IS2SPhotonWirePeer)operationContext.Peer);
            }
        }

        /// <summary>
        /// Broadcast to all(in current gameserver) except caller client.
        /// </summary>
        public GroupInvoker<T> Others
        {
            get
            {
                return new GroupInvoker<T>(context, GetPeerManager().GetExceptOne(operationContext.Peer));
            }
        }
    }
}