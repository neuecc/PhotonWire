using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Photon.SocketServer;
using Photon.SocketServer.ServerToServer;
using PhotonHostRuntimeInterfaces;

namespace PhotonWire.Server.ServerToServer
{
    public class PhotonWireOutboundS2SPeer : OutboundS2SPeer, IS2SPhotonWirePeer
    {
        int messageId = 0;
        Dictionary<int, TaskCompletionSource<OperationResponse>> operationResponseFuture = new Dictionary<int, TaskCompletionSource<OperationResponse>>();

        public ConcurrentDictionary<object, object> Items { get; }
        public PeerBase PeerBase => this;
        public PeerKind PeerKind => PeerKind.Outbound;

        public IPEndPoint RemoteEndPoint { get; private set; }
        public string ApplicationName { get; private set; }

        readonly HashSet<Action<int, string>> disconnectActions = new HashSet<Action<int, string>>();

        readonly object connectionLock = new object();
        TaskCompletionSource<bool> connectionFuture;

        public PhotonWireOutboundS2SPeer(ApplicationBase application)
            : base(application)
        {
            Items = new ConcurrentDictionary<object, object>();
        }

        TaskCompletionSource<OperationResponse> IssueOperationResponseFuture(int messageId)
        {
            var future = new TaskCompletionSource<OperationResponse>();
            lock (operationResponseFuture)
            {
                operationResponseFuture.Add(messageId, future);
            }
            return future;
        }

        bool TryGetAndRemoveFuture(int messageId, out TaskCompletionSource<OperationResponse> future)
        {
            lock (operationResponseFuture)
            {
                var success = operationResponseFuture.TryGetValue(messageId, out future);
                if (success)
                {
                    operationResponseFuture.Remove(ReservedParameterNo.MessageId);
                }
                return success;
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

        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectTcp(IPEndPoint remoteEndPoint, string applicationName, bool useMux = false, IRpcProtocol protocol = null) => base.ConnectTcp(remoteEndPoint, applicationName, useMux, protocol);
        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectTcp(IPEndPoint remoteEndPoint, string applicationName, object customInitObject, bool useMux = false, IRpcProtocol protocol = null) => base.ConnectTcp(remoteEndPoint, applicationName, customInitObject, useMux, protocol);
        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectToServerUdp(IPEndPoint remoteEndPoint, string applicationName, byte numChannels, short? mtu) => base.ConnectToServerUdp(remoteEndPoint, applicationName, numChannels, mtu);
        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectToServerUdp(IPEndPoint remoteEndPoint, string applicationName, object customInitObject, byte numChannels, short? mtu) => base.ConnectToServerUdp(remoteEndPoint, applicationName, customInitObject, numChannels, mtu);
        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectToServerWebSocket(IPEndPoint remoteEndPoint, string applicationName, WebSocketVersion? webSocketVersion, IRpcProtocol protocol) => base.ConnectToServerWebSocket(remoteEndPoint, applicationName, webSocketVersion, protocol);
        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectToServerWebSocket(IPEndPoint remoteEndPoint, string applicationName, object customInitObject, WebSocketVersion? webSocketVersion, IRpcProtocol protocol) => base.ConnectToServerWebSocket(remoteEndPoint, applicationName, customInitObject, webSocketVersion, protocol);
        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectToServerWebSocketHixie76(IPEndPoint remoteEndPoint, string applicationName, string origin) => base.ConnectToServerWebSocketHixie76(remoteEndPoint, applicationName, origin);
        [Obsolete("Use ConnectTcpAsync instead.", false)]
        new public bool ConnectToServerWebSocketHixie76(IPEndPoint remoteEndPoint, string applicationName, object customInitObject, string origin) => base.ConnectToServerWebSocketHixie76(remoteEndPoint, applicationName, customInitObject, origin);

        public Task<bool> ConnectTcpAsync(IPEndPoint remoteEndPoint, string applicationName, bool useMux = false, IRpcProtocol protocol = null)
        {
            return ConnectTcpAsync(remoteEndPoint, applicationName, null, useMux, protocol);
        }

        public Task<bool> ConnectTcpAsync(IPEndPoint remoteEndPoint, string applicationName, object customInitObject, bool useMux = false, IRpcProtocol protocol = null)
        {
            lock (connectionLock)
            {
                if (connectionFuture != null) return Task.FromResult(false);
                connectionFuture = new TaskCompletionSource<bool>();

                this.RemoteEndPoint = remoteEndPoint;
                this.ApplicationName = applicationName;
                if (!base.ConnectTcp(remoteEndPoint, applicationName, customInitObject, useMux, protocol))
                {
                    connectionFuture.TrySetResult(false);
                    var result = connectionFuture.Task;
                    connectionFuture = null;
                    return result;
                }
            }

            return connectionFuture.Task;
        }

        protected override sealed void OnConnectionEstablished(object responseObject)
        {
            lock (connectionLock)
            {
                if (connectionFuture != null) connectionFuture.TrySetResult(true);
                connectionFuture = null;
            }
            OnConnectionEstablishedCore(responseObject);
        }

        protected virtual void OnConnectionEstablishedCore(object responseObject)
        {
        }

        protected override sealed void OnConnectionFailed(int errorCode, string errorMessage)
        {
            lock (connectionLock)
            {
                if (connectionFuture != null) connectionFuture.TrySetResult(false);
                connectionFuture = null;
            }
            var appName = PhotonWireApplicationBase.Instance.ApplicationName;
            PhotonWireApplicationBase.Instance.Logger.OutboundS2SPeerConnectionFailed(appName, RemoteIP, RemotePort, errorCode, errorMessage);
            OnConnectionFailedCore(errorCode, errorMessage);
        }

        protected virtual void OnConnectionFailedCore(int errorCode, string errorMessage)
        {
        }

        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            PhotonWireApplicationBase.Instance.Logger.OutboundPeerOnDisconnect(PhotonWireApplicationBase.Instance.ApplicationName, RemoteEndPoint?.ToString(), ApplicationName, this.ConnectionId, reasonCode.ToString(), reasonDetail);

            PeerManager.OutboundServerConnections.Remove(this);
            List<Exception> exceptions = new List<Exception>();
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

        protected override void OnEvent(IEventData eventData, SendParameters sendParameters)
        {
            // do nothing
        }

        protected override async void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            await PhotonWireEngine.Instance.ProcessRequest(HubKind.ReceiveServer, this, operationRequest, sendParameters).ConfigureAwait(false);
        }

        public Task<OperationResponse> SendOperationRequestAsync(byte operationCode, Dictionary<byte, object> parameters, SendParameters sendParameters)
        {
            var msgId = Interlocked.Increment(ref messageId);
            var future = IssueOperationResponseFuture(msgId);

            parameters[ReservedParameterNo.MessageId] = msgId;

            var request = new OperationRequest(operationCode, parameters);

            var sendResult = this.SendOperationRequest(request, sendParameters);
            if (sendResult != SendResult.Ok)
            {
                TaskCompletionSource<OperationResponse> _future;
                TryGetAndRemoveFuture(msgId, out _future);
                future.SetException(new Exception(string.Format("Can't send message. SendResult:{0}", sendResult)));
                return future.Task;
            }

            return future.Task;
        }

        protected override void OnOperationResponse(OperationResponse operationResponse, SendParameters sendParameters)
        {
            // receive

            object messageId;
            if (!operationResponse.Parameters.TryGetValue(ReservedParameterNo.MessageId, out messageId))
            {
                return;
            }

            TaskCompletionSource<OperationResponse> future;
            if (TryGetAndRemoveFuture((int)messageId, out future))
            {
                if (operationResponse.ReturnCode == 0)
                {
                    future.TrySetResult(operationResponse);
                }
                else
                {
                    future.TrySetException(new ServerResponseErrorException(operationResponse.ReturnCode, operationResponse.DebugMessage ?? ""));
                }
            }
            else
            {
                // canceled, already removed
            }
        }

        class Subscription : IDisposable
        {
            readonly PhotonWireOutboundS2SPeer peer;
            readonly Action<int, string> action;

            public Subscription(PhotonWireOutboundS2SPeer peer, Action<int, string> action)
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
