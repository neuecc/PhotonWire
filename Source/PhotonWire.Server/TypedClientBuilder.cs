using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Photon.SocketServer;

namespace PhotonWire.Server
{
    public class TypedClientBuilder<T>
    {
        private const string ClientModuleName = "PhotonWire.TypedClientBuilder";

        private static Lazy<Func<HubContext, IEnumerable<IPhotonWirePeer>, T>> builder = new Lazy<Func<HubContext, IEnumerable<IPhotonWirePeer>, T>>(() => GenerateClientBuilder());

        public static T Build(HubContext context, IEnumerable<IPhotonWirePeer> targetPeers)
        {
            return builder.Value(context, targetPeers);
        }

        private static Func<HubContext, IEnumerable<IPhotonWirePeer>, T> GenerateClientBuilder()
        {
            VerifyInterface(typeof(T));

            var assemblyName = new AssemblyName(ClientModuleName);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(ClientModuleName);
            var clientType = GenerateInterfaceImplementation(moduleBuilder);

            return (context, targetPeers) => (T)Activator.CreateInstance(clientType, context, targetPeers);
        }

        private static Type GenerateInterfaceImplementation(ModuleBuilder moduleBuilder)
        {
            var type = moduleBuilder.DefineType(
                ClientModuleName + "." + typeof(T).Name + "Impl",
                TypeAttributes.Public,
                typeof(Object),
                new[] { typeof(T) });

            var contextField = type.DefineField("_context", typeof(HubContext), FieldAttributes.Private);
            var targetPeerField = type.DefineField("_targetPeers", typeof(IEnumerable<IPhotonWirePeer>), FieldAttributes.Private);

            BuildConstructor(type, contextField, targetPeerField);

            foreach (var method in GetAllInterfaceMethods(typeof(T)))
            {
                BuildMethod(type, method, contextField, targetPeerField);
            }

            return type.CreateType();
        }

        private static IEnumerable<MethodInfo> GetAllInterfaceMethods(Type interfaceType)
        {
            foreach (var parent in interfaceType.GetInterfaces())
            {
                foreach (var parentMethod in GetAllInterfaceMethods(parent))
                {
                    yield return parentMethod;
                }
            }

            foreach (var method in interfaceType.GetMethods())
            {
                yield return method;
            }
        }

        private static void BuildConstructor(TypeBuilder type, FieldInfo proxyField, FieldInfo targetPeerField)
        {
            MethodBuilder method = type.DefineMethod(".ctor", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig);

            ConstructorInfo ctor = typeof(object).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new Type[] { }, null);

            method.SetReturnType(typeof(void));
            method.SetParameters(typeof(HubContext), typeof(IEnumerable<IPhotonWirePeer>));

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

        private static void BuildMethod(TypeBuilder type, MethodInfo interfaceMethodInfo, FieldInfo contextField, FieldInfo targetPeerField)
        {
            MethodAttributes methodAttributes =
                  MethodAttributes.Public
                | MethodAttributes.Virtual
                | MethodAttributes.Final
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot;

            ParameterInfo[] parameters = interfaceMethodInfo.GetParameters();
            Type[] paramTypes = parameters.Select(param => param.ParameterType).ToArray();

            MethodBuilder methodBuilder = type.DefineMethod(interfaceMethodInfo.Name, methodAttributes);

            // void BroadcastEvent(IEnumerable<IPhotonWirePeer> targetPeers, byte eventCode, params object[] args);
            MethodInfo invokeMethod = typeof(HubContext).GetMethod(
                "BroadcastEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new Type[] { typeof(IEnumerable<IPhotonWirePeer>), typeof(byte), typeof(object[]) }, null);

            methodBuilder.SetReturnType(interfaceMethodInfo.ReturnType);
            methodBuilder.SetParameters(paramTypes);

            ILGenerator generator = methodBuilder.GetILGenerator();

            generator.DeclareLocal(typeof(object[]));

            // Get Context and peer
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, contextField); // context
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, targetPeerField); // peer

            // OpCode
            var opCode = interfaceMethodInfo.GetCustomAttribute<OperationAttribute>().OperationCode;
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

            // Call BroadcastEvent
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Callvirt, invokeMethod);

            generator.Emit(OpCodes.Ret);
        }

        // Verify

        private static void VerifyInterface(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException($"Hub<T>'s T must be interface : {interfaceType.Name}");
            }

            if (interfaceType.GetProperties().Length != 0)
            {
                throw new InvalidOperationException($"Client proxy type must not contains properties : {interfaceType.Name}");
            }

            if (interfaceType.GetEvents().Length != 0)
            {
                throw new InvalidOperationException($"Client proxy type must not contains events : {interfaceType.Name}");
            }

            foreach (var method in interfaceType.GetMethods())
            {
                VerifyMethod(interfaceType, method);
            }

            foreach (var parent in interfaceType.GetInterfaces())
            {
                VerifyInterface(parent);
            }
        }

        private static void VerifyMethod(Type interfaceType, MethodInfo interfaceMethod)
        {
            if (interfaceMethod.ReturnType != typeof(void))
            {
                throw new InvalidOperationException($"Client proxy's method must return void : {interfaceType.Name}.{interfaceMethod.Name}");
            }

            if (interfaceMethod.GetCustomAttributes<OperationAttribute>().FirstOrDefault() == null)
            {
                throw new InvalidOperationException($"Client proxy's method must put OperationAttribute : {interfaceType.Name}.{interfaceMethod.Name}");
            }

            foreach (var parameter in interfaceMethod.GetParameters())
            {
                VerifyParameter(interfaceType, interfaceMethod, parameter);
            }
        }

        private static void VerifyParameter(Type interfaceType, MethodInfo interfaceMethod, ParameterInfo parameter)
        {
            if (parameter.IsOut)
            {
                throw new InvalidOperationException($"Client proxy's method must not take out parameter : {interfaceType.Name}.{interfaceMethod.Name}({parameter.Name})");
            }

            if (parameter.ParameterType.IsByRef)
            {
                throw new InvalidOperationException($"Client proxy's method must not take ref parameter : {interfaceType.Name}.{interfaceMethod.Name}({parameter.Name})");
            }
        }
    }
}