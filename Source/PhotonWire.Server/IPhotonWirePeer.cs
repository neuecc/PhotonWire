using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Photon.SocketServer;

namespace PhotonWire.Server
{
    // abstraction of ClientPeer and S2S Peer

    public interface IPhotonWirePeer
    {
        /// <summary>Property storage per connection.</summary>
        ConcurrentDictionary<object, object> Items { get; }
        PeerBase PeerBase { get; }
        PeerKind PeerKind { get; }
        int ConnectionId { get; }

        /// <summary>
        /// Action = DisconnectReason reasonCode, string reasonDetail
        /// </summary>
        IDisposable RegisterDisconnectAction(Action<int, string> disconnectAction);
        SendResult SendOperationResponse(OperationResponse operationResponse, SendParameters sendParameters);
    }

    public interface IS2SPhotonWirePeer : IPhotonWirePeer
    {
        Task<OperationResponse> SendOperationRequestAsync(byte operationCode, Dictionary<byte, object> parameters, SendParameters sendParameters);
    }

    public enum PeerKind
    {
        Inbound, Outbound, Client
    }

    public static class PhotonWirePeerExtensions
    {
        public static void AddGroup(this IPhotonWirePeer peer, string groupName)
        {
            PeerManager.GetPeerManager(peer.PeerKind).AddGroup(groupName, peer);
        }

        public static void RemoveGroup(this IPhotonWirePeer peer, string groupName)
        {
            PeerManager.GetPeerManager(peer.PeerKind).RemoveGroup(groupName, peer);
        }
    }
}