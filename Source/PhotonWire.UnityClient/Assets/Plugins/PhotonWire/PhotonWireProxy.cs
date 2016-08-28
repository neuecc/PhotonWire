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
using System.Reactive.Subjects;
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
        public abstract short HubId { get; }
        public abstract string HubName { get; }
        public ObservablePhotonPeer Peer { get; private set; }
        public TServer Invoke { get; private set; }
        public TClient Receive { get; private set; }
        public TClientListener Publish { get; private set; }

        public void Initialize(ObservablePhotonPeer peer)
        {
            this.Peer = peer;
            
            TServer invoke;
            TClient client;
            TClientListener publish; 
            Initialize(out invoke, out client, out publish);

            Invoke = invoke;
            Receive = client;
            Publish = publish;
        }

        protected abstract void Initialize(out TServer invoke, out TClient client, out TClientListener publisher);

        /// <summary>Register broadcast event listener.(Note:can not attach filter)</summary>
        public abstract IDisposable RegisterListener(TClientListener clientListener, bool runOnMainThread = true);

        public void AttachInvokeFilter(Func<TServer, TServer> serverFilterFactory)
        {
            Invoke = serverFilterFactory(Invoke);
        }

        public void AttachReceiveFilter(Func<TClient, TClient> clientFilterFactory)
        {
            Receive = clientFilterFactory(Receive);
        }

        public void AttachFilter(Func<TServer, TServer> serverFilterFactory, Func<TClient, TClient> clientFilterFactory)
        {
            Invoke = serverFilterFactory(Invoke);
            Receive = clientFilterFactory(Receive);
        }
    }

    // Auto generated proxy code
    public class ForUnitTestProxy : PhotonWireProxy<ForUnitTestProxy.IForUnitTestServerInvoker, ForUnitTestProxy.IForUnitTestClientReceiver, ForUnitTestProxy.INoClient>
    {
        static object[] EmptyArray = new object[0];
        static Tuple<byte, object[]> NullTuple = Tuple.Create((byte)0, (object[])null);
        IObservable<Tuple<byte, object[]>> receiver = null;

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

        protected override void Initialize(out IForUnitTestServerInvoker invoke, out IForUnitTestClientReceiver client, out INoClient publisher)
        {
            invoke = new ForUnitTestServerInvoker(Peer, HubId);

            var rawPublisher = new Subject<Tuple<byte, object[]>>();
            receiver = Peer.ObserveReceiveEventData()
                .Select(eventData =>
                {
                    object hubIdObj;
                    if (!eventData.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                    {
                        return NullTuple;
                    }
                    if ((short)hubIdObj != HubId) return NullTuple;

                    switch (eventData.Code)
                    {
                        default:
                            return NullTuple;
                    }
                })
                .Where(x => x.Item2 != null)
                .Multicast(rawPublisher)
                .RefCount();


            var r = new ForUnitTestClientReceiver(receiver, rawPublisher);
            client = r;
            publisher = r;
        }
        
        public override IDisposable RegisterListener(INoClient clientListener, bool runOnMainThread = true)
        {
            return receiver.Subscribe(__args =>
            {
                switch (__args.Item1)
                {
                    default:
                        break;
                }
            });                
        }

        public new ForUnitTestProxy AttachInvokeFilter(Func<IForUnitTestServerInvoker, IForUnitTestServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(serverFilterFactory);
            return this;
        }

        public ForUnitTestProxy AttachInvokeFilter(Func<ForUnitTestProxy, IForUnitTestServerInvoker, IForUnitTestServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(x => serverFilterFactory(this, x));
            return this;
        }

        public new ForUnitTestProxy AttachReceiveFilter(Func<IForUnitTestClientReceiver, IForUnitTestClientReceiver> clientFilterFactory)
        {
            base.AttachReceiveFilter(clientFilterFactory);
            return this;
        }

        public new ForUnitTestProxy AttachFilter(Func<IForUnitTestServerInvoker, IForUnitTestServerInvoker> serverFilterFactory, Func<IForUnitTestClientReceiver, IForUnitTestClientReceiver> clientFilterFactory)
        {
            base.AttachFilter(serverFilterFactory, clientFilterFactory);
            return this;
        }

        public interface IForUnitTestServerInvoker
        {
            IObservable<System.Int32> EchoAsync(System.Int32 x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Byte> EchoAsync(System.Byte x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Boolean> EchoAsync(System.Boolean x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int16> EchoAsync(System.Int16 x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int64> EchoAsync(System.Int64 x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Single> EchoAsync(System.Single x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Double> EchoAsync(System.Double x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int32[]> EchoAsync(System.Int32[] x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String> EchoAsync(System.String x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Byte[]> EchoAsync(System.Byte[] x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.DateTime> EchoAsync(System.DateTime x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Uri> EchoAsync(System.Uri x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Nullable<System.Int32>> EchoAsync(System.Nullable<System.Int32> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Nullable<System.Double>> EchoAsync(System.Nullable<System.Double> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Double[]> EchoAsync(System.Double[] x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Collections.Generic.List<System.Double>> EchoAsync(System.Collections.Generic.List<System.Double> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Collections.Generic.Dictionary<System.String, System.Int32>> EchoAsync(System.Collections.Generic.Dictionary<System.String, System.Int32> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<PhotonWire.Sample.ServerApp.Hubs.MyClass> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.MyClass x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.Yo yo, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String> EchoAsync(System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> yo, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int32> Echo2Async(System.Int32 x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Byte> Echo2Async(System.Byte x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Boolean> Echo2Async(System.Boolean x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int16> Echo2Async(System.Int16 x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int64> Echo2Async(System.Int64 x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Single> Echo2Async(System.Single x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Double> Echo2Async(System.Double x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int32[]> Echo2Async(System.Int32[] x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String> Echo2Async(System.String x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Byte[]> Echo2Async(System.Byte[] x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.DateTime> Echo2Async(System.DateTime x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Uri> Echo2Async(System.Uri x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Nullable<System.Int32>> Echo2Async(System.Nullable<System.Int32> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Nullable<System.Double>> Echo2Async(System.Nullable<System.Double> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Double[]> Echo2Async(System.Double[] x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Collections.Generic.List<System.Double>> Echo2Async(System.Collections.Generic.List<System.Double> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Collections.Generic.Dictionary<System.String, System.Int32>> Echo2Async(System.Collections.Generic.Dictionary<System.String, System.Int32> x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<PhotonWire.Sample.ServerApp.Hubs.MyClass> Echo2Async(PhotonWire.Sample.ServerApp.Hubs.MyClass x, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String> Echo2Async(PhotonWire.Sample.ServerApp.Hubs.Yo yo, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String> Echo2Async(System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> yo, bool observeOnMainThread = true, bool encrypt = false);
        }

        public class DelegatingForUnitTestServerInvoker : IForUnitTestServerInvoker
        {
            readonly IForUnitTestServerInvoker parent;

            public DelegatingForUnitTestServerInvoker(IForUnitTestServerInvoker parent)
            {
                this.parent = parent;
            }

            public virtual IObservable<System.Int32> EchoAsync(System.Int32 x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Byte> EchoAsync(System.Byte x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Boolean> EchoAsync(System.Boolean x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int16> EchoAsync(System.Int16 x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int64> EchoAsync(System.Int64 x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Single> EchoAsync(System.Single x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Double> EchoAsync(System.Double x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int32[]> EchoAsync(System.Int32[] x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String> EchoAsync(System.String x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Byte[]> EchoAsync(System.Byte[] x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.DateTime> EchoAsync(System.DateTime x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Uri> EchoAsync(System.Uri x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Nullable<System.Int32>> EchoAsync(System.Nullable<System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Nullable<System.Double>> EchoAsync(System.Nullable<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Double[]> EchoAsync(System.Double[] x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Collections.Generic.List<System.Double>> EchoAsync(System.Collections.Generic.List<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Collections.Generic.Dictionary<System.String, System.Int32>> EchoAsync(System.Collections.Generic.Dictionary<System.String, System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<PhotonWire.Sample.ServerApp.Hubs.MyClass> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.MyClass x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.Yo yo, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( yo,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String> EchoAsync(System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> yo, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.EchoAsync( yo,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int32> Echo2Async(System.Int32 x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Byte> Echo2Async(System.Byte x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Boolean> Echo2Async(System.Boolean x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int16> Echo2Async(System.Int16 x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int64> Echo2Async(System.Int64 x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Single> Echo2Async(System.Single x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Double> Echo2Async(System.Double x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int32[]> Echo2Async(System.Int32[] x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String> Echo2Async(System.String x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Byte[]> Echo2Async(System.Byte[] x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.DateTime> Echo2Async(System.DateTime x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Uri> Echo2Async(System.Uri x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Nullable<System.Int32>> Echo2Async(System.Nullable<System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Nullable<System.Double>> Echo2Async(System.Nullable<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Double[]> Echo2Async(System.Double[] x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Collections.Generic.List<System.Double>> Echo2Async(System.Collections.Generic.List<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Collections.Generic.Dictionary<System.String, System.Int32>> Echo2Async(System.Collections.Generic.Dictionary<System.String, System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<PhotonWire.Sample.ServerApp.Hubs.MyClass> Echo2Async(PhotonWire.Sample.ServerApp.Hubs.MyClass x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( x,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String> Echo2Async(PhotonWire.Sample.ServerApp.Hubs.Yo yo, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( yo,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String> Echo2Async(System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> yo, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.Echo2Async( yo,  observeOnMainThread, encrypt);
            }

        }

        public class ForUnitTestServerInvoker : IForUnitTestServerInvoker
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public ForUnitTestServerInvoker(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            public IObservable<System.Int32> EchoAsync(System.Int32 x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 0;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Byte> EchoAsync(System.Byte x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 1;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Byte>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Boolean> EchoAsync(System.Boolean x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 2;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Boolean>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int16> EchoAsync(System.Int16 x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 3;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int16>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int64> EchoAsync(System.Int64 x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 4;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int64>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Single> EchoAsync(System.Single x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 5;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Single>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Double> EchoAsync(System.Double x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 6;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Double>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int32[]> EchoAsync(System.Int32[] x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 7;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String> EchoAsync(System.String x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 8;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Byte[]> EchoAsync(System.Byte[] x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 9;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Byte[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.DateTime> EchoAsync(System.DateTime x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 10;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.DateTime>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Uri> EchoAsync(System.Uri x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 11;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Uri>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Nullable<System.Int32>> EchoAsync(System.Nullable<System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 12;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Nullable<System.Int32>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Nullable<System.Double>> EchoAsync(System.Nullable<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 13;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Nullable<System.Double>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Double[]> EchoAsync(System.Double[] x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 14;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Double[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Collections.Generic.List<System.Double>> EchoAsync(System.Collections.Generic.List<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 15;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Collections.Generic.List<System.Double>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Collections.Generic.Dictionary<System.String, System.Int32>> EchoAsync(System.Collections.Generic.Dictionary<System.String, System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 16;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Collections.Generic.Dictionary<System.String, System.Int32>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<PhotonWire.Sample.ServerApp.Hubs.MyClass> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.MyClass x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 17;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<PhotonWire.Sample.ServerApp.Hubs.MyClass>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String> EchoAsync(PhotonWire.Sample.ServerApp.Hubs.Yo yo, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 18;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(yo));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String> EchoAsync(System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> yo, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 19;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(yo));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int32> Echo2Async(System.Int32 x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 20;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Byte> Echo2Async(System.Byte x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 21;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Byte>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Boolean> Echo2Async(System.Boolean x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 22;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Boolean>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int16> Echo2Async(System.Int16 x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 23;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int16>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int64> Echo2Async(System.Int64 x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 24;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int64>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Single> Echo2Async(System.Single x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 25;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Single>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Double> Echo2Async(System.Double x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 26;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Double>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int32[]> Echo2Async(System.Int32[] x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 27;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String> Echo2Async(System.String x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 28;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Byte[]> Echo2Async(System.Byte[] x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 29;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Byte[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.DateTime> Echo2Async(System.DateTime x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 30;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.DateTime>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Uri> Echo2Async(System.Uri x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 31;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Uri>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Nullable<System.Int32>> Echo2Async(System.Nullable<System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 32;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Nullable<System.Int32>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Nullable<System.Double>> Echo2Async(System.Nullable<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 33;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Nullable<System.Double>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Double[]> Echo2Async(System.Double[] x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 34;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Double[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Collections.Generic.List<System.Double>> Echo2Async(System.Collections.Generic.List<System.Double> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 35;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Collections.Generic.List<System.Double>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Collections.Generic.Dictionary<System.String, System.Int32>> Echo2Async(System.Collections.Generic.Dictionary<System.String, System.Int32> x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 36;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Collections.Generic.Dictionary<System.String, System.Int32>>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<PhotonWire.Sample.ServerApp.Hubs.MyClass> Echo2Async(PhotonWire.Sample.ServerApp.Hubs.MyClass x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 37;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<PhotonWire.Sample.ServerApp.Hubs.MyClass>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String> Echo2Async(PhotonWire.Sample.ServerApp.Hubs.Yo yo, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 38;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(yo));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String> Echo2Async(System.Nullable<PhotonWire.Sample.ServerApp.Hubs.Yo> yo, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 39;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(yo));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

        }

        public interface IForUnitTestClientReceiver
        {
        }
        
        public class DelegatingForUnitTestClientReceiver : IForUnitTestClientReceiver
        {
            readonly IForUnitTestClientReceiver parent;

            public DelegatingForUnitTestClientReceiver(IForUnitTestClientReceiver parent)
            {
                this.parent = parent;
            }

        }


        public class ForUnitTestClientReceiver : IForUnitTestClientReceiver, INoClient
        {
            readonly IObservable<Tuple<byte, object[]>> receiver;
            readonly IObserver<Tuple<byte, object[]>> __publisher;
            static readonly object[] EmptyArray = new object[0];

            public ForUnitTestClientReceiver(IObservable<Tuple<byte, object[]>> receiver, IObserver<Tuple<byte, object[]>> publisher)
            {
                this.receiver = receiver;
                this.__publisher = publisher;
            }

        }

        public interface INoClient
        {
        }

    }
    public class ChatHubProxy : PhotonWireProxy<ChatHubProxy.IChatHubServerInvoker, ChatHubProxy.IChatHubClientReceiver, ChatHubProxy.IChatClient>
    {
        static object[] EmptyArray = new object[0];
        static Tuple<byte, object[]> NullTuple = Tuple.Create((byte)0, (object[])null);
        IObservable<Tuple<byte, object[]>> receiver = null;

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

        protected override void Initialize(out IChatHubServerInvoker invoke, out IChatHubClientReceiver client, out IChatClient publisher)
        {
            invoke = new ChatHubServerInvoker(Peer, HubId);

            var rawPublisher = new Subject<Tuple<byte, object[]>>();
            receiver = Peer.ObserveReceiveEventData()
                .Select(eventData =>
                {
                    object hubIdObj;
                    if (!eventData.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                    {
                        return NullTuple;
                    }
                    if ((short)hubIdObj != HubId) return NullTuple;

                    switch (eventData.Code)
                    {
                        case 0:
                            return Tuple.Create((byte)0, 
                                new object[]
                                {
                                    (object)PhotonSerializer.Deserialize<System.String>(eventData.Parameters[0]),
                                    (object)PhotonSerializer.Deserialize<System.String>(eventData.Parameters[1]),
                                }
                            );
                        case 1:
                            return Tuple.Create((byte)1, 
                                new object[] { (object)PhotonSerializer.Deserialize<System.String>(eventData.Parameters[0]) }
                            );
                        case 2:
                            return Tuple.Create((byte)2, 
                                new object[] { (object)PhotonSerializer.Deserialize<System.String>(eventData.Parameters[0]) }
                            );
                        default:
                            return NullTuple;
                    }
                })
                .Where(x => x.Item2 != null)
                .Multicast(rawPublisher)
                .RefCount();


            var r = new ChatHubClientReceiver(receiver, rawPublisher);
            client = r;
            publisher = r;
        }
        
        public override IDisposable RegisterListener(IChatClient clientListener, bool runOnMainThread = true)
        {
            return receiver.Subscribe(__args =>
            {
                switch (__args.Item1)
                {
                    case 0:
                        {
                            var userName = (System.String)(__args.Item2[0]);
                            var message = (System.String)(__args.Item2[1]);
                            if(runOnMainThread)
                            {
                                Scheduler.MainThread.Schedule(() => clientListener.ReceiveMessage(userName, message));
                            }
                            else
                            {
                                clientListener.ReceiveMessage(userName, message);
                            }
                        }
                        break;
                    case 1:
                        {
                            var userName = (System.String)(__args.Item2[0]);
                            if(runOnMainThread)
                            {
                                Scheduler.MainThread.Schedule(() => clientListener.JoinUser(userName));
                            }
                            else
                            {
                                clientListener.JoinUser(userName);
                            }
                        }
                        break;
                    case 2:
                        {
                            var userName = (System.String)(__args.Item2[0]);
                            if(runOnMainThread)
                            {
                                Scheduler.MainThread.Schedule(() => clientListener.LeaveUser(userName));
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

        public new ChatHubProxy AttachInvokeFilter(Func<IChatHubServerInvoker, IChatHubServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(serverFilterFactory);
            return this;
        }

        public ChatHubProxy AttachInvokeFilter(Func<ChatHubProxy, IChatHubServerInvoker, IChatHubServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(x => serverFilterFactory(this, x));
            return this;
        }

        public new ChatHubProxy AttachReceiveFilter(Func<IChatHubClientReceiver, IChatHubClientReceiver> clientFilterFactory)
        {
            base.AttachReceiveFilter(clientFilterFactory);
            return this;
        }

        public new ChatHubProxy AttachFilter(Func<IChatHubServerInvoker, IChatHubServerInvoker> serverFilterFactory, Func<IChatHubClientReceiver, IChatHubClientReceiver> clientFilterFactory)
        {
            base.AttachFilter(serverFilterFactory, clientFilterFactory);
            return this;
        }

        public interface IChatHubServerInvoker
        {
            IObservable<System.String> CreateRoomAsync(System.String roomName, System.String userName, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String[]> GetRoomsAsync(bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String[]> GetRoomMembersAsync(System.String roomId, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<Unit> PublishMessageAsync(System.String roomId, System.String message, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<Unit> JoinRoomAsync(System.String roomId, System.String userName, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<Unit> LeaveRoomAsync(System.String roomId, bool observeOnMainThread = true, bool encrypt = false);
        }

        public class DelegatingChatHubServerInvoker : IChatHubServerInvoker
        {
            readonly IChatHubServerInvoker parent;

            public DelegatingChatHubServerInvoker(IChatHubServerInvoker parent)
            {
                this.parent = parent;
            }

            public virtual IObservable<System.String> CreateRoomAsync(System.String roomName, System.String userName, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.CreateRoomAsync( roomName, userName,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String[]> GetRoomsAsync(bool observeOnMainThread, bool encrypt)
            {
                return this.parent.GetRoomsAsync(  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String[]> GetRoomMembersAsync(System.String roomId, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.GetRoomMembersAsync( roomId,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<Unit> PublishMessageAsync(System.String roomId, System.String message, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.PublishMessageAsync( roomId, message,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<Unit> JoinRoomAsync(System.String roomId, System.String userName, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.JoinRoomAsync( roomId, userName,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<Unit> LeaveRoomAsync(System.String roomId, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.LeaveRoomAsync( roomId,  observeOnMainThread, encrypt);
            }

        }

        public class ChatHubServerInvoker : IChatHubServerInvoker
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public ChatHubServerInvoker(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            public IObservable<System.String> CreateRoomAsync(System.String roomName, System.String userName, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 0;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomName));
                parameter.Add(1, PhotonSerializer.Serialize(userName));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String[]> GetRoomsAsync(bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 1;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String[]> GetRoomMembersAsync(System.String roomId, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 2;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String[]>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<Unit> PublishMessageAsync(System.String roomId, System.String message, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 3;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));
                parameter.Add(1, PhotonSerializer.Serialize(message));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<Unit> JoinRoomAsync(System.String roomId, System.String userName, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 4;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));
                parameter.Add(1, PhotonSerializer.Serialize(userName));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<Unit> LeaveRoomAsync(System.String roomId, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 5;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(roomId));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

        }

        public interface IChatHubClientReceiver
        {
            IObservable<ChatClientReceiveMessageResponse> ReceiveMessage(bool observeOnMainThread = true);
            IObservable<System.String> JoinUser(bool observeOnMainThread = true);
            IObservable<System.String> LeaveUser(bool observeOnMainThread = true);
        }
        
        public class DelegatingChatHubClientReceiver : IChatHubClientReceiver
        {
            readonly IChatHubClientReceiver parent;

            public DelegatingChatHubClientReceiver(IChatHubClientReceiver parent)
            {
                this.parent = parent;
            }

            public virtual IObservable<ChatClientReceiveMessageResponse> ReceiveMessage(bool observeOnMainThread)
            {
                return this.parent.ReceiveMessage(observeOnMainThread);
            }

            public virtual IObservable<System.String> JoinUser(bool observeOnMainThread)
            {
                return this.parent.JoinUser(observeOnMainThread);
            }

            public virtual IObservable<System.String> LeaveUser(bool observeOnMainThread)
            {
                return this.parent.LeaveUser(observeOnMainThread);
            }

        }


        public class ChatHubClientReceiver : IChatHubClientReceiver, IChatClient
        {
            readonly IObservable<Tuple<byte, object[]>> receiver;
            readonly IObserver<Tuple<byte, object[]>> __publisher;
            static readonly object[] EmptyArray = new object[0];

            public ChatHubClientReceiver(IObservable<Tuple<byte, object[]>> receiver, IObserver<Tuple<byte, object[]>> publisher)
            {
                this.receiver = receiver;
                this.__publisher = publisher;
            }

            public IObservable<ChatClientReceiveMessageResponse> ReceiveMessage(bool observeOnMainThread)
            {
                var __result = receiver
                    .Where(__args => __args.Item1 == 0)
                    .Select(__args =>
                    {
                        var ____result = new ChatClientReceiveMessageResponse
                        {
                            userName = (System.String)(__args.Item2[0]),
                            message = (System.String)(__args.Item2[1]),
                        };
                        return ____result;
                    });

                return (observeOnMainThread) ? __result.ObserveOn(Scheduler.MainThread) : __result;
            }

            void IChatClient.ReceiveMessage(System.String userName, System.String message)
            {
                __publisher.OnNext(Tuple.Create((byte)0, 
                    new object[]
                    {
                        (object)userName,
                        (object)message,
                    }
                ));
            }

            public IObservable<System.String> JoinUser(bool observeOnMainThread)
            {
                var __result = receiver
                    .Where(__args => __args.Item1 == 1)
                    .Select(__args =>
                    {
                        return (System.String)(__args.Item2[0]);
                    });

                return (observeOnMainThread) ? __result.ObserveOn(Scheduler.MainThread) : __result;
            }

            void IChatClient.JoinUser(System.String userName)
            {
                __publisher.OnNext(Tuple.Create((byte)1, 
                    new object[] { (object)userName }
                ));
            }

            public IObservable<System.String> LeaveUser(bool observeOnMainThread)
            {
                var __result = receiver
                    .Where(__args => __args.Item1 == 2)
                    .Select(__args =>
                    {
                        return (System.String)(__args.Item2[0]);
                    });

                return (observeOnMainThread) ? __result.ObserveOn(Scheduler.MainThread) : __result;
            }

            void IChatClient.LeaveUser(System.String userName)
            {
                __publisher.OnNext(Tuple.Create((byte)2, 
                    new object[] { (object)userName }
                ));
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
    public class SimpleHubProxy : PhotonWireProxy<SimpleHubProxy.ISimpleHubServerInvoker, SimpleHubProxy.ISimpleHubClientReceiver, SimpleHubProxy.ISimpleHubClient>
    {
        static object[] EmptyArray = new object[0];
        static Tuple<byte, object[]> NullTuple = Tuple.Create((byte)0, (object[])null);
        IObservable<Tuple<byte, object[]>> receiver = null;

        public override short HubId
        {
            get
            {
                return 9932;
            }
        }

        public override string HubName
        {
            get
            {
                return "SimpleHub";
            }
        }

        protected override void Initialize(out ISimpleHubServerInvoker invoke, out ISimpleHubClientReceiver client, out ISimpleHubClient publisher)
        {
            invoke = new SimpleHubServerInvoker(Peer, HubId);

            var rawPublisher = new Subject<Tuple<byte, object[]>>();
            receiver = Peer.ObserveReceiveEventData()
                .Select(eventData =>
                {
                    object hubIdObj;
                    if (!eventData.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                    {
                        return NullTuple;
                    }
                    if ((short)hubIdObj != HubId) return NullTuple;

                    switch (eventData.Code)
                    {
                        case 0:
                            return Tuple.Create((byte)0, 
                                new object[]
                                {
                                    (object)PhotonSerializer.Deserialize<System.Int32>(eventData.Parameters[0]),
                                    (object)PhotonSerializer.Deserialize<System.Int32>(eventData.Parameters[1]),
                                }
                            );
                        case 1:
                            return Tuple.Create((byte)1, 
                                EmptyArray
                            );
                        case 2:
                            return Tuple.Create((byte)2, 
                                new object[] { (object)PhotonSerializer.Deserialize<System.Int32>(eventData.Parameters[0]) }
                            );
                        default:
                            return NullTuple;
                    }
                })
                .Where(x => x.Item2 != null)
                .Multicast(rawPublisher)
                .RefCount();


            var r = new SimpleHubClientReceiver(receiver, rawPublisher);
            client = r;
            publisher = r;
        }
        
        public override IDisposable RegisterListener(ISimpleHubClient clientListener, bool runOnMainThread = true)
        {
            return receiver.Subscribe(__args =>
            {
                switch (__args.Item1)
                {
                    case 0:
                        {
                            var x = (System.Int32)(__args.Item2[0]);
                            var y = (System.Int32)(__args.Item2[1]);
                            if(runOnMainThread)
                            {
                                Scheduler.MainThread.Schedule(() => clientListener.ToClient(x, y));
                            }
                            else
                            {
                                clientListener.ToClient(x, y);
                            }
                        }
                        break;
                    case 1:
                        {
                            if(runOnMainThread)
                            {
                                Scheduler.MainThread.Schedule(() => clientListener.Blank());
                            }
                            else
                            {
                                clientListener.Blank();
                            }
                        }
                        break;
                    case 2:
                        {
                            var z = (System.Int32)(__args.Item2[0]);
                            if(runOnMainThread)
                            {
                                Scheduler.MainThread.Schedule(() => clientListener.Single(z));
                            }
                            else
                            {
                                clientListener.Single(z);
                            }
                        }
                        break;
                    default:
                        break;
                }
            });                
        }

        public new SimpleHubProxy AttachInvokeFilter(Func<ISimpleHubServerInvoker, ISimpleHubServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(serverFilterFactory);
            return this;
        }

        public SimpleHubProxy AttachInvokeFilter(Func<SimpleHubProxy, ISimpleHubServerInvoker, ISimpleHubServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(x => serverFilterFactory(this, x));
            return this;
        }

        public new SimpleHubProxy AttachReceiveFilter(Func<ISimpleHubClientReceiver, ISimpleHubClientReceiver> clientFilterFactory)
        {
            base.AttachReceiveFilter(clientFilterFactory);
            return this;
        }

        public new SimpleHubProxy AttachFilter(Func<ISimpleHubServerInvoker, ISimpleHubServerInvoker> serverFilterFactory, Func<ISimpleHubClientReceiver, ISimpleHubClientReceiver> clientFilterFactory)
        {
            base.AttachFilter(serverFilterFactory, clientFilterFactory);
            return this;
        }

        public interface ISimpleHubServerInvoker
        {
            IObservable<System.String> HogeAsync(System.Int32 x, bool observeOnMainThread = true, bool encrypt = false);
        }

        public class DelegatingSimpleHubServerInvoker : ISimpleHubServerInvoker
        {
            readonly ISimpleHubServerInvoker parent;

            public DelegatingSimpleHubServerInvoker(ISimpleHubServerInvoker parent)
            {
                this.parent = parent;
            }

            public virtual IObservable<System.String> HogeAsync(System.Int32 x, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.HogeAsync( x,  observeOnMainThread, encrypt);
            }

        }

        public class SimpleHubServerInvoker : ISimpleHubServerInvoker
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public SimpleHubServerInvoker(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            public IObservable<System.String> HogeAsync(System.Int32 x, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 0;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

        }

        public interface ISimpleHubClientReceiver
        {
            IObservable<SimpleHubClientToClientResponse> ToClient(bool observeOnMainThread = true);
            IObservable<Unit> Blank(bool observeOnMainThread = true);
            IObservable<System.Int32> Single(bool observeOnMainThread = true);
        }
        
        public class DelegatingSimpleHubClientReceiver : ISimpleHubClientReceiver
        {
            readonly ISimpleHubClientReceiver parent;

            public DelegatingSimpleHubClientReceiver(ISimpleHubClientReceiver parent)
            {
                this.parent = parent;
            }

            public virtual IObservable<SimpleHubClientToClientResponse> ToClient(bool observeOnMainThread)
            {
                return this.parent.ToClient(observeOnMainThread);
            }

            public virtual IObservable<Unit> Blank(bool observeOnMainThread)
            {
                return this.parent.Blank(observeOnMainThread);
            }

            public virtual IObservable<System.Int32> Single(bool observeOnMainThread)
            {
                return this.parent.Single(observeOnMainThread);
            }

        }


        public class SimpleHubClientReceiver : ISimpleHubClientReceiver, ISimpleHubClient
        {
            readonly IObservable<Tuple<byte, object[]>> receiver;
            readonly IObserver<Tuple<byte, object[]>> __publisher;
            static readonly object[] EmptyArray = new object[0];

            public SimpleHubClientReceiver(IObservable<Tuple<byte, object[]>> receiver, IObserver<Tuple<byte, object[]>> publisher)
            {
                this.receiver = receiver;
                this.__publisher = publisher;
            }

            public IObservable<SimpleHubClientToClientResponse> ToClient(bool observeOnMainThread)
            {
                var __result = receiver
                    .Where(__args => __args.Item1 == 0)
                    .Select(__args =>
                    {
                        var ____result = new SimpleHubClientToClientResponse
                        {
                            x = (System.Int32)(__args.Item2[0]),
                            y = (System.Int32)(__args.Item2[1]),
                        };
                        return ____result;
                    });

                return (observeOnMainThread) ? __result.ObserveOn(Scheduler.MainThread) : __result;
            }

            void ISimpleHubClient.ToClient(System.Int32 x, System.Int32 y)
            {
                __publisher.OnNext(Tuple.Create((byte)0, 
                    new object[]
                    {
                        (object)x,
                        (object)y,
                    }
                ));
            }

            public IObservable<Unit> Blank(bool observeOnMainThread)
            {
                var __result = receiver
                    .Where(__args => __args.Item1 == 1)
                    .Select(__args =>
                    {
                        return Unit.Default;
                    });

                return (observeOnMainThread) ? __result.ObserveOn(Scheduler.MainThread) : __result;
            }

            void ISimpleHubClient.Blank()
            {
                __publisher.OnNext(Tuple.Create((byte)1, 
                    EmptyArray
                ));
            }

            public IObservable<System.Int32> Single(bool observeOnMainThread)
            {
                var __result = receiver
                    .Where(__args => __args.Item1 == 2)
                    .Select(__args =>
                    {
                        return (System.Int32)(__args.Item2[0]);
                    });

                return (observeOnMainThread) ? __result.ObserveOn(Scheduler.MainThread) : __result;
            }

            void ISimpleHubClient.Single(System.Int32 z)
            {
                __publisher.OnNext(Tuple.Create((byte)2, 
                    new object[] { (object)z }
                ));
            }

        }

        public interface ISimpleHubClient
        {
            void ToClient(System.Int32 x, System.Int32 y);
            void Blank();
            void Single(System.Int32 z);
        }

       
        public class SimpleHubClientToClientResponse
        {
            public System.Int32 x { get; set; }
            public System.Int32 y { get; set; }
        }

    }
    public class TutorialProxy : PhotonWireProxy<TutorialProxy.ITutorialServerInvoker, TutorialProxy.ITutorialClientReceiver, TutorialProxy.ITutorialClient>
    {
        static object[] EmptyArray = new object[0];
        static Tuple<byte, object[]> NullTuple = Tuple.Create((byte)0, (object[])null);
        IObservable<Tuple<byte, object[]>> receiver = null;

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

        protected override void Initialize(out ITutorialServerInvoker invoke, out ITutorialClientReceiver client, out ITutorialClient publisher)
        {
            invoke = new TutorialServerInvoker(Peer, HubId);

            var rawPublisher = new Subject<Tuple<byte, object[]>>();
            receiver = Peer.ObserveReceiveEventData()
                .Select(eventData =>
                {
                    object hubIdObj;
                    if (!eventData.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObj) || Convert.GetTypeCode(hubIdObj) != TypeCode.Int16)
                    {
                        return NullTuple;
                    }
                    if ((short)hubIdObj != HubId) return NullTuple;

                    switch (eventData.Code)
                    {
                        case 0:
                            return Tuple.Create((byte)0, 
                                new object[] { (object)PhotonSerializer.Deserialize<System.String>(eventData.Parameters[0]) }
                            );
                        default:
                            return NullTuple;
                    }
                })
                .Where(x => x.Item2 != null)
                .Multicast(rawPublisher)
                .RefCount();


            var r = new TutorialClientReceiver(receiver, rawPublisher);
            client = r;
            publisher = r;
        }
        
        public override IDisposable RegisterListener(ITutorialClient clientListener, bool runOnMainThread = true)
        {
            return receiver.Subscribe(__args =>
            {
                switch (__args.Item1)
                {
                    case 0:
                        {
                            var message = (System.String)(__args.Item2[0]);
                            if(runOnMainThread)
                            {
                                Scheduler.MainThread.Schedule(() => clientListener.GroupBroadcastMessage(message));
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

        public new TutorialProxy AttachInvokeFilter(Func<ITutorialServerInvoker, ITutorialServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(serverFilterFactory);
            return this;
        }

        public TutorialProxy AttachInvokeFilter(Func<TutorialProxy, ITutorialServerInvoker, ITutorialServerInvoker> serverFilterFactory)
        {
            base.AttachInvokeFilter(x => serverFilterFactory(this, x));
            return this;
        }

        public new TutorialProxy AttachReceiveFilter(Func<ITutorialClientReceiver, ITutorialClientReceiver> clientFilterFactory)
        {
            base.AttachReceiveFilter(clientFilterFactory);
            return this;
        }

        public new TutorialProxy AttachFilter(Func<ITutorialServerInvoker, ITutorialServerInvoker> serverFilterFactory, Func<ITutorialClientReceiver, ITutorialClientReceiver> clientFilterFactory)
        {
            base.AttachFilter(serverFilterFactory, clientFilterFactory);
            return this;
        }

        public interface ITutorialServerInvoker
        {
            IObservable<System.Int32> SumAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.String> GetHtmlAsync(System.String url, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<Unit> BroadcastAllAsync(System.String message, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<Unit> RegisterGroupAsync(System.String groupName, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<Unit> BroadcastToAsync(System.String groupName, System.String message, bool observeOnMainThread = true, bool encrypt = false);
            IObservable<System.Int32> ServerToServerAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread = true, bool encrypt = false);
        }

        public class DelegatingTutorialServerInvoker : ITutorialServerInvoker
        {
            readonly ITutorialServerInvoker parent;

            public DelegatingTutorialServerInvoker(ITutorialServerInvoker parent)
            {
                this.parent = parent;
            }

            public virtual IObservable<System.Int32> SumAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.SumAsync( x, y,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.String> GetHtmlAsync(System.String url, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.GetHtmlAsync( url,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<Unit> BroadcastAllAsync(System.String message, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.BroadcastAllAsync( message,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<Unit> RegisterGroupAsync(System.String groupName, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.RegisterGroupAsync( groupName,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<Unit> BroadcastToAsync(System.String groupName, System.String message, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.BroadcastToAsync( groupName, message,  observeOnMainThread, encrypt);
            }

            public virtual IObservable<System.Int32> ServerToServerAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread, bool encrypt)
            {
                return this.parent.ServerToServerAsync( x, y,  observeOnMainThread, encrypt);
            }

        }

        public class TutorialServerInvoker : ITutorialServerInvoker
        {
            readonly ObservablePhotonPeer peer;
            readonly short hubId;

            public TutorialServerInvoker(ObservablePhotonPeer peer, short hubId)
            {
                this.peer = peer;
                this.hubId = hubId;
            }

            public IObservable<System.Int32> SumAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 0;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));
                parameter.Add(1, PhotonSerializer.Serialize(y));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.String> GetHtmlAsync(System.String url, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 1;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(url));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.String>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<Unit> BroadcastAllAsync(System.String message, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 2;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(message));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<Unit> RegisterGroupAsync(System.String groupName, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 3;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(groupName));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<Unit> BroadcastToAsync(System.String groupName, System.String message, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 4;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(groupName));
                parameter.Add(1, PhotonSerializer.Serialize(message));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<Unit>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

            public IObservable<System.Int32> ServerToServerAsync(System.Int32 x, System.Int32 y, bool observeOnMainThread, bool encrypt)
            {
                byte opCode = 5;
                var parameter = new System.Collections.Generic.Dictionary<byte, object>();
                parameter.Add(ReservedParameterNo.RequestHubId, hubId);
                parameter.Add(0, PhotonSerializer.Serialize(x));
                parameter.Add(1, PhotonSerializer.Serialize(y));

                var __response = peer.OpCustomAsync(opCode, parameter, true, 0, encrypt)
                    .Select(__operationResponse =>
                    {
                        var __result = __operationResponse[ReservedParameterNo.ResponseId];
                        return PhotonSerializer.Deserialize<System.Int32>(__result);
                    });

                return (observeOnMainThread) ? __response.ObserveOn(Scheduler.MainThread) : __response;
            }

        }

        public interface ITutorialClientReceiver
        {
            IObservable<System.String> GroupBroadcastMessage(bool observeOnMainThread = true);
        }
        
        public class DelegatingTutorialClientReceiver : ITutorialClientReceiver
        {
            readonly ITutorialClientReceiver parent;

            public DelegatingTutorialClientReceiver(ITutorialClientReceiver parent)
            {
                this.parent = parent;
            }

            public virtual IObservable<System.String> GroupBroadcastMessage(bool observeOnMainThread)
            {
                return this.parent.GroupBroadcastMessage(observeOnMainThread);
            }

        }


        public class TutorialClientReceiver : ITutorialClientReceiver, ITutorialClient
        {
            readonly IObservable<Tuple<byte, object[]>> receiver;
            readonly IObserver<Tuple<byte, object[]>> __publisher;
            static readonly object[] EmptyArray = new object[0];

            public TutorialClientReceiver(IObservable<Tuple<byte, object[]>> receiver, IObserver<Tuple<byte, object[]>> publisher)
            {
                this.receiver = receiver;
                this.__publisher = publisher;
            }

            public IObservable<System.String> GroupBroadcastMessage(bool observeOnMainThread)
            {
                var __result = receiver
                    .Where(__args => __args.Item1 == 0)
                    .Select(__args =>
                    {
                        return (System.String)(__args.Item2[0]);
                    });

                return (observeOnMainThread) ? __result.ObserveOn(Scheduler.MainThread) : __result;
            }

            void ITutorialClient.GroupBroadcastMessage(System.String message)
            {
                __publisher.OnNext(Tuple.Create((byte)0, 
                    new object[] { (object)message }
                ));
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


