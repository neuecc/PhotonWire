using System;
using System.Diagnostics.Tracing;

namespace PhotonWire.Server.Diagnostics
{
    public interface IPhotonWireLogger
    {
        void InitializeComplete(string application, double elapsed);
        void SetupFailed(string application, string type, string message, string stackTrace);

        void ExecuteStart(string application, string hubType, short hubId, string hubName, byte operationCode, string methodName);
        void ExecuteFinished(string application, string hubType, short hubId, string hubName, byte operationCode, string methodName, bool isError, double elapsed);
        void RequiredParameterNotFound(string application, byte parameterNo);
        void UnhandledException(string application, string type, string message, string stackTrace);
        void SendOperationFailed(string application, short hubId, byte operationCode, string sendResult);

        void PeerReceived(string application, string applicationId, string clientVersion, int connectionId, string remoteIp, int remotePort);
        void ConnectToOutboundServer(string application, string ipEndPoint, string applicationName, double elapsed);
        void ConnectToOutboundServerFailed(string application, string ipEndPoint, string applicationName, double elapsed);
        void ReconnectToOutboundServer(string application, string ipEndPoint, string applicationName, double elapsed);
        void ReconnectToOutboundServerFailed(string application, string ipEndPoint, string applicationName, double elapsed);
        void OutboundS2SPeerConnectionFailed(string application, string remoteIp, int remotePort, int errorCode, string errorMessage);

        void OnStopRequested(string application);
        void StopOutboundReconnectTimer(string application);
        void TearDown(string application);
        void OutboundPeerOnDisconnect(string application, string remoteEndPopint, string applicationName, int connectionId, string reasonCode, string reasonDetail);
        void InboundPeerOnDisconnect(string application, string remoteAddress, int connectionId, string reasonCode, string reasonDetail);
        void ClientPeerOnDisconnect(string application, string remoteAddress, int connectionId, string reasonCode, string reasonDetail);

        void ConnectToOutboundReconnectTimerException(string application, string type, string message, string stackTrace);
        void DisconnectPeerOnRequestStoppedException(string application, string type, string message, string stackTrace);
    }

    // EventSource is "Public", you can enable SLAB/EtwStream's ObservableEventListener
    [EventSource(Name = "PhotonWire")]
    public sealed class PhotonWireEventSource : EventSource, IPhotonWireLogger
    {
        public static readonly PhotonWireEventSource Log = new PhotonWireEventSource();

        public class Keywords
        {
            public const EventKeywords Engine = (EventKeywords)1;
            public const EventKeywords ProcessRequest = (EventKeywords)2;
            public const EventKeywords Connection = (EventKeywords)4;
        }

        private PhotonWireEventSource() { }


        [Event(1, Level = EventLevel.Informational, Keywords = Keywords.Engine)]
        public void InitializeComplete(string application, double elapsed)
        {
            WriteEvent(1, application ?? "", elapsed);
        }

        [Event(2, Level = EventLevel.Error, Keywords = Keywords.Engine)]
        public void SetupFailed(string application, string type, string message, string stackTrace)
        {
            WriteEvent(2, application ?? "", type ?? "", message ?? "", stackTrace ?? "");
        }

        [Event(3, Level = EventLevel.Verbose, Keywords = Keywords.ProcessRequest)]
        public void ExecuteStart(string application, string hubType, short hubId, string hubName, byte operationCode, string methodName)
        {
            WriteEvent(3, application ?? "", hubType ?? "", hubId, hubName ?? "", operationCode, methodName ?? "");
        }

        [Event(4, Level = EventLevel.Verbose, Keywords = Keywords.ProcessRequest)]
        public void ExecuteFinished(string application, string hubType, short hubId, string hubName, byte operationCode, string methodName, bool isError, double elapsed)
        {
            WriteEvent(4, application ?? "", hubType ?? "", hubId, hubName ?? "", operationCode, methodName ?? "", isError, elapsed);
        }

        [Event(5, Level = EventLevel.Warning, Keywords = Keywords.ProcessRequest)]
        public void RequiredParameterNotFound(string application, byte parameterNo)
        {
            WriteEvent(5, application ?? "", parameterNo);
        }

        [Event(6, Level = EventLevel.Error, Keywords = Keywords.ProcessRequest)]
        public void UnhandledException(string application, string type, string message, string stackTrace)
        {
            WriteEvent(6, application ?? "", type ?? "", message ?? "", stackTrace ?? "");
        }

        [Event(7, Level = EventLevel.Error, Keywords = Keywords.ProcessRequest)]
        public void SendOperationFailed(string application, short hubId, byte operationCode, string sendResult)
        {
            WriteEvent(7, application ?? "", hubId, operationCode, sendResult ?? "");
        }

        [Event(8, Level = EventLevel.Verbose, Keywords = Keywords.Connection)]
        public void PeerReceived(string application, string applicationId, string clientVersion, int connectionId, string remoteIp, int remotePort)
        {
            WriteEvent(8, application ?? "", applicationId ?? "", clientVersion ?? "", connectionId, remoteIp ?? "", remotePort);
        }

        [Event(9, Level = EventLevel.Verbose, Keywords = Keywords.Connection)]
        public void ConnectToOutboundServer(string application, string ipEndPoint, string applicationName, double elapsed)
        {
            WriteEvent(9, application ?? "", ipEndPoint ?? "", applicationName ?? "", elapsed);
        }

        [Event(10, Level = EventLevel.Error, Keywords = Keywords.Connection)]
        public void ConnectToOutboundServerFailed(string application, string ipEndPoint, string applicationName, double elapsed)
        {
            WriteEvent(10, application ?? "", ipEndPoint ?? "", applicationName ?? "", elapsed);
        }

        [Event(11, Level = EventLevel.Verbose, Keywords = Keywords.Connection)]
        public void ReconnectToOutboundServer(string application, string ipEndPoint, string applicationName, double elapsed)
        {
            WriteEvent(11, application ?? "", ipEndPoint ?? "", applicationName ?? "", elapsed);
        }

        [Event(12, Level = EventLevel.Error, Keywords = Keywords.Connection)]
        public void ReconnectToOutboundServerFailed(string application, string ipEndPoint, string applicationName, double elapsed)
        {
            WriteEvent(12, application ?? "", ipEndPoint ?? "", applicationName ?? "", elapsed);
        }

        [Event(13, Level = EventLevel.Error, Keywords = Keywords.Connection)]
        public void OutboundS2SPeerConnectionFailed(string application, string remoteIp, int remotePort, int errorCode, string errorMessage)
        {
            WriteEvent(13, application ?? "", remoteIp ?? "", remotePort, errorCode, errorMessage ?? "");
        }

        [Event(14, Level = EventLevel.Verbose, Keywords = Keywords.Engine)]
        public void OnStopRequested(string application)
        {
            WriteEvent(14, application ?? "");
        }

        [Event(15, Level = EventLevel.Informational, Keywords = Keywords.Connection)]
        public void StopOutboundReconnectTimer(string application)
        {
            WriteEvent(15, application ?? "");
        }

        [Event(16, Level = EventLevel.Informational, Keywords = Keywords.Engine)]
        public void TearDown(string application)
        {
            WriteEvent(16, application ?? "");
        }

        [Event(17, Level = EventLevel.Verbose, Keywords = Keywords.Connection)]
        public void OutboundPeerOnDisconnect(string application, string remoteEndPopint, string applicationName, int connectionId, string reasonCode, string reasonDetail)
        {
            WriteEvent(17, application ?? "", remoteEndPopint ?? "", applicationName ?? "", connectionId, reasonCode ?? "", reasonDetail ?? "");
        }

        [Event(18, Level = EventLevel.Verbose, Keywords = Keywords.Connection)]
        public void InboundPeerOnDisconnect(string application, string remoteAddress, int connectionId, string reasonCode, string reasonDetail)
        {
            WriteEvent(18, application ?? "", remoteAddress ?? "", connectionId, reasonCode ?? "", reasonDetail ?? "");
        }

        [Event(19, Level = EventLevel.Verbose, Keywords = Keywords.Connection)]
        public void ClientPeerOnDisconnect(string application, string remoteAddress, int connectionId, string reasonCode, string reasonDetail)
        {
            WriteEvent(19, application ?? "", remoteAddress ?? "", connectionId, reasonCode ?? "", reasonDetail ?? "");
        }

        [Event(20, Level = EventLevel.Error, Keywords = Keywords.Connection)]
        public void ConnectToOutboundReconnectTimerException(string application, string type, string message, string stackTrace)
        {
            WriteEvent(20, application ?? "", type ?? "", message ?? "", stackTrace ?? "");
        }

        [Event(21, Level = EventLevel.Error, Keywords = Keywords.Connection)]
        public void DisconnectPeerOnRequestStoppedException(string application, string type, string message, string stackTrace)
        {
            WriteEvent(21, application ?? "", type ?? "", message ?? "", stackTrace ?? "");
        }
    }
}