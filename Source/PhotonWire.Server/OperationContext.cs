using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.SocketServer;

namespace PhotonWire.Server
{
    /// <summary>
    /// Encapsulates all information about an individual Photon request.
    /// </summary>
    public sealed class OperationContext
    {
        /// <summary>Info of belongs to Hub.</summary>
        public HubDescriptor Hub { get; }

        /// <summary>Info of belongs to Method.</summary>
        public MethodDescriptor Method { get; }

        /// <summary>Peer of caller.</summary>
        public IPhotonWirePeer Peer { get; }

        /// <summary>Property storage per operation.</summary>
        public IDictionary<object, object> Items { get; }

        /// <summary>PhotonPeer's raw argument of OperationRequest.</summary>
        public OperationRequest OperationRequest { get; }

        /// <summary>PhotonPeer's raw argument of SendParameters.</summary>
        public SendParameters SendParameters { get; }

        /// <summary>Initial timestamp of the current Photon request.</summary>
        public DateTime Timestamp { get; }

        /// <summary>Parameter values of method.</summary>
        public IReadOnlyList<object> Parameters { get; internal set; } // object[]

        readonly short hubId;

        // use case for only throw error message
        internal OperationContext(short hubId, IPhotonWirePeer peer, OperationRequest operationRequest, SendParameters sendParameters, DateTime timestamp)
        {
            this.hubId = hubId;
            this.Peer = peer;
            this.OperationRequest = operationRequest;
            this.SendParameters = sendParameters;
            this.Timestamp = timestamp;
            this.Items = new Dictionary<object, object>();
        }

        internal OperationContext(HubDescriptor hub, MethodDescriptor method, IPhotonWirePeer peer, OperationRequest operationRequest, SendParameters sendParameters, DateTime timestamp)
            : this(hub.HubId, peer, operationRequest, sendParameters, timestamp)
        {
            this.Hub = hub;
            this.Method = method;
        }

        internal void SendOperation(int messageId, object result, string debugMessage, bool isError, IPhotonSerializer serializer, short? returnCode)
        {
            var parameters = new Dictionary<byte, object>();
            parameters[ReservedParameterNo.RequestHubId] = hubId;
            parameters[ReservedParameterNo.MessageId] = messageId;
            var operationResponse = new OperationResponse()
            {
                OperationCode = OperationRequest.OperationCode, // return same code:)
                Parameters = parameters,
                DebugMessage = debugMessage
            };

            parameters[ReservedParameterNo.ResponseId] = (serializer != null)
                ? serializer.Serialize(result)
                : result;
            if (!isError)
            {
                operationResponse.ReturnCode = 0; // success
            }
            else if (returnCode == null)
            {
                operationResponse.ReturnCode = -1;
            }
            else
            {
                operationResponse.ReturnCode = returnCode.Value;
            }

            var sendResult = Peer.SendOperationResponse(operationResponse, SendParameters);
            if (sendResult != SendResult.Ok)
            {
                PhotonWireApplicationBase.Instance.Logger.SendOperationFailed(PhotonWireApplicationBase.Instance.ApplicationName, hubId, OperationRequest.OperationCode, sendResult.ToString());
            }
        }
    }
}
