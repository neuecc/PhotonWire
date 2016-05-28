using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Photon.SocketServer;
using Photon.SocketServer.ServerToServer;
using PhotonHostRuntimeInterfaces;

namespace PhotonWire.Server.ServerToServer
{
    public class PhotonWireInboundS2SPeer : InboundS2SPeer, IS2SPhotonWirePeer
    {
        int messageId = 0;
        Dictionary<int, TaskCompletionSource<OperationResponse>> operationResponseFuture = new Dictionary<int, TaskCompletionSource<OperationResponse>>();

        public ConcurrentDictionary<object, object> Items { get; }
        public PeerBase PeerBase => this;
        public PeerKind PeerKind => PeerKind.Inbound;

        readonly HashSet<Action<int, string>> disconnectActions = new HashSet<Action<int, string>>();

        public PhotonWireInboundS2SPeer(InitRequest initRequest)
            : base(initRequest)
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

        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            var remoteAddress = this.RemoteIP + ":" + this.RemotePort;
            PhotonWireApplicationBase.Instance.Logger.InboundPeerOnDisconnect(PhotonWireApplicationBase.Instance.ApplicationName, remoteAddress, this.ConnectionId, reasonCode.ToString(), reasonDetail);

            PeerManager.InboundServerConnections.Remove(this);
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
            await PhotonWireEngine.Instance.ProcessRequest(HubKind.Server, this, operationRequest, sendParameters).ConfigureAwait(false);
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
            readonly PhotonWireInboundS2SPeer peer;
            readonly Action<int, string> action;

            public Subscription(PhotonWireInboundS2SPeer peer, Action<int, string> action)
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