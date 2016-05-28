using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace PhotonWire.Server.ServerToServer
{
    public class TypedServerHubClientBuilder<T> where T : IServerHub
    {
        private const string ClientModuleName = "PhotonWire.TypedServerHubClientBuilder";

        private static Lazy<Func<IServerHubContext, IS2SPhotonWirePeer, T>> builder = new Lazy<Func<IServerHubContext, IS2SPhotonWirePeer, T>>(() => GenerateClientBuilder());

        public static T Build(IServerHubContext context, IS2SPhotonWirePeer peer)
        {
            return builder.Value(context, peer);
        }

        private static Func<IServerHubContext, IPhotonWirePeer, T> GenerateClientBuilder()
        {
            VeryifyServerHubType(typeof(T));

            var assemblyName = new AssemblyName(ClientModuleName);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(ClientModuleName);
            var clientType = GenerateProxyClassImplementation(moduleBuilder);

            return (context, targetPeers) => (T)Activator.CreateInstance(clientType, context, targetPeers);
        }

        private static Type GenerateProxyClassImplementation(ModuleBuilder moduleBuilder)
        {
            var type = moduleBuilder.DefineType(
                ClientModuleName + "." + typeof(T).Name + "Impl",
                TypeAttributes.Public,
                typeof(T));

            var contextField = type.DefineField("_context", typeof(IServerHubContext), FieldAttributes.Private);
            var targetPeerField = type.DefineField("_peer", typeof(IS2SPhotonWirePeer), FieldAttributes.Private);

            BuildConstructor(type, contextField, targetPeerField);

            foreach (var method in GetAllTargetMethod(typeof(T)))
            {
                BuildMethod(type, method, contextField, targetPeerField);
            }

            return type.CreateType();
        }

        private static IEnumerable<MethodInfo> GetAllTargetMethod(Type type)
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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

                yield return methodInfo;
            }
        }

        private static void BuildConstructor(TypeBuilder type, FieldInfo proxyField, FieldInfo targetPeerField)
        {
            MethodBuilder method = type.DefineMethod(".ctor", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig);

            ConstructorInfo ctor = typeof(object).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new Type[] { }, null);

            method.SetReturnType(typeof(void));
            method.SetParameters(typeof(IServerHubContext), typeof(IS2SPhotonWirePeer));

            ILGenerator generator = method.GetILGenerator();

            // ctor
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, ctor);

            // Assign
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, proxyField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Stfld, targetPeerField);

            generator.Emit(OpCodes.Ret);
        }

        private static void BuildMethod(TypeBuilder type, MethodInfo methodInfo, FieldInfo contextField, FieldInfo targetPeerField)
        {
            MethodAttributes methodAttributes =
                  MethodAttributes.Public
                | MethodAttributes.Virtual
                | MethodAttributes.Final
                | MethodAttributes.HideBySig;

            ParameterInfo[] parameters = methodInfo.GetParameters();
            Type[] paramTypes = parameters.Select(param => param.ParameterType).ToArray();

            MethodBuilder methodBuilder = type.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.ReturnType, paramTypes);

            var invokeMethods = typeof(IServerHubContext).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.Name == "SendOperationRequestAsync");

            MethodInfo invokeMethod;
            if (methodInfo.ReturnType != typeof(Task))
            {
                invokeMethod = invokeMethods.First(x => x.IsGenericMethod).MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()[0]);
            }
            else
            {
                invokeMethod = invokeMethods.First(x => !x.IsGenericMethod);
            }

            methodBuilder.SetReturnType(methodInfo.ReturnType);
            methodBuilder.SetParameters(paramTypes);

            ILGenerator generator = methodBuilder.GetILGenerator();

            generator.DeclareLocal(typeof(object[]));

            // Get Context and peer
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, contextField); // context
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, targetPeerField); // peer

            // OpCode
            var opCode = methodInfo.GetCustomAttribute<OperationAttribute>().OperationCode;
            generator.Emit(OpCodes.Ldc_I4, (int)opCode);

            // new[]{ }
            generator.Emit(OpCodes.Ldc_I4, parameters.Length);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc_0);

            // object[]
            for (int i = 0; i < paramTypes.Length; i++)
            {
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldc_I4, i);
                generator.Emit(OpCodes.Ldarg, i + 1);
                generator.Emit(OpCodes.Box, paramTypes[i]);
                generator.Emit(OpCodes.Stelem_Ref);
            }

            // Call method
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Callvirt, invokeMethod);

            generator.Emit(OpCodes.Ret);
        }

        private static void VeryifyServerHubType(Type classType)
        {
            if (!classType.IsClass)
            {
                throw new InvalidOperationException($"ServerHub<T>'s T must be class : {classType.Name}");
            }

            if (classType.GetCustomAttributes<HubAttribute>().FirstOrDefault() == null)
            {
                throw new InvalidOperationException($"ServerHub must put HubAttribute : {classType.Name}");
            }

            foreach (var method in GetAllTargetMethod(classType))
            {
                VerifyMethod(classType, method);
            }
        }

        private static void VerifyMethod(Type classType, MethodInfo methodInfo)
        {
            var returnType = methodInfo.ReturnType;
            if (returnType != typeof(Task) && !(returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)))
            {
                throw new InvalidOperationException($"ServerHub's method must return Task or Task<T> : {classType.Name}.{methodInfo.Name}");
            }

            if (methodInfo.GetCustomAttributes<OperationAttribute>().FirstOrDefault() == null)
            {
                throw new InvalidOperationException($"ServerHub's method must put OperationAttribute : {classType.Name}.{methodInfo.Name}");
            }

            if (!methodInfo.IsVirtual)
            {
                throw new InvalidOperationException($"ServerHub's method must be virtual : {classType.Name}.{methodInfo.Name}");
            }

            foreach (var parameter in methodInfo.GetParameters())
            {
                VerifyParameter(classType, methodInfo, parameter);
            }
        }

        private static void VerifyParameter(Type classType, MethodInfo methodInfo, ParameterInfo parameter)
        {
            if (parameter.IsOut)
            {
                throw new InvalidOperationException($"ServerHub proxy's method must not take out parameter : {classType.Name}.{methodInfo.Name}({parameter.Name})");
            }

            if (parameter.ParameterType.IsByRef)
            {
                throw new InvalidOperationException($"ServerHub proxy's method must not take ref parameter : {classType.Name}.{methodInfo.Name}({parameter.Name})");
            }
        }
    }
}