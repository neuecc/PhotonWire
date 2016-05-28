using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using System.Threading;
using System.Diagnostics;

#if (UNITY || UNITY_10 || UNITY_9 || UNITY_8 || UNITY_7 || UNITY_6 || UNITY_5 || UNITY_5_0 || UNITY_4_6 || UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0_0 || UNITY_3_0 || UNITY_2_6_1 || UNITY_2_6)
using UniRx;
#else
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
#endif

namespace PhotonWire.Client
{
    public struct PhotonPeerListenerLog
    {
        public DebugLevel Level { get; private set; }
        public string Message { get; private set; }

        public PhotonPeerListenerLog(DebugLevel level, string message)
        {
            this.Level = level;
            this.Message = message;
        }
    }

    public static class ReservedParameterNo
    {
        /// <summary>Key for Byte[] Response Result</summary>
        public const byte ResponseId = 253;

        /// <summary>Key for short Request HubId</summary>
        public const byte RequestHubId = 254;

        /// <summary>Key for Int MessageId</summary>
        public const byte MessageId = 255;
    }

    public struct OperationResponseResult
    {
        public bool IsHandled { get; private set; }
        public OperationResponse OperationResponse { get; set; }

        public OperationResponseResult(bool isHandled, OperationResponse operationResponse)
        {
            this.IsHandled = isHandled;
            this.OperationResponse = operationResponse;
        }
    }

    public class ConnectionFailedException : Exception
    {
        public ConnectionFailedException(string serverAddress, string applicationName)
            : base("Can't Connect to " + serverAddress + ", " + applicationName)
        {

        }
    }

    public class EncryptionFailedToEstablishException : Exception
    {
        public EncryptionFailedToEstablishException()
            : base()
        {

        }
    }

    public class ObservablePhotonPeer : PhotonPeer, IDisposable
    {
        const int ReservedMessageIdKey = 255;
        int messageId = 0;
        readonly Timer serviceLoop = null;
        readonly int callIntervalMs = 0;
        readonly Subject<Dictionary<byte, object>> opCustomPublisher = new Subject<Dictionary<byte, object>>();

        public string PeerName { get; private set; }
        public TimeSpan Timeout { get; set; } // can set from property.
        public bool IsDisposed { get; private set; }
        public string LastConnectServerAddress { get; private set; }
        public string LastConnectApplicationName { get; private set; }

        new ObservablePhotonPeerListener Listener
        {
            get { return (ObservablePhotonPeerListener)base.Listener; }
        }

        public ObservablePhotonPeer(ConnectionProtocol protocolType, string peerName = null, int serviceCallRate = 20)
            : base(new ObservablePhotonPeerListener(), protocolType)
        {
            if (serviceCallRate <= 0)
            {
                throw new ArgumentOutOfRangeException("serviceCallRate must be > 0, serviceCallRate:" + serviceCallRate);
            }

            this.Timeout = TimeSpan.FromSeconds(15); // default timeout
            this.PeerName = peerName;
            this.callIntervalMs = (int)(1000 / serviceCallRate);
            this.serviceLoop = new Timer(CallService, null, callIntervalMs, System.Threading.Timeout.Infinite);

#if UNITY_EDITOR
            PhotonWire.Editor.PhotonWireWindow.AddConnection(this);

            MainThreadDispatcher.OnApplicationQuitAsObservable().Subscribe(_ =>
            {
                // If connection is leaked, unity freeze on play again.
                // http://forum.photonengine.com/discussion/6082/leaked-connection-causes-unity-to-freeze
                this.Dispose();
            });

#endif
        }

        void CallService(object state)
        {
            try
            {
                Service();
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogException(ex);
#else
                Trace.WriteLine(ex.ToString());
#endif
            }
            serviceLoop.Change(callIntervalMs, System.Threading.Timeout.Infinite);
        }

        public virtual IObservable<bool> ConnectAsync(string serverAddress, string applicationName)
        {
            ValidateDisposed();

            var future = new AsyncSubject<bool>();
            var watchStatusChanged = new SingleAssignmentDisposable();
            watchStatusChanged.Disposable = this.ObserveStatusChanged().Subscribe(x =>
            {
                if (x == StatusCode.ExceptionOnConnect)
                {
                    watchStatusChanged.Dispose();
                    future.OnError(new ConnectionFailedException(serverAddress, applicationName));
                }

                if (x == StatusCode.Connect)
                {
                    watchStatusChanged.Dispose();

                    future.OnNext(true);
                    future.OnCompleted();
                }
            });

            if (this.Connect(serverAddress, applicationName))
            {
                this.LastConnectServerAddress = serverAddress;
                this.LastConnectApplicationName = applicationName;

                return future.Timeout(Timeout).Catch((Exception ex) =>
                {
                    watchStatusChanged.Dispose();
                    this.Disconnect();
                    return Observable.Throw<bool>(ex);
                });
            }
            else
            {
                watchStatusChanged.Dispose();
                return Observable.Return(false);
            }
        }

        public virtual IObservable<bool> EstablishEncryptionAsync()
        {
            ValidateDisposed();

            var future = new AsyncSubject<bool>();
            var watchStatusChanged = new SingleAssignmentDisposable();
            watchStatusChanged.Disposable = this.ObserveStatusChanged().Subscribe(x =>
            {
                if (x == StatusCode.EncryptionFailedToEstablish)
                {
                    watchStatusChanged.Dispose();
                    future.OnError(new EncryptionFailedToEstablishException());
                }

                if (x == StatusCode.EncryptionEstablished)
                {
                    watchStatusChanged.Dispose();

                    future.OnNext(true);
                    future.OnCompleted();
                }
            });

            if (this.EstablishEncryption())
            {
                return future.Timeout(Timeout).Catch((Exception ex) =>
                {
                    watchStatusChanged.Dispose();
                    this.Disconnect();
                    return Observable.Throw<bool>(ex);
                });
            }
            else
            {
                watchStatusChanged.Dispose();
                return Observable.Return(false);
            }
        }

        public virtual IObservable<bool> SwitchConnectionAsync(string serverAddress, string applicationName)
        {
            ValidateDisposed();

            this.Disconnect();
            return ConnectAsync(serverAddress, applicationName);
        }

        public virtual IObservable<OperationResponse> OpCustomAsync(byte customOpCode, Dictionary<byte, object> customOpParameters, bool sendReliable, byte channelId = 0, bool encrypt = false)
        {
            ValidateDisposed();

            var msgId = Interlocked.Increment(ref messageId);
            var future = Listener.IssueOperationResponseFuture(msgId);

            customOpParameters[ReservedMessageIdKey] = msgId;
            opCustomPublisher.OnNext(customOpParameters);

            var beforeStatusCode = Listener.LastStatusCode;
            if (!base.OpCustom(customOpCode, customOpParameters, sendReliable, channelId, encrypt))
            {
                AsyncSubject<OperationResponse> _future;
                Listener.TryGetAndRemoveFuture(msgId, out _future);
                future.OnError(new Exception(string.Format("Can't send message. BeforeStatusCode:{0} AfterStatusCode:{1}", beforeStatusCode, Listener.LastStatusCode)));
                return future.AsObservable();
            }

            return future.Timeout(Timeout).Do(_ => { }, ex =>
            {
                if (ex is TimeoutException)
                {
                    AsyncSubject<OperationResponse> removeTarget;
                    Listener.TryGetAndRemoveFuture(msgId, out removeTarget);
                }
            });
        }

        public IObservable<StatusCode> ObserveStatusChanged()
        {
            return this.Listener.OnStatusChangedAsObservable();
        }

        public IObservable<Dictionary<byte, object>> ObserveSendOpCustom()
        {
            return opCustomPublisher.AsObservable();
        }

        public IObservable<object> ObserveReceiveMessage()
        {
            return this.Listener.OnMessageAsObservable();
        }

        public IObservable<EventData> ObserveReceiveEventData()
        {
            return this.Listener.OnEventAsObservable();
        }

        public IObservable<OperationResponseResult> ObserveOperationResponse()
        {
            return this.Listener.OnOperationResponseAsObservable();
        }

        public IObservable<PhotonPeerListenerLog> ObserveDebugReturn()
        {
            return this.Listener.OnDebugReturnAsObservable();
        }

        protected void ValidateDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(this.GetType().Name);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (serviceLoop != null)
            {
                serviceLoop.Dispose(); // stop timer
            }
            this.StopThread();

            IsDisposed = true;
        }

        ~ObservablePhotonPeer()
        {
            Dispose(false);
        }

        class ObservablePhotonPeerListener : IPhotonPeerListener
        {
            StatusCode lastStatusCode = StatusCode.Disconnect;
            Dictionary<int, AsyncSubject<OperationResponse>> operationResponseFuture = new Dictionary<int, AsyncSubject<OperationResponse>>();

            public AsyncSubject<OperationResponse> IssueOperationResponseFuture(int messageId)
            {
                var future = new AsyncSubject<OperationResponse>();
                lock (operationResponseFuture)
                {
                    operationResponseFuture.Add(messageId, future);
                }
                return future;
            }

            readonly Subject<PhotonPeerListenerLog> photonPeerListenerLogPublisher = new Subject<PhotonPeerListenerLog>();
            readonly Subject<StatusCode> onstatusChangedPublisher = new Subject<StatusCode>();
            readonly Subject<object> onMessagePublisher = new Subject<object>();
            readonly Subject<EventData> onEventPublisher = new Subject<EventData>();
            readonly Subject<OperationResponseResult> onOperationResponse = new Subject<OperationResponseResult>();

            public StatusCode LastStatusCode { get { return lastStatusCode; } }

            // IPhotonPeerListener

            public void DebugReturn(DebugLevel level, string message)
            {
                photonPeerListenerLogPublisher.OnNext(new PhotonPeerListenerLog(level, message));
            }

            public IObservable<PhotonPeerListenerLog> OnDebugReturnAsObservable()
            {
                return photonPeerListenerLogPublisher.AsObservable();
            }

            public void OnEvent(EventData eventData)
            {
                onEventPublisher.OnNext(eventData);
            }

            public IObservable<EventData> OnEventAsObservable()
            {
                return onEventPublisher.AsObservable();
            }

            public void OnMessage(object messages)
            {
                onMessagePublisher.OnNext(messages);
            }

            public IObservable<object> OnMessageAsObservable()
            {
                return onMessagePublisher.AsObservable();
            }

            public void OnOperationResponse(OperationResponse operationResponse)
            {
                object messageId;
                if (!operationResponse.Parameters.TryGetValue(ReservedMessageIdKey, out messageId))
                {
                    onOperationResponse.OnNext(new OperationResponseResult(false, operationResponse));
                    return;
                }

                AsyncSubject<OperationResponse> future;
                if (TryGetAndRemoveFuture((int)messageId, out future))
                {
                    if (operationResponse.ReturnCode == 0)
                    {
                        future.OnNext(operationResponse);
                        future.OnCompleted();
                    }
                    else if (operationResponse.ReturnCode == -1)
                    {
                        future.OnError(new ServerResponseErrorException(operationResponse.ReturnCode, operationResponse.DebugMessage ?? ""));
                    }
                    else
                    {
                        var result = operationResponse.Parameters[ReservedParameterNo.ResponseId];
                        future.OnError(new CustomErrorException(operationResponse.ReturnCode, operationResponse.DebugMessage ?? "", result));
                    }

                    onOperationResponse.OnNext(new OperationResponseResult(true, operationResponse));
                }
                else
                {
                    // canceled, already removed
                    onOperationResponse.OnNext(new OperationResponseResult(false, operationResponse));
                }
            }

            public IObservable<OperationResponseResult> OnOperationResponseAsObservable()
            {
                return onOperationResponse.AsObservable();
            }

            public bool TryGetAndRemoveFuture(int messageId, out AsyncSubject<OperationResponse> future)
            {
                lock (operationResponseFuture)
                {
                    var success = operationResponseFuture.TryGetValue(messageId, out future);
                    if (success)
                    {
                        operationResponseFuture.Remove(ReservedMessageIdKey);
                    }
                    return success;
                }
            }

            public void OnStatusChanged(StatusCode statusCode)
            {
                lastStatusCode = statusCode;
                onstatusChangedPublisher.OnNext(statusCode);
            }

            public IObservable<StatusCode> OnStatusChangedAsObservable()
            {
                return Observable.Defer(() => Observable.Return(lastStatusCode))
                    .Concat(onstatusChangedPublisher);
            }
        }
    }
}