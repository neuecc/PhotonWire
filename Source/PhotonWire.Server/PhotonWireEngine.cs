using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Photon.SocketServer;
using PhotonWire.Server.Diagnostics;
using PhotonWire.Server.ServerToServer;

namespace PhotonWire.Server
{
    internal enum HubKind
    {
        Client,
        Server,
        ReceiveServer
    }

    internal static class HubKindExtensions
    {
        public static string FastToString(this HubKind hubKind)
        {
            switch (hubKind)
            {
                case HubKind.Client:
                    return nameof(HubKind.Client);
                case HubKind.Server:
                    return nameof(HubKind.Server);
                case HubKind.ReceiveServer:
                    return nameof(HubKind.ReceiveServer);
                default:
                    return ((int)hubKind).ToString();
            }
        }
    }

    internal class PhotonWireEngine
    {
        readonly Dictionary<Tuple<HubKind, short>, HubDescriptor> hubs = new Dictionary<Tuple<HubKind, short>, HubDescriptor>();
        readonly Dictionary<Type, HubDescriptor> hubsByType = new Dictionary<Type, HubDescriptor>();
        readonly HashSet<ApplicationBase> apps = new HashSet<ApplicationBase>();

        static int registeredEngine = -1;

        public static PhotonWireEngine Instance { get; private set; }

        bool enableExceptionReturnDebugError = true;
        int runningCount;
        int completeCount;
        IPhotonSerializer serializer;

        public int RunningCount { get { return runningCount; } }
        public int TotalCompleteCount { get { return completeCount; } }

        PhotonWireEngine()
        {

        }

        public static void Initialize(Assembly[] targetAssemblies, string[] targetTags, IPhotonSerializer serializer, bool enableExceptionReturnDebugError)
        {
            if (Interlocked.Increment(ref registeredEngine) != 0) return;

            var assemblies = targetAssemblies;
            var types = assemblies
                .SelectMany(x =>
                {
                    try
                    {
                        return x.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null);
                    }
                });

            var engine = new PhotonWireEngine()
            {
                serializer = serializer,
                enableExceptionReturnDebugError = enableExceptionReturnDebugError
            };
            Parallel.ForEach(types, type =>
            {
                var hub = HubDescriptor.CreateIfPossible(type);
                if (hub == null) return;

                lock (engine.hubs)
                {
                    // check tag
                    if (targetTags.Length > 0)
                    {
                        if (!targetTags.Any(x => hub.HubTags.Contains(x)))
                        {
                            hub.CanExecute = false;
                        }
                    }

                    var key = Tuple.Create(hub.HubKind, hub.HubId);
                    if (engine.hubs.ContainsKey(key))
                    {
                        throw new InvalidOperationException(string.Format("same hubId is not allowed, class:{0} hubid:{1}", hub.HubName, hub.HubId));
                    }
                    engine.hubs.Add(key, hub);
                    engine.hubsByType.Add(hub.HubType, hub);
                }
            });

            Instance = engine;
        }

        public void RegisterApplication(ApplicationBase app)
        {
            lock (apps)
            {
                apps.Add(app);
            }
        }

        public void UnregisterApplication(ApplicationBase app)
        {
            lock (apps)
            {
                apps.Remove(app);
            }
        }

        internal ApplicationBaseStats[] GetApplicationBaseStats()
        {
            lock (apps)
            {
                return apps.Select(x => new ApplicationBaseStats(x)).ToArray();
            }
        }

        internal HubDescriptor GetHubDescriptor<T>()
            where T : IPhotonWireHub
        {
            HubDescriptor hub;
            return hubsByType.TryGetValue(typeof(T), out hub)
                ? hub
                : null;
        }

        // Routing -> ParameterBinding -> Execute
        public async Task ProcessRequest(HubKind hubType, IPhotonWirePeer peer, OperationRequest operationRequest, SendParameters sendParameters)
        {
            // must don't throw error, all code return response
            var appName = PhotonWireApplicationBase.Instance.ApplicationName;
            var requestStopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref runningCount);
            try
            {
                var now = DateTime.Now;

                var useSerializer = this.serializer;
                object serializerObject;
                // PhotonWire.HubInvoker use only Json, flag is embeded.
                if (peer.Items.TryGetValue("PhotonWireApplicationBase.ModifySerializer", out serializerObject))
                {
                    useSerializer = (IPhotonSerializer)serializerObject;
                }

                // Routing

                object hubIdObject;
                if (!operationRequest.Parameters.TryGetValue(ReservedParameterNo.RequestHubId, out hubIdObject) && (Convert.GetTypeCode(hubIdObject) != TypeCode.Int16))
                {
                    PhotonWireApplicationBase.Instance.Logger.RequiredParameterNotFound(appName, ReservedParameterNo.RequestHubId);
                    return;
                }
                object messageIdObject;
                if (!operationRequest.Parameters.TryGetValue(ReservedParameterNo.MessageId, out messageIdObject) && (Convert.GetTypeCode(messageIdObject) != TypeCode.Int32))
                {
                    PhotonWireApplicationBase.Instance.Logger.RequiredParameterNotFound(appName, ReservedParameterNo.MessageId);
                    return;
                }

                var hubId = (short)hubIdObject;
                var messageId = (int)messageIdObject;
                var operationCode = operationRequest.OperationCode;

                var parameters = new Dictionary<byte, object>();
                parameters[ReservedParameterNo.RequestHubId] = hubId;
                parameters[ReservedParameterNo.MessageId] = messageId;
                var operationResponse = new OperationResponse()
                {
                    OperationCode = operationCode, // return same code:)
                    Parameters = parameters
                };

                try
                {
                    var methodDescriptor = FindMethod(hubType, hubId, operationCode);
                    if (methodDescriptor == null)
                    {
                        throw new InvalidOperationException($"methodDescriptor is null, hubType:{hubType} hubid:{hubId} operationCode:{operationCode}");
                    }
                    var context = new OperationContext(methodDescriptor.Hub, methodDescriptor, peer, operationRequest, sendParameters, now);

                    // Parameter Binding
                    var methodparameters = ParameterBinder.BindParameter(useSerializer, context);
                    context.Parameters = methodparameters;

                    // Execute
                    var hubTypeString = hubType.FastToString();
                    var isError = true;
                    try
                    {
                        PhotonWireApplicationBase.Instance.Logger.ExecuteStart(appName, hubTypeString, hubId, methodDescriptor.Hub.HubName, operationCode, methodDescriptor.MethodName);
                        var result = await methodDescriptor.Execute(context);
                        context.SendOperation(messageId, result, null, false, useSerializer, null);
                        isError = false;
                    }
                    finally
                    {
                        requestStopwatch.Stop();
                        PhotonWireApplicationBase.Instance.Logger.ExecuteFinished(appName, hubTypeString, hubId, methodDescriptor.Hub.HubName, operationCode, methodDescriptor.MethodName, isError, requestStopwatch.Elapsed.TotalMilliseconds);
                    }
                }
                catch (CustomErrorException ex)
                {
                    var context = new OperationContext(hubId, peer, operationRequest, sendParameters, now);
                    context.SendOperation(messageId, ex.Parameter, ex.ErrorMessage, true, null, ex.ReturnCode);
                }
                catch (Exception ex)
                {
                    var context = new OperationContext(hubId, peer, operationRequest, sendParameters, now);
                    context.SendOperation(messageId, null, enableExceptionReturnDebugError ? ex.ToString() : null, true, useSerializer, null);
                    PhotonWireApplicationBase.Instance.Logger.UnhandledException(appName, ex.GetType().Name, ex.Message, ex.StackTrace);
                }
            }
            finally
            {
                Interlocked.Decrement(ref runningCount);
                Interlocked.Increment(ref completeCount);
            }
        }

        MethodDescriptor FindMethod(HubKind hubType, short hubId, byte operationCode)
        {
            HubDescriptor hubDescriptor;
            if (!hubs.TryGetValue(Tuple.Create(hubType, hubId), out hubDescriptor))
            {
                return null;
            }
            if (!hubDescriptor.CanExecute)
            {
                return null;
            }

            MethodDescriptor methodDescriptor;
            if (!hubDescriptor.TryGetMethod(operationCode, out methodDescriptor))
            {
                return null;
            }

            return methodDescriptor;
        }

        // diagnostics only
        internal IEnumerable<KeyValuePair<string, string[]>> GetRegisteredHubInfo()
        {
            foreach (var item in hubs)
            {
                var methods = item.Value.GetRegisteredMethods();
                yield return new KeyValuePair<string, string[]>(item.Value.HubName, methods);
            }
        }
    }
}