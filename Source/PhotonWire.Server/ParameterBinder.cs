using System;
using System.Linq;
using PhotonWire;

namespace PhotonWire.Server
{
    internal static class ParameterBinder
    {
        internal static object[] BindParameter(IPhotonSerializer serializer, OperationContext context)
        {
            var arguments = context.Method.Arguments;
            var methodParameters = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                var item = arguments[i];

                object rawValue;
                context.OperationRequest.Parameters.TryGetValue((byte)i, out rawValue);

                if (rawValue == null)
                {
                    if (item.IsOptional)
                    {
                        methodParameters[i] = item.DefaultValue;
                    }
                    else if (item.ParameterTypeIsClass || item.ParameterTypeIsNullable)
                    {
                        methodParameters[i] = null;
                    }
                    else if (item.ParameterTypeIsArray)
                    {
                        methodParameters[i] = Array.CreateInstance(item.ParameterType.GetElementType(), 0);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Parameter Missing, {context.Hub.HubName}/{context.Method.MethodName}({item.Name})");
                    }
                }
                else
                {
                    if (rawValue.GetType() != typeof(byte[]))
                    {                                               
                        if (item.ParameterType != rawValue.GetType())
                        {
                            if (item.ParameterTypeIsNullable)
                            {
                                methodParameters[i] = rawValue; // if nullable, use rawValue.
                                continue;
                            }

                            var parameters = string.Join(", ", arguments.Select(x =>
                            {
                                return (x == item)
                                    ? "[" + x.ParameterType.Name + " " + x.Name + "]"
                                    : x.ParameterType.Name + " " + x.Name;
                            }));

                            throw new InvalidOperationException($"Parameter Type Unmatch, {context.Hub.HubName}/{context.Method.MethodName}({parameters}) ReceivedType:{rawValue.GetType().Name} Value:{rawValue}");
                        }
                    }

                    methodParameters[i] = serializer.Deserialize(item.ParameterType, rawValue);
                }
            }

            return methodParameters;
        }
    }
}