using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PhotonWire.Server.Collections;
using PhotonWire.Server.ServerToServer;

namespace PhotonWire.Server
{
    public class HubDescriptor
    {
        public Type HubType { get; private set; }
        public Type ClientType { get; private set; }
        public short HubId { get; private set; }
        public string HubName { get; private set; }
        public bool CanExecute { get; internal set; } // set from PhotonWireEngine.Initialize

        internal HubKind HubKind { get; private set; }
        internal ReadOnlyHashSet<string> HubTags { get; private set; }

        Dictionary<byte, MethodDescriptor> Methods = new Dictionary<byte, MethodDescriptor>();

        HubDescriptor()
        {
        }

        internal static HubDescriptor CreateIfPossible(Type type)
        {
            HubKind kind;
            if (typeof(IHub).IsAssignableFrom(type))
            {
                kind = HubKind.Client;
            }
            else if (typeof(ServerHub).IsAssignableFrom(type))
            {
                kind = HubKind.Server;
            }
            else if (typeof(ReceiveServerHub).IsAssignableFrom(type))
            {
                kind = HubKind.ReceiveServer;
            }
            else
            {
                return null;
            }

            if (type.IsAbstract) return null;
            if (type.GetCustomAttribute<IgnoreOperationAttribute>(true) != null) return null; // ignore

            var className = type.Name;
            if (!type.GetConstructors().Any(x => x.GetParameters().Length == 0))
            {
                throw new InvalidOperationException(string.Format("Hub needs parameterless constructor, class:{0}", type.FullName));
            }

            var hubAttr = type.GetCustomAttributes<HubAttribute>(false).FirstOrDefault();
            if (hubAttr == null)
            {
                throw new InvalidOperationException(string.Format("Hub needs HubAttribute, class:{0}", type.FullName));
            }
            var clientType = (kind == HubKind.Client)
                ? FindHubClientType(type)
                : typeof(INoClient);

            var tags = new HashSet<string>(type.GetCustomAttributes<HubTag>(true).SelectMany(x => x.Tags));

            var hub = new HubDescriptor()
            {
                HubName = className,
                HubType = type,
                ClientType = clientType,
                HubKind = kind,
                HubId = hubAttr.HubId,
                CanExecute = true,
                HubTags = new ReadOnlyHashSet<string>(tags)
            };

            foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (methodInfo.IsSpecialName && (methodInfo.Name.StartsWith("set_") || methodInfo.Name.StartsWith("get_"))) continue; // as property
                if (methodInfo.GetCustomAttribute<IgnoreOperationAttribute>(true) != null) continue; // ignore

                var methodName = methodInfo.Name;

                // ignore default methods
                if (methodName == "Equals"
                 || methodName == "GetHashCode"
                 || methodName == "GetType"
                 || methodName == "ToString")
                {
                    continue;
                }

                // create handler
                var handler = new MethodDescriptor(hub, methodInfo);

                if (hub.Methods.ContainsKey(handler.OperationCode))
                {
                    throw new InvalidOperationException(string.Format("same operationCode is not allowed, class:{0} method:{1} opCode:{2}", className, methodName, handler.OperationCode));
                }
                else
                {
                    hub.Methods.Add(handler.OperationCode, handler);
                }
            }

            return hub;
        }

        internal bool TryGetMethod(byte operationCode, out MethodDescriptor methodDescriptor)
        {
            return Methods.TryGetValue(operationCode, out methodDescriptor);
        }

        // diagnostics only.
        internal string[] GetRegisteredMethods()
        {
            return Methods.Select(x => x.Value.MethodName).ToArray();
        }

        static Type FindHubClientType(Type classType)
        {
            var baseType = classType.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Hub<>))
                {
                    return baseType.GetGenericArguments()[0];
                }
                baseType = baseType.BaseType;
            }
            throw new InvalidOperationException($"Can't find client type, Type:{classType.FullName}");
        }
    }
}