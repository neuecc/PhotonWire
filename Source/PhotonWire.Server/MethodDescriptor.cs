using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace PhotonWire.Server
{
    public class ParameterInfoSlim
    {
        public Type ParameterType { get; }
        public bool ParameterTypeIsArray { get; }
        public bool ParameterTypeIsClass { get; }
        public bool ParameterTypeIsNullable { get; }

        public string Name { get; }
        public bool IsOptional { get; }
        public object DefaultValue { get; }

        internal ParameterInfoSlim(ParameterInfo parameterInfo)
        {
            Name = parameterInfo.Name;
            DefaultValue = parameterInfo.DefaultValue;
            IsOptional = parameterInfo.IsOptional;
            ParameterType = parameterInfo.ParameterType;
            ParameterTypeIsArray = parameterInfo.ParameterType.IsArray;
            ParameterTypeIsClass = parameterInfo.ParameterType.IsClass;
            ParameterTypeIsNullable = parameterInfo.ParameterType.IsNullable();
        }
    }

    internal enum HandlerBodyType
    {
        // 0 is invalid

        Func = 1,
        AsyncFunc = 2,
        Action = 3,
        AsyncAction = 4
    }

    public class MethodDescriptor
    {
        readonly static Dictionary<Type, Func<object, object>> taskResultExtractors = new Dictionary<Type, Func<object, object>>();

        public HubDescriptor Hub { get; private set; }

        public string MethodName { get; private set; }
        public byte OperationCode { get; private set; }

        public ParameterInfoSlim[] Arguments { get; private set; }
        public IReadOnlyList<string> ParameterNames { get; private set; }

        public Type ReturnType { get; private set; }

        public ILookup<Type, Attribute> AttributeLookup { get; private set; }

        // internal use
        readonly PhotonWireFilterAttribute[] filters;

        readonly HandlerBodyType handlerBodyType;

        // MethodCache Delegate => environment, arguments, returnType

        readonly Func<OperationContext, object[], object> methodFuncBody;

        readonly Func<OperationContext, object[], Task> methodAsyncFuncBody;

        readonly Action<OperationContext, object[]> methodActionBody;

        readonly Func<OperationContext, object[], Task> methodAsyncActionBody;


        public MethodDescriptor(HubDescriptor hub, MethodInfo methodInfo)
        {
            var classType = hub.HubType;

            var opAttr = methodInfo.GetCustomAttributes<OperationAttribute>().FirstOrDefault();
            if (opAttr == null) throw new InvalidOperationException($"Method needs OperationAttribute, class:{classType.Name} method:{methodInfo.Name}");

            this.Hub = hub;
            this.OperationCode = opAttr.OperationCode;
            this.MethodName = methodInfo.Name;
            this.Arguments = methodInfo.GetParameters()
                .Select(x => new ParameterInfoSlim(x))
                .ToArray();
            this.ParameterNames = Arguments.Select(x => x.Name).ToList().AsReadOnly();
            this.ReturnType = methodInfo.ReturnType;

            this.filters = classType.GetCustomAttributes<PhotonWireFilterAttribute>(true)
                .Concat(methodInfo.GetCustomAttributes<PhotonWireFilterAttribute>(true))
                .OrderBy(x => x.Order)
                .ToArray();

            this.AttributeLookup = classType.GetCustomAttributes(true)
                .Concat(methodInfo.GetCustomAttributes(true))
                .Cast<Attribute>()
                .ToLookup(x => x.GetType());

            // prepare lambda parameters
            var contextArg = Expression.Parameter(typeof(OperationContext), "context");
            var contextBind = Expression.Bind(classType.GetProperty("Context"), contextArg);
            var args = Expression.Parameter(typeof(object[]), "args");
            var parameters = methodInfo.GetParameters()
                .Select((x, i) => Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(i)), x.ParameterType))
                .ToArray();

            // Task or Task<T>
            if (typeof(Task).IsAssignableFrom(this.ReturnType))
            {
                // (object[] args) => new X().M((T1)args[0], (T2)args[1])...
                var lambda = Expression.Lambda<Func<OperationContext, object[], Task>>(
                    Expression.Call(
                        Expression.MemberInit(Expression.New(classType), contextBind),
                        methodInfo,
                        parameters),
                    contextArg, args);

                if (this.ReturnType.IsGenericType && this.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    this.handlerBodyType = HandlerBodyType.AsyncFunc;
                    this.methodAsyncFuncBody = lambda.Compile();

                    lock (taskResultExtractors)
                    {
                        if (!taskResultExtractors.ContainsKey(this.ReturnType))
                        {
                            // (object task) => (object)((Task<>).Result)
                            var taskParameter = Expression.Parameter(typeof(object), "task");
                            var resultLambda = Expression.Lambda<Func<object, object>>(
                                Expression.Convert(
                                    Expression.Property(
                                        Expression.Convert(taskParameter, this.ReturnType),
                                        "Result"),
                                    typeof(object)),
                                taskParameter);

                            var compiledResultLambda = resultLambda.Compile();

                            taskResultExtractors[this.ReturnType] = compiledResultLambda;
                        }
                    }
                }
                else
                {
                    this.handlerBodyType = HandlerBodyType.AsyncAction;
                    this.methodAsyncActionBody = lambda.Compile();
                }
            }
            else if (this.ReturnType == typeof(void)) // of course void
            {
                // (object[] args) => { new X().M((T1)args[0], (T2)args[1])... }
                var lambda = Expression.Lambda<Action<OperationContext, object[]>>(
                    Expression.Call(
                        Expression.MemberInit(Expression.New(classType), contextBind),
                        methodInfo,
                        parameters),
                    contextArg, args);

                this.handlerBodyType = HandlerBodyType.Action;
                this.methodActionBody = lambda.Compile();
            }
            else // return T
            {
                // (object[] args) => (object)new X().M((T1)args[0], (T2)args[1])...
                var lambda = Expression.Lambda<Func<OperationContext, object[], object>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.MemberInit(Expression.New(classType), contextBind),
                            methodInfo,
                            parameters)
                    , typeof(object)),
                    contextArg, args);

                this.handlerBodyType = HandlerBodyType.Func;
                this.methodFuncBody = lambda.Compile();
            }
        }

        internal Task<object> Execute(OperationContext context)
        {
            return InvokeRecursive(-1, context);
        }

        Task<object> InvokeRecursive(int index, OperationContext context)
        {
            index += 1;
            if (filters.Length != index)
            {
                // chain next filter
                return filters[index].Invoke(context, () => InvokeRecursive(index, context));
            }
            else
            {
                // execute operation
                return ExecuteOperation(context);
            }
        }

        async Task<object> ExecuteOperation(OperationContext context)
        {
            // prepare
            var handler = this;
            var methodParameters = (object[])context.Parameters;

            object result = null;
            switch (handler.handlerBodyType)
            {
                case HandlerBodyType.Action:
                    handler.methodActionBody(context, methodParameters);
                    break;
                case HandlerBodyType.Func:
                    result = handler.methodFuncBody(context, methodParameters);
                    break;
                case HandlerBodyType.AsyncAction:
                    var actionTask = handler.methodAsyncActionBody(context, methodParameters);
                    await actionTask.ConfigureAwait(false);
                    break;
                case HandlerBodyType.AsyncFunc:
                    var funcTask = handler.methodAsyncFuncBody(context, methodParameters);
                    await funcTask.ConfigureAwait(false);
                    var extractor = taskResultExtractors[FindTaskType(funcTask.GetType())];
                    result = extractor(funcTask);
                    break;
                default:
                    throw new InvalidOperationException("critical:register code is broken");
            }

            return result;
        }

        // Task.WhenAll = WhenAllPromise, find base Task<T> type
        Type FindTaskType(Type t)
        {
            while (t.GetGenericTypeDefinition() != typeof(Task<>))
            {
                t = t.BaseType;
            }
            return t;
        }
    }
}