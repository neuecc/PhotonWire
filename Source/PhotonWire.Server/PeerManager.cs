using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Photon.SocketServer;
using PhotonWire.Server.ServerToServer;

namespace PhotonWire.Server
{
    /// <summary>
    /// Managing Peer and Peer Groups.
    /// </summary>
    public class PeerManager
    {
        public static PeerManager ClientConnections { get; } = new PeerManager(PeerKind.Client);
        public static PeerManager InboundServerConnections { get; } = new PeerManager(PeerKind.Inbound);
        public static PeerManager OutboundServerConnections { get; } = new PeerManager(PeerKind.Outbound);

        ImmutableHashSet<IPhotonWirePeer> currentHandlers = ImmutableHashSet<IPhotonWirePeer>.Empty;
        ConcurrentDictionary<int, IPhotonWirePeer> connectionIdToPeer = new ConcurrentDictionary<int, IPhotonWirePeer>();

        readonly PeerKind verifyPeerKind;

        // Peer -> Group Manager, needs lock
        readonly object groupLock = new object();
        Dictionary<string, ImmutableHashSet<IPhotonWirePeer>> groupHandlers = new Dictionary<string, ImmutableHashSet<IPhotonWirePeer>>();
        Dictionary<IPhotonWirePeer, List<string>> peerToGroup = new Dictionary<IPhotonWirePeer, List<string>>();

        PeerManager(PeerKind peerKind)
        {
            verifyPeerKind = peerKind;
        }

        public static PeerManager GetPeerManager(PeerKind peerKind)
        {
            switch (peerKind)
            {
                case PeerKind.Inbound:
                    return PeerManager.InboundServerConnections;
                case PeerKind.Outbound:
                    return PeerManager.OutboundServerConnections;
                case PeerKind.Client:
                    return PeerManager.ClientConnections;
                default:
                    throw new ArgumentOutOfRangeException("Unknown PeerKind, " + peerKind);
            }
        }

        void Verify(IPhotonWirePeer peer)
        {
            if (peer.PeerKind != verifyPeerKind)
            {
                throw new InvalidOperationException($"Target peer is not match PeerKind, PeerManagerKind:{verifyPeerKind} PeerKind:{peer.PeerKind}");
            }
        }

        IEnumerable<IPhotonWirePeer> Verify(IEnumerable<IPhotonWirePeer> peers)
        {
            foreach (var peer in peers)
            {
                Verify(peer);
                yield return peer;
            }
        }

        internal void Add(IPhotonWirePeer peer)
        {
            Verify(peer);

            ImmutableInterlocked.Update(ref currentHandlers, (x, y) => x.Add(y), peer);
            connectionIdToPeer[peer.ConnectionId] = peer;
        }

        internal void Remove(IPhotonWirePeer peer)
        {
            Verify(peer);

            ImmutableInterlocked.Update(ref currentHandlers, (x, y) => x.Remove(y), peer);
            IPhotonWirePeer _out;
            connectionIdToPeer.TryRemove(peer.ConnectionId, out _out);

            lock (groupLock)
            {
                List<string> groupNames;
                if (peerToGroup.TryGetValue(peer, out groupNames))
                {
                    foreach (var groupName in groupNames.ToArray()) // avoid halloween problem
                    {
                        ImmutableHashSet<IPhotonWirePeer> set;
                        if (groupHandlers.TryGetValue(groupName, out set))
                        {
                            set = set.Remove(peer);
                            if (set.Count == 0)
                            {
                                groupHandlers.Remove(groupName);
                            }
                            else
                            {
                                groupHandlers[groupName] = set;
                            }
                        }

                        groupNames.Remove(groupName);
                        if (groupNames.Count == 0)
                        {
                            peerToGroup.Remove(peer);
                        }
                    }
                }
            }
        }

        public ImmutableHashSet<IPhotonWirePeer> GetExceptOne(IPhotonWirePeer peer)
        {
            Verify(peer);

            return currentHandlers.Remove(peer);
        }

        public ImmutableHashSet<IPhotonWirePeer> GetExceptPeers(IEnumerable<IPhotonWirePeer> peers)
        {
            return currentHandlers.Except(Verify(peers));
        }

        public ImmutableHashSet<IPhotonWirePeer> GetAll()
        {
            return currentHandlers;
        }

        // Group

        public void AddGroup(string groupName, IPhotonWirePeer peer)
        {
            Verify(peer);

            lock (groupLock)
            {
                ImmutableHashSet<IPhotonWirePeer> set;
                if (!groupHandlers.TryGetValue(groupName, out set))
                {
                    set = ImmutableHashSet.Create<IPhotonWirePeer>();
                }
                set = set.Add(peer);
                groupHandlers[groupName] = set;

                List<string> groups;
                if (!peerToGroup.TryGetValue(peer, out groups))
                {
                    groups = new List<string>();
                    peerToGroup.Add(peer, groups);
                }
                groups.Add(groupName);
            }
        }

        public void RemoveGroup(string groupName, IPhotonWirePeer peer)
        {
            Verify(peer);

            lock (groupLock)
            {
                ImmutableHashSet<IPhotonWirePeer> set;
                if (groupHandlers.TryGetValue(groupName, out set))
                {
                    set = set.Remove(peer);
                    if (set.Count == 0)
                    {
                        groupHandlers.Remove(groupName);
                    }
                    else
                    {
                        groupHandlers[groupName] = set;
                    }
                }

                List<string> groups;
                if (peerToGroup.TryGetValue(peer, out groups))
                {
                    groups.Remove(groupName);
                    if (groups.Count == 0)
                    {
                        peerToGroup.Remove(peer);
                    }
                }
            }
        }

        public void RemoveGroupAll(string groupName)
        {
            lock (groupLock)
            {
                ImmutableHashSet<IPhotonWirePeer> set;
                if (groupHandlers.TryGetValue(groupName, out set))
                {
                    groupHandlers.Remove(groupName);

                    foreach (var peer in set)
                    {
                        List<string> groups;
                        if (peerToGroup.TryGetValue(peer, out groups))
                        {
                            groups.Remove(groupName);
                            if (groups.Count == 0)
                            {
                                peerToGroup.Remove(peer);
                            }
                        }
                    }
                }
            }
        }

        public ImmutableHashSet<IPhotonWirePeer> GetGroup(string groupName)
        {
            lock (groupLock)
            {
                ImmutableHashSet<IPhotonWirePeer> set;
                return groupHandlers.TryGetValue(groupName, out set)
                    ? set
                    : ImmutableHashSet<IPhotonWirePeer>.Empty;
            }
        }

        // ByConn

        public IPhotonWirePeer GetByConnectionId(int connectionId)
        {
            IPhotonWirePeer peer;
            return connectionIdToPeer.TryGetValue(connectionId, out peer)
                ? peer
                : null;
        }

        // Context

        public static HubContext GetHubContext<THub>()
            where THub : IHub
        {
            var descriptor = PhotonWireEngine.Instance.GetHubDescriptor<THub>();
            if (descriptor == null)
            {
                throw new InvalidOperationException("Couldn't find hub. Type:" + typeof(THub).FullName);
            }
            return new HubContext(descriptor);
        }

        public static HubContext GetHubContext<THub>(SendParameters sendParameters)
            where THub : IHub
        {
            var descriptor = PhotonWireEngine.Instance.GetHubDescriptor<THub>();
            if (descriptor == null)
            {
                throw new InvalidOperationException("Couldn't find hub. Type:" + typeof(THub).FullName);
            }
            return new HubContext(descriptor, sendParameters);
        }

        public static HubContext<TClient> GetHubContext<THub, TClient>()
            where THub : IHub
            where TClient : class
        {
            var descriptor = PhotonWireEngine.Instance.GetHubDescriptor<THub>();
            if (descriptor == null)
            {
                throw new InvalidOperationException("Couldn't find hub. Type:" + typeof(THub).FullName);
            }

            return new HubContext<TClient>(descriptor);
        }

        public static HubContext<TClient> GetHubContext<THub, TClient>(SendParameters sendParameters)
            where THub : IHub
            where TClient : class
        {
            var descriptor = PhotonWireEngine.Instance.GetHubDescriptor<THub>();
            if (descriptor == null)
            {
                throw new InvalidOperationException("Couldn't find hub. Type:" + typeof(THub).FullName);
            }

            return new HubContext<TClient>(descriptor, sendParameters);
        }

        public static HubClientProxy<TClient> GetHubClient<THub, TClient>()
            where THub : IHub
            where TClient : class
        {
            return GetHubContext<THub, TClient>().Clients;
        }

        public static HubClientProxy<TClient> GetHubClient<THub, TClient>(SendParameters sendParameters)
            where THub : IHub
            where TClient : class
        {
            return GetHubContext<THub, TClient>(sendParameters).Clients;
        }

        public static ServerHubContext<THub> GetServerHubContext<THub>()
            where THub : ServerHub
        {
            return GetServerHubContextCore<THub>();
        }

        public static ServerHubContext<THub> GetServerHubContext<THub>(SendParameters sendParameters)
            where THub : ServerHub
        {
            return GetServerHubContextCore<THub>(sendParameters);
        }

        public static ServerHubContext<THub> GetReceiveServerHubContext<THub>()
            where THub : ReceiveServerHub
        {
            return GetServerHubContextCore<THub>();
        }

        public static ServerHubContext<THub> GetReceiveServerHubContext<THub>(SendParameters sendParameters)
            where THub : ReceiveServerHub
        {
            return GetServerHubContextCore<THub>(sendParameters);
        }

        static ServerHubContext<THub> GetServerHubContextCore<THub>()
           where THub : IServerHub
        {
            var descriptor = PhotonWireEngine.Instance.GetHubDescriptor<THub>();
            if (descriptor == null)
            {
                throw new InvalidOperationException("Couldn't find hub. Type:" + typeof(THub).FullName);
            }

            return new ServerHubContext<THub>(descriptor);
        }

        static ServerHubContext<THub> GetServerHubContextCore<THub>(SendParameters sendParameters)
           where THub : IServerHub
        {
            var descriptor = PhotonWireEngine.Instance.GetHubDescriptor<THub>();
            if (descriptor == null)
            {
                throw new InvalidOperationException("Couldn't find hub. Type:" + typeof(THub).FullName);
            }

            return new ServerHubContext<THub>(descriptor, sendParameters);
        }
    }
}