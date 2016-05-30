#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

using System;
#if (UNITY || UNITY_10 || UNITY_9 || UNITY_8 || UNITY_7 || UNITY_6 || UNITY_5 || UNITY_5_0 || UNITY_4_6 || UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0_0 || UNITY_3_0 || UNITY_2_6_1 || UNITY_2_6)
using UniRx;
#else
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
#endif
using MsgPack.Serialization;
using System.IO;
using PhotonWire.Client;
using ExitGames.Client.Photon;
using PhotonWire.Client.GeneratedSerializers;

namespace PhotonWire.Client
{
    public static class PhotonSerializer
    {
        static readonly PhotonSerializerBase serializer = new PhotonMsgPackSerializer();

        public static object Serialize<T>(T obj)
        {
            return serializer.Serialize(typeof(T), obj);
        }

        public static T Deserialize<T>(object value)
        {
            if (typeof(T) == typeof(Unit)) return default(T);
            return (T)serializer.Deserialize(typeof(T), value);
        }
    }

    public abstract class PhotonSerializerBase
    {
        static bool IsPhotonSupportedType(Type type)
        {
            if (type == typeof(int[])) return true;
            if (type == typeof(byte[])) return true;
            if (type.IsEnum) return false;

            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Byte:
                case TypeCode.Boolean:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.String:
                    return true;
                default:
                    return false;
            }
        }

        public object Serialize(Type type, object obj)
        {
            if (obj == null) return null;
            if (IsPhotonSupportedType(type)) return obj;

            return SerializeCore(obj);
        }

        public abstract byte[] SerializeCore(object obj);

        public object Deserialize(Type type, object value)
        {
            if (value == null) return null;
            if (value.GetType() != typeof(byte[])) return value;

            if (type == typeof(byte[])) return value;

            return DeserializeCore(type, (byte[])value);
        }

        public abstract object DeserializeCore(Type type, byte[] value);
    }

    public class PhotonMsgPackSerializer : PhotonSerializerBase
    {
        internal readonly MsgPack.Serialization.SerializationContext serializationContext = new MsgPack.Serialization.SerializationContext
        {
            EnumSerializationMethod = MsgPack.Serialization.EnumSerializationMethod.ByUnderlyingValue,
            SerializationMethod = MsgPack.Serialization.SerializationMethod.Array
        };

        readonly System.Collections.Generic.Dictionary<Type, Action<MsgPack.Serialization.ResolveSerializerEventArgs>> setSerializers = new System.Collections.Generic.Dictionary<Type, Action<MsgPack.Serialization.ResolveSerializerEventArgs>>(12);

        public PhotonMsgPackSerializer()
        {
            serializationContext.ResolveSerializer += SerializationContext_ResolveSerializer;

            setSerializers.Add(typeof(System.Int32?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_Int32_Serializer(e.Context)));
            setSerializers.Add(typeof(System.Byte?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_Byte_Serializer(e.Context)));
            setSerializers.Add(typeof(System.Boolean?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_Boolean_Serializer(e.Context)));
            setSerializers.Add(typeof(System.Int16?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_Int16_Serializer(e.Context)));
            setSerializers.Add(typeof(System.Int64?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_Int64_Serializer(e.Context)));
            setSerializers.Add(typeof(System.Single?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_Single_Serializer(e.Context)));
            setSerializers.Add(typeof(System.Double?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_Double_Serializer(e.Context)));
            setSerializers.Add(typeof(System.DateTime?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_System_DateTime_Serializer(e.Context)));
            setSerializers.Add(typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.PhotonWire_Sample_ServerApp_Hubs_MyClassSerializer(e.Context)));
            setSerializers.Add(typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass2), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.PhotonWire_Sample_ServerApp_Hubs_MyClass2Serializer(e.Context)));
            setSerializers.Add(typeof(PhotonWire.Sample.ServerApp.Hubs.Yo), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.PhotonWire_Sample_ServerApp_Hubs_YoSerializer(e.Context)));
            setSerializers.Add(typeof(PhotonWire.Sample.ServerApp.Hubs.Yo?), e => e.SetSerializer(new PhotonWire.Client.GeneratedSerializers.System_Nullable_1_PhotonWire_Sample_ServerApp_Hubs_Yo_Serializer(e.Context)));
   

            MsgPack.Serialization.MessagePackSerializer.PrepareType<PhotonWire.Sample.ServerApp.Hubs.Yo>();
        }

        void SerializationContext_ResolveSerializer(object sender, MsgPack.Serialization.ResolveSerializerEventArgs e)
        {
            Action<MsgPack.Serialization.ResolveSerializerEventArgs> setSerializer;
            if(setSerializers.TryGetValue(e.TargetType, out setSerializer))
            {
                setSerializer(e);
            }
        }

        public override object DeserializeCore(Type type, byte[] value)
        {
            return serializationContext.GetSerializer(type).UnpackSingleObject(value);
        }

        public override byte[] SerializeCore(object obj)
        {
            return serializationContext.GetSerializer(obj.GetType()).PackSingleObject(obj);
        }
    }
    
    public static class ObservablePhotonPeerExtensions
    {
        public static T CreateTypedHub<T>(this ObservablePhotonPeer peer)
            where T : IPhotonWireProxy, new()
        {
            var contract = new T();
            contract.Initialize(peer);

            return contract;
        }
    }

    public interface IPhotonWireProxy
    {
        ObservablePhotonPeer Peer { get; }
        void Initialize(ObservablePhotonPeer peer);
    }

    public abstract class PhotonWireProxy<TServer, TClient, TClientListener> : IPhotonWireProxy
    {
        public ObservablePhotonPeer Peer { get; private set; }

        public void Initialize(ObservablePhotonPeer peer)
        {
            this.Peer = peer;
        }

        public abstract short HubId { get; }
        public abstract string HubName { get; }
        public abstract TServer Invoke { get; }
        public abstract TClient Receive { get; }
        public abstract IDisposable RegisterListener(TClientListener clientListener, bool runOnMainThread = true);
    }

    // Auto generated proxy code
    public class ForUnitTestProxy : PhotonWireProxy<ForUnitTestProxy.ForUnitTestServer, ForUnitTestProxy.ForUnitTestClient, ForUnitTestProxy.INoClient>
    {
        public override short HubId
        {
            get
            {
                return 0;
            }
        }

        public override string HubName
        {
            get
            {
                return "ForUnitTest";
            }
        }

        ForUnitTestServer invoke;
        public override ForUnitTestServer Invoke
        {
            get
            {
                return invoke ?? (invoke = new ForUnitTestServer(Peer, HubId));
            }
        }

        ForUnitTestClient receive;
        public override ForUnitTestClient Receive
        {
            get
            {
                return receive ?? (receive = new ForUnitTestClient(Peer, HubId));
            }
        }
        
        public override IDisposable RegisterListener(INoClient clientListener, bool runOnMainThread = true)
        {
            return Peer.ObserveReceiveEventData().Subscribe(__args =>
            {
                {
                    object hubIdObj;
                    if (!__args.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                    {
                        return;
                    }
                    if ((short)hubIdObj != HubId) return;
                }

                var __parameters = __args.Parameters;
                switch (__args.Code)
                {
                    default:
                        break;
                }
            });                
        }

        public class ForUnitTestServer
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public ForUnitTestServer(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            public IObservable<System.Int32> EchoAsync(System.Int32 x, bool observeOnMainThread = true)
            {
                byte opCode = 0;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Byte> EchoAsync(System.Byte x, bool observeOnMainThread = true)
            {
                byte opCode = 1;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Byte>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Boolean> EchoAsync(System.Boolean x, bool observeOnMainThread = true)
            {
                byte opCode = 2;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Boolean>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Int16> EchoAsync(System.Int16 x, bool observeOnMainThread = true)
            {
                byte opCode = 3;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int16>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Int64> EchoAsync(System.Int64 x, bool observeOnMainThread = true)
            {
                byte opCode = 4;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int64>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Single> EchoAsync(System.Single x, bool observeOnMainThread = true)
            {
                byte opCode = 5;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Single>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Double> EchoAsync(System.Double x, bool observeOnMainThread = true)
            {
                byte opCode = 6;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Double>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Int32[]> EchoAsync(System.Int32[] x, bool observeOnMainThread = true)
            {
                byte opCode = 7;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.String> EchoAsync(System.String x, bool observeOnMainThread = true)
            {
                byte opCode = 8;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Byte[]> EchoAsync(System.Byte[] x, bool observeOnMainThread = true)
            {
                byte opCode = 9;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Byte[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.DateTime> EchoAsync(System.DateTime x, bool observeOnMainThread = true)
            {
                byte opCode = 10;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.DateTime>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Uri> EchoAsync(System.Uri x, bool observeOnMainThread = true)
            {
                byte opCode = 11;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Uri>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Nullable<System.Int32>> EchoAsync(System.Nullable<System.Int32> x, bool observeOnMainThread = true)
            {
                byte opCode = 12;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Nullable<System.Int32>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Nullable<System.Double>> EchoAsync(System.Nullable<System.Double> x, bool observeOnMainThread = true)
            {
                byte opCode = 13;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Nullable<System.Double>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Double[]> EchoAsync(System.Double[] x, bool observeOnMainThread = true)
            {
                byte opCode = 14;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Double[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Collections.Generic.List<System.Double>> EchoAsync(System.Collections.Generic.List<System.Double> x, bool observeOnMainThread = true)
            {
                byte opCode = 15;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Collections.Generic.List<System.Double>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Collections.Generic.Dictionary<System.String, System.Int32>> EchoAsync(System.Collections.Generic.Dictionary<System.String, System.Int32> x, bool observeOnMainThread = true)
            {
                byte opCode = 16;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Collections.Generic.Dictionary<System.String, System.Int32>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<PhotonWire.Sample.ServerApp.Hubs.MyClass> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.MyClass x, bool observeOnMainThread = true)
            {
                byte opCode = 17;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<PhotonWire.Sample.ServerApp.Hubs.MyClass>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.String> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.Yo yo, bool observeOnMainThread = true)
            {
                byte opCode = 18;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(yo));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.String> EchoAsync(System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> yo, bool observeOnMainThread = true)
            {
                byte opCode = 19;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(yo));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

        }

        public class ForUnitTestClient
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public ForUnitTestClient(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            IObservable<EventData> ReceiveEventData(byte eventCode)
            {
                return peer.ObserveReceiveEventData()
                    .Where(x =>
                    {
                        object hubIdObj;
                        if (!x.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                        {
                            return false;
                        }

                        if (x.Code != eventCode) return false;
                        if ((short)hubIdObj != hubId) return false;

                        return true;
                    });
            }

        }

        public interface INoClient
        {
        }

    }
    public class ChatHubProxy : PhotonWireProxy<ChatHubProxy.ChatHubServer, ChatHubProxy.ChatHubClient, ChatHubProxy.IChatClient>
    {
        public override short HubId
        {
            get
            {
                return 9;
            }
        }

        public override string HubName
        {
            get
            {
                return "ChatHub";
            }
        }

        ChatHubServer invoke;
        public override ChatHubServer Invoke
        {
            get
            {
                return invoke ?? (invoke = new ChatHubServer(Peer, HubId));
            }
        }

        ChatHubClient receive;
        public override ChatHubClient Receive
        {
            get
            {
                return receive ?? (receive = new ChatHubClient(Peer, HubId));
            }
        }
        
        public override IDisposable RegisterListener(IChatClient clientListener, bool runOnMainThread = true)
        {
            return Peer.ObserveReceiveEventData().Subscribe(__args =>
            {
                {
                    object hubIdObj;
                    if (!__args.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                    {
                        return;
                    }
                    if ((short)hubIdObj != HubId) return;
                }

                var __parameters = __args.Parameters;
                switch (__args.Code)
                {
                    case 0:
                        {
                            var userName = PhotonSerializer.Deserialize<System.String>(__parameters[0]);
                            var message = PhotonSerializer.Deserialize<System.String>(__parameters[1]);
                            if(runOnMainThread)
                            {
                                CurrentThreadScheduler.Instance.Schedule(() => clientListener.ReceiveMessage(userName, message));
                            }
                            else
                            {
                                clientListener.ReceiveMessage(userName, message);
                            }
                        }
                        break;
                    case 1:
                        {
                            var userName = PhotonSerializer.Deserialize<System.String>(__parameters[0]);
                            if(runOnMainThread)
                            {
                                CurrentThreadScheduler.Instance.Schedule(() => clientListener.JoinUser(userName));
                            }
                            else
                            {
                                clientListener.JoinUser(userName);
                            }
                        }
                        break;
                    case 2:
                        {
                            var userName = PhotonSerializer.Deserialize<System.String>(__parameters[0]);
                            if(runOnMainThread)
                            {
                                CurrentThreadScheduler.Instance.Schedule(() => clientListener.LeaveUser(userName));
                            }
                            else
                            {
                                clientListener.LeaveUser(userName);
                            }
                        }
                        break;
                    default:
                        break;
                }
            });                
        }

        public class ChatHubServer
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public ChatHubServer(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            public IObservable<System.String> CreateRoomAsync(System.String roomName, System.String userName, bool observeOnMainThread = true)
            {
                byte opCode = 0;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomName));
                parameter.Add(1, PhotonSerializer.Serialize(userName));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.String[]> GetRoomsAsync(bool observeOnMainThread = true)
            {
                byte opCode = 1;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.String[]> GetRoomMembersAsync(System.String roomId, bool observeOnMainThread = true)
            {
                byte opCode = 2;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<Unit> PublishMessageAsync(System.String roomId, System.String message, bool observeOnMainThread = true)
            {
                byte opCode = 3;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));
                parameter.Add(1, PhotonSerializer.Serialize(message));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<Unit> JoinRoomAsync(System.String roomId, System.String userName, bool observeOnMainThread = true)
            {
                byte opCode = 4;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));
                parameter.Add(1, PhotonSerializer.Serialize(userName));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<Unit> LeaveRoomAsync(System.String roomId, bool observeOnMainThread = true)
            {
                byte opCode = 5;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

        }

        public class ChatHubClient
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public ChatHubClient(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            IObservable<EventData> ReceiveEventData(byte eventCode)
            {
                return peer.ObserveReceiveEventData()
                    .Where(x =>
                    {
                        object hubIdObj;
                        if (!x.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                        {
                            return false;
                        }

                        if (x.Code != eventCode) return false;
                        if ((short)hubIdObj != hubId) return false;

                        return true;
                    });
            }

            public IObservable<ChatClientReceiveMessageResponse> ReceiveMessage(bool observeOnMainThread = true)
            {
                var __result = ReceiveEventData(0)
                    .Select(__args =>
                    {
                        var ____result = new ChatClientReceiveMessageResponse
                        {
                            userName = PhotonSerializer.Deserialize<System.String>(__args.Parameters[0]),
                            message = PhotonSerializer.Deserialize<System.String>(__args.Parameters[1]),
                        };
                        return ____result;
                    });

                return (observeOnMainThread) ? __result.ObserveOn(CurrentThreadScheduler.Instance) : __result;
            }

            public IObservable<System.String> JoinUser(bool observeOnMainThread = true)
            {
                var __result = ReceiveEventData(1)
                    .Select(__args =>
                    {
                        return PhotonSerializer.Deserialize<System.String>(__args.Parameters[0]);
                    });

                return (observeOnMainThread) ? __result.ObserveOn(CurrentThreadScheduler.Instance) : __result;
            }

            public IObservable<System.String> LeaveUser(bool observeOnMainThread = true)
            {
                var __result = ReceiveEventData(2)
                    .Select(__args =>
                    {
                        return PhotonSerializer.Deserialize<System.String>(__args.Parameters[0]);
                    });

                return (observeOnMainThread) ? __result.ObserveOn(CurrentThreadScheduler.Instance) : __result;
            }

        }

        public interface IChatClient
        {
            void ReceiveMessage(System.String userName, System.String message);
            void JoinUser(System.String userName);
            void LeaveUser(System.String userName);
        }

       
        public class ChatClientReceiveMessageResponse
        {
            public System.String userName { get; set; }
            public System.String message { get; set; }
        }

    }
    public class TutorialProxy : PhotonWireProxy<TutorialProxy.TutorialServer, TutorialProxy.TutorialClient, TutorialProxy.ITutorialClient>
    {
        public override short HubId
        {
            get
            {
                return 100;
            }
        }

        public override string HubName
        {
            get
            {
                return "Tutorial";
            }
        }

        TutorialServer invoke;
        public override TutorialServer Invoke
        {
            get
            {
                return invoke ?? (invoke = new TutorialServer(Peer, HubId));
            }
        }

        TutorialClient receive;
        public override TutorialClient Receive
        {
            get
            {
                return receive ?? (receive = new TutorialClient(Peer, HubId));
            }
        }
        
        public override IDisposable RegisterListener(ITutorialClient clientListener, bool runOnMainThread = true)
        {
            return Peer.ObserveReceiveEventData().Subscribe(__args =>
            {
                {
                    object hubIdObj;
                    if (!__args.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                    {
                        return;
                    }
                    if ((short)hubIdObj != HubId) return;
                }

                var __parameters = __args.Parameters;
                switch (__args.Code)
                {
                    case 0:
                        {
                            var message = PhotonSerializer.Deserialize<System.String>(__parameters[0]);
                            if(runOnMainThread)
                            {
                                CurrentThreadScheduler.Instance.Schedule(() => clientListener.GroupBroadcastMessage(message));
                            }
                            else
                            {
                                clientListener.GroupBroadcastMessage(message);
                            }
                        }
                        break;
                    default:
                        break;
                }
            });                
        }

        public class TutorialServer
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public TutorialServer(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            public IObservable<System.Int32> SumAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread = true)
            {
                byte opCode = 0;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));
                parameter.Add(1, PhotonSerializer.Serialize(y));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.String> GetHtmlAsync(System.String url, bool observeOnMainThread = true)
            {
                byte opCode = 1;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(url));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<Unit> BroadcastAllAsync(System.String message, bool observeOnMainThread = true)
            {
                byte opCode = 2;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(message));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<Unit> RegisterGroupAsync(System.String groupName, bool observeOnMainThread = true)
            {
                byte opCode = 3;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(groupName));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<Unit> BroadcastToAsync(System.String groupName, System.String message, bool observeOnMainThread = true)
            {
                byte opCode = 4;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(groupName));
                parameter.Add(1, PhotonSerializer.Serialize(message));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

            public IObservable<System.Int32> ServerToServerAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread = true)
            {
                byte opCode = 5;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));
                parameter.Add(1, PhotonSerializer.Serialize(y));

                var __response = peer.OpCustomAsync(opCode, parameter, true)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(CurrentThreadScheduler.Instance) : __response;
            }

        }

        public class TutorialClient
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public TutorialClient(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            IObservable<EventData> ReceiveEventData(byte eventCode)
            {
                return peer.ObserveReceiveEventData()
                    .Where(x =>
                    {
                        object hubIdObj;
                        if (!x.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                        {
                            return false;
                        }

                        if (x.Code != eventCode) return false;
                        if ((short)hubIdObj != hubId) return false;

                        return true;
                    });
            }

            public IObservable<System.String> GroupBroadcastMessage(bool observeOnMainThread = true)
            {
                var __result = ReceiveEventData(0)
                    .Select(__args =>
                    {
                        return PhotonSerializer.Deserialize<System.String>(__args.Parameters[0]);
                    });

                return (observeOnMainThread) ? __result.ObserveOn(CurrentThreadScheduler.Instance) : __result;
            }

        }

        public interface ITutorialClient
        {
            void GroupBroadcastMessage(System.String message);
        }

    }
}

namespace PhotonWire.Client.GeneratedSerializers
{
}


namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_Int32_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<int>> {
        
        private MsgPack.Serialization.MessagePackSerializer<int> _serializer0;
        
        public System_Nullable_1_System_Int32_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<int>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<int> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<int> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<int>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_Byte_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<byte>> {
        
        private MsgPack.Serialization.MessagePackSerializer<byte> _serializer0;
        
        public System_Nullable_1_System_Byte_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<byte>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<byte> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<byte> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<byte>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_Boolean_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<bool>> {
        
        private MsgPack.Serialization.MessagePackSerializer<bool> _serializer0;
        
        public System_Nullable_1_System_Boolean_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<bool>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<bool> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<bool> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<bool>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_Int16_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<short>> {
        
        private MsgPack.Serialization.MessagePackSerializer<short> _serializer0;
        
        public System_Nullable_1_System_Int16_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<short>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<short> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<short> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<short>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_Int64_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<long>> {
        
        private MsgPack.Serialization.MessagePackSerializer<long> _serializer0;
        
        public System_Nullable_1_System_Int64_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<long>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<long> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<long> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<long>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_Single_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<float>> {
        
        private MsgPack.Serialization.MessagePackSerializer<float> _serializer0;
        
        public System_Nullable_1_System_Single_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<float>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<float> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<float> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<float>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_Double_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<double>> {
        
        private MsgPack.Serialization.MessagePackSerializer<double> _serializer0;
        
        public System_Nullable_1_System_Double_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<double>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<double> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<double> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<double>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_System_DateTime_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<System.DateTime>> {
        
        private MsgPack.Serialization.MessagePackSerializer<System.DateTime> _serializer0;
        
        public System_Nullable_1_System_DateTime_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            this._serializer0 = context.GetSerializer<System.DateTime>(MsgPack.Serialization.DateTimeMessagePackSerializerHelpers.DetermineDateTimeConversionMethod(context, MsgPack.Serialization.DateTimeMemberConversionMethod.Default));
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<System.DateTime> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<System.DateTime> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<System.DateTime>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class PhotonWire_Sample_ServerApp_Hubs_MyClassSerializer : MsgPack.Serialization.MessagePackSerializer<PhotonWire.Sample.ServerApp.Hubs.MyClass> {
        
        private MsgPack.Serialization.MessagePackSerializer<int> _serializer0;
        
        private MsgPack.Serialization.MessagePackSerializer<string> _serializer1;
        
        private MsgPack.Serialization.MessagePackSerializer<PhotonWire.Sample.ServerApp.Hubs.MyClass2> _serializer2;
        
        public PhotonWire_Sample_ServerApp_Hubs_MyClassSerializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<int>(schema0);
            MsgPack.Serialization.PolymorphismSchema schema1 = default(MsgPack.Serialization.PolymorphismSchema);
            schema1 = null;
            this._serializer1 = context.GetSerializer<string>(schema1);
            MsgPack.Serialization.PolymorphismSchema schema2 = default(MsgPack.Serialization.PolymorphismSchema);
            schema2 = null;
            this._serializer2 = context.GetSerializer<PhotonWire.Sample.ServerApp.Hubs.MyClass2>(schema2);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, PhotonWire.Sample.ServerApp.Hubs.MyClass objectTree) {
            packer.PackArrayHeader(3);
            this._serializer0.PackTo(packer, objectTree.MyPropertyA);
            this._serializer1.PackTo(packer, objectTree.MyPropertyB);
            this._serializer2.PackTo(packer, objectTree.MyPropertyC);
        }
        
        protected override PhotonWire.Sample.ServerApp.Hubs.MyClass UnpackFromCore(MsgPack.Unpacker unpacker) {
            PhotonWire.Sample.ServerApp.Hubs.MyClass result = default(PhotonWire.Sample.ServerApp.Hubs.MyClass);
            result = new PhotonWire.Sample.ServerApp.Hubs.MyClass();
            if (unpacker.IsArrayHeader) {
                int unpacked = default(int);
                int itemsCount = default(int);
                itemsCount = MsgPack.Serialization.UnpackHelpers.GetItemsCount(unpacker);
                System.Nullable<int> nullable = default(System.Nullable<int>);
                if ((unpacked < itemsCount)) {
                    nullable = MsgPack.Serialization.UnpackHelpers.UnpackNullableInt32Value(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass), "Int32 MyPropertyA");
                }
                if (nullable.HasValue) {
                    result.MyPropertyA = nullable.Value;
                }
                unpacked = (unpacked + 1);
                string nullable0 = default(string);
                if ((unpacked < itemsCount)) {
                    nullable0 = MsgPack.Serialization.UnpackHelpers.UnpackStringValue(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass), "System.String MyPropertyB");
                }
                if (((nullable0 == null) 
                            == false)) {
                    result.MyPropertyB = nullable0;
                }
                unpacked = (unpacked + 1);
                PhotonWire.Sample.ServerApp.Hubs.MyClass2 nullable1 = default(PhotonWire.Sample.ServerApp.Hubs.MyClass2);
                if ((unpacked < itemsCount)) {
                    if ((unpacker.Read() == false)) {
                        throw MsgPack.Serialization.SerializationExceptions.NewMissingItem(2);
                    }
                    if (((unpacker.IsArrayHeader == false) 
                                && (unpacker.IsMapHeader == false))) {
                        nullable1 = this._serializer2.UnpackFrom(unpacker);
                    }
                    else {
                        MsgPack.Unpacker disposable = default(MsgPack.Unpacker);
                        disposable = unpacker.ReadSubtree();
                        try {
                            nullable1 = this._serializer2.UnpackFrom(disposable);
                        }
                        finally {
                            if (((disposable == null) 
                                        == false)) {
                                disposable.Dispose();
                            }
                        }
                    }
                }
                if (((nullable1 == null) 
                            == false)) {
                    result.MyPropertyC = nullable1;
                }
                unpacked = (unpacked + 1);
            }
            else {
                int itemsCount0 = default(int);
                itemsCount0 = MsgPack.Serialization.UnpackHelpers.GetItemsCount(unpacker);
                for (int i = 0; (i < itemsCount0); i = (i + 1)) {
                    string key = default(string);
                    string nullable2 = default(string);
                    nullable2 = MsgPack.Serialization.UnpackHelpers.UnpackStringValue(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass), "MemberName");
                    if (((nullable2 == null) 
                                == false)) {
                        key = nullable2;
                    }
                    else {
                        throw MsgPack.Serialization.SerializationExceptions.NewNullIsProhibited("MemberName");
                    }
                    if ((key == "MyPropertyC")) {
                        PhotonWire.Sample.ServerApp.Hubs.MyClass2 nullable5 = default(PhotonWire.Sample.ServerApp.Hubs.MyClass2);
                        if ((unpacker.Read() == false)) {
                            throw MsgPack.Serialization.SerializationExceptions.NewMissingItem(i);
                        }
                        if (((unpacker.IsArrayHeader == false) 
                                    && (unpacker.IsMapHeader == false))) {
                            nullable5 = this._serializer2.UnpackFrom(unpacker);
                        }
                        else {
                            MsgPack.Unpacker disposable0 = default(MsgPack.Unpacker);
                            disposable0 = unpacker.ReadSubtree();
                            try {
                                nullable5 = this._serializer2.UnpackFrom(disposable0);
                            }
                            finally {
                                if (((disposable0 == null) 
                                            == false)) {
                                    disposable0.Dispose();
                                }
                            }
                        }
                        if (((nullable5 == null) 
                                    == false)) {
                            result.MyPropertyC = nullable5;
                        }
                    }
                    else {
                        if ((key == "MyPropertyB")) {
                            string nullable4 = default(string);
                            nullable4 = MsgPack.Serialization.UnpackHelpers.UnpackStringValue(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass), "System.String MyPropertyB");
                            if (((nullable4 == null) 
                                        == false)) {
                                result.MyPropertyB = nullable4;
                            }
                        }
                        else {
                            if ((key == "MyPropertyA")) {
                                System.Nullable<int> nullable3 = default(System.Nullable<int>);
                                nullable3 = MsgPack.Serialization.UnpackHelpers.UnpackNullableInt32Value(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass), "Int32 MyPropertyA");
                                if (nullable3.HasValue) {
                                    result.MyPropertyA = nullable3.Value;
                                }
                            }
                            else {
                                unpacker.Skip();
                            }
                        }
                    }
                }
            }
            return result;
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class PhotonWire_Sample_ServerApp_Hubs_MyClass2Serializer : MsgPack.Serialization.MessagePackSerializer<PhotonWire.Sample.ServerApp.Hubs.MyClass2> {
        
        private MsgPack.Serialization.MessagePackSerializer<int> _serializer0;
        
        public PhotonWire_Sample_ServerApp_Hubs_MyClass2Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<int>(schema0);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, PhotonWire.Sample.ServerApp.Hubs.MyClass2 objectTree) {
            packer.PackArrayHeader(1);
            this._serializer0.PackTo(packer, objectTree.MyProperty);
        }
        
        protected override PhotonWire.Sample.ServerApp.Hubs.MyClass2 UnpackFromCore(MsgPack.Unpacker unpacker) {
            PhotonWire.Sample.ServerApp.Hubs.MyClass2 result = default(PhotonWire.Sample.ServerApp.Hubs.MyClass2);
            result = new PhotonWire.Sample.ServerApp.Hubs.MyClass2();
            if (unpacker.IsArrayHeader) {
                int unpacked = default(int);
                int itemsCount = default(int);
                itemsCount = MsgPack.Serialization.UnpackHelpers.GetItemsCount(unpacker);
                System.Nullable<int> nullable = default(System.Nullable<int>);
                if ((unpacked < itemsCount)) {
                    nullable = MsgPack.Serialization.UnpackHelpers.UnpackNullableInt32Value(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass2), "Int32 MyProperty");
                }
                if (nullable.HasValue) {
                    result.MyProperty = nullable.Value;
                }
                unpacked = (unpacked + 1);
            }
            else {
                int itemsCount0 = default(int);
                itemsCount0 = MsgPack.Serialization.UnpackHelpers.GetItemsCount(unpacker);
                for (int i = 0; (i < itemsCount0); i = (i + 1)) {
                    string key = default(string);
                    string nullable0 = default(string);
                    nullable0 = MsgPack.Serialization.UnpackHelpers.UnpackStringValue(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass2), "MemberName");
                    if (((nullable0 == null) 
                                == false)) {
                        key = nullable0;
                    }
                    else {
                        throw MsgPack.Serialization.SerializationExceptions.NewNullIsProhibited("MemberName");
                    }
                    if ((key == "MyProperty")) {
                        System.Nullable<int> nullable1 = default(System.Nullable<int>);
                        nullable1 = MsgPack.Serialization.UnpackHelpers.UnpackNullableInt32Value(unpacker, typeof(PhotonWire.Sample.ServerApp.Hubs.MyClass2), "Int32 MyProperty");
                        if (nullable1.HasValue) {
                            result.MyProperty = nullable1.Value;
                        }
                    }
                    else {
                        unpacker.Skip();
                    }
                }
            }
            return result;
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class PhotonWire_Sample_ServerApp_Hubs_YoSerializer : MsgPack.Serialization.EnumMessagePackSerializer<PhotonWire.Sample.ServerApp.Hubs.Yo> {
        
        public PhotonWire_Sample_ServerApp_Hubs_YoSerializer(MsgPack.Serialization.SerializationContext context) : 
                this(context, MsgPack.Serialization.EnumSerializationMethod.ByUnderlyingValue) {
        }
        
        public PhotonWire_Sample_ServerApp_Hubs_YoSerializer(MsgPack.Serialization.SerializationContext context, MsgPack.Serialization.EnumSerializationMethod enumSerializationMethod) : 
                base(context, enumSerializationMethod) {
        }
        
        protected override void PackUnderlyingValueTo(MsgPack.Packer packer, PhotonWire.Sample.ServerApp.Hubs.Yo enumValue) {
            packer.Pack(((int)(enumValue)));
        }
        
        protected override PhotonWire.Sample.ServerApp.Hubs.Yo UnpackFromUnderlyingValue(MsgPack.MessagePackObject messagePackObject) {
            return ((PhotonWire.Sample.ServerApp.Hubs.Yo)(messagePackObject.AsInt32()));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

namespace PhotonWire.Client.GeneratedSerializers {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("MsgPack.Serialization.CodeDomSerializers.CodeDomSerializerBuilder", "0.6.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    public class System_Nullable_1_PhotonWire_Sample_ServerApp_Hubs_Yo_Serializer : MsgPack.Serialization.MessagePackSerializer<System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo>> {
        
        private MsgPack.Serialization.MessagePackSerializer<PhotonWire.Sample.ServerApp.Hubs.Yo> _serializer0;
        
        public System_Nullable_1_PhotonWire_Sample_ServerApp_Hubs_Yo_Serializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            this._serializer0 = context.GetSerializer<PhotonWire.Sample.ServerApp.Hubs.Yo>(MsgPack.Serialization.EnumMessagePackSerializerHelpers.DetermineEnumSerializationMethod(context, typeof(PhotonWire.Sample.ServerApp.Hubs.Yo), MsgPack.Serialization.EnumMemberSerializationMethod.Default));
        }
        
        protected override void PackToCore(MsgPack.Packer packer, System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> objectTree) {
            this._serializer0.PackTo(packer, objectTree.Value);
        }
        
        protected override System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> UnpackFromCore(MsgPack.Unpacker unpacker) {
            return new System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo>(this._serializer0.UnpackFrom(unpacker));
        }
        
        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
}

#pragma warning disable 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612


