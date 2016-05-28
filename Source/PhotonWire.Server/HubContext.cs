using System;
using System.Collections.Generic;
using System.Linq;
using Photon.SocketServer;

namespace PhotonWire.Server
{
    public class HubContext
    {
        public HubDescriptor Hub { get; }
        public SendParameters SendParameters { get; }

        internal HubContext(HubDescriptor hub)
        {
            this.Hub = hub;
            this.SendParameters = default(SendParameters);
        }

        internal HubContext(HubDescriptor hub, SendParameters sendParameters)
        {
            this.Hub = hub;
            this.SendParameters = sendParameters;
        }

        public void BroadcastEvent(IEnumerable<IPhotonWirePeer> targetPeers, byte eventCode, params object[] args)
        {
            var parameters = new Dictionary<byte, object>();
            var eventData = new EventData(eventCode, parameters);
            var appBase = (PhotonWireApplicationBase)PhotonWireApplicationBase.Instance;
            var serializer = appBase.Serializer;

            for (int i = 0; i < args.Length; i++)
            {
                parameters.Add((byte)i, serializer.Serialize(args[i]));
            }
            parameters.Add(ReservedParameterNo.RequestHubId, Hub.HubId);

            if (appBase.IsDebugMode)
            {
                var standardPeers = new List<PeerBase>();
                var hubInvokerPeers = new List<PeerBase>();
                foreach (var peer in targetPeers)
                {
                    if (peer.Items.ContainsKey("PhotonWireApplicationBase.ModifySerializer"))
                    {
                        hubInvokerPeers.Add(peer.PeerBase);
                    }
                    else
                    {
                        standardPeers.Add(peer.PeerBase);
                    }
                }

                if (standardPeers.Count > 0)
                {
                    EventData.SendTo(eventData, standardPeers, SendParameters);
                }
                if (hubInvokerPeers.Count > 0)
                {
                    var jsonParameters = new Dictionary<byte, object>();
                    var jsonEventData = new EventData(eventCode, jsonParameters);
                    var jsonSerializer = PhotonSerializers.Json;
                    for (int i = 0; i < args.Length; i++)
                    {
                        jsonParameters.Add((byte)i, jsonSerializer.Serialize(args[i]));
                    }
                    jsonParameters.Add(ReservedParameterNo.RequestHubId, Hub.HubId);

                    EventData.SendTo(jsonEventData, hubInvokerPeers, SendParameters);
                }
            }
            else
            {
                EventData.SendTo(eventData, targetPeers.Select(x => x.PeerBase), SendParameters);
            }
        }

        public HubClientProxy<T> GetClientProxy<T>()
        {
            if (Hub.ClientType != typeof(T)) throw new InvalidOperationException($"Doesn't match client type, Expected:{Hub.ClientType.FullName} Actual:{typeof(T).FullName}");

            return new HubClientProxy<T>(this);
        }
    }

    public sealed class HubContext<TClient> : HubContext
    {
        internal HubContext(HubDescriptor hub)
            : base(hub)
        {
            if (hub.ClientType != typeof(TClient)) throw new InvalidOperationException($"Doesn't match client type, Expected:{Hub.ClientType.FullName} Actual:{typeof(TClient).FullName}");
        }

        internal HubContext(HubDescriptor hub, SendParameters sendParameters)
            : base(hub, sendParameters)
        {
            if (hub.ClientType != typeof(TClient)) throw new InvalidOperationException($"Doesn't match client type, Expected:{Hub.ClientType.FullName} Actual:{typeof(TClient).FullName}");
        }

        HubClientProxy<TClient> clientProxy = null;
        public HubClientProxy<TClient> Clients
        {
            get
            {
                return clientProxy ?? (clientProxy = GetClientProxy<TClient>());
            }
        }
    }
}