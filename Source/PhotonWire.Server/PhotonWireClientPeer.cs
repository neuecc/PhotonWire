using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;

namespace PhotonWire.Server
{
    public sealed class PhotonWireClientPeer : ClientPeer, IPhotonWirePeer
    {
        /// <summary>Property storage per connection.</summary>
        public ConcurrentDictionary<object, object> Items { get; }
        public PeerBase PeerBase => this;
        public PeerKind PeerKind => PeerKind.Client;

        readonly HashSet<Action<int, string>> disconnectActions = new HashSet<Action<int, string>>();

        internal PhotonWireClientPeer(InitRequest initRequest) : base(initRequest)
        {
            Items = new ConcurrentDictionary<object, object>();
        }

        // async runner.
        protected override async void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            await PhotonWireEngine.Instance.ProcessRequest(HubKind.Client, this, operationRequest, sendParameters).ConfigureAwait(false);
        }

        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            var remoteAddress = this.RemoteIP + ":" + this.RemotePort;
            PhotonWireApplicationBase.Instance.Logger.ClientPeerOnDisconnect(PhotonWireApplicationBase.Instance.ApplicationName, remoteAddress, this.ConnectionId, reasonCode.ToString(), reasonDetail);

            List<Exception> exceptions = new List<Exception>();
            PeerManager.ClientConnections.Remove(this);
            Action<int, string>[] copy;
            lock (disconnectActions)
            {
                if (disconnectActions.Count == 0) return;
                copy = new Action<int, string>[disconnectActions.Count];
                disconnectActions.CopyTo(copy);
            }

            foreach (var item in copy)
            {
                try
                {
                    item((int)reasonCode, reasonDetail);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public IDisposable RegisterDisconnectAction(Action<int, string> action)
        {
            lock (disconnectActions)
            {
                disconnectActions.Add(action);
            }
            return new Subscription(this, action);
        }


        class Subscription : IDisposable
        {
            readonly PhotonWireClientPeer peer;
            readonly Action<int, string> action;

            public Subscription(PhotonWireClientPeer peer, Action<int, string> action)
            {
                this.peer = peer;
                this.action = action;
            }

            public void Dispose()
            {
                lock (peer.disconnectActions)
                {
                    peer.disconnectActions.Remove(action);
                }
            }
        }
    }
}
