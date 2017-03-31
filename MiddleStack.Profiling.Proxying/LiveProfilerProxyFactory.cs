using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Proxying
{
    /// <summary>
    ///     Dynamically generates a proxy class that profiles the methods of an interface 
    ///     using <see cref="LiveProfiler"/>.
    /// </summary>
    /// <typeparam name="TTarget">
    ///     The type of class or interface that the generated proxies will profile.
    ///     This type needs to be a virtual class or interface, 
    ///     have at least one constructor whose visibility is protected or above,
    ///     and have at least one virtual method.
    /// </typeparam>
    public class LiveProfilerProxyFactory<TTarget>
        where TTarget: class
    {
        private readonly LiveProfilerProxyOptions _options;
        private const string InnerObjectFieldName = "_innerObject";
        private readonly Func<TTarget, TTarget> _constructorInvoker;

        /// <summary>
        ///     Initializes a new instance of <see cref="LiveProfilerProxyFactory{TTarget}"/>.
        /// </summary>
        /// <param name="options">
        ///     A <see cref="LiveProfilerProxyOptions"/> object providing behavioral customization
        ///     options for generating the proxy. Specify <see langword="null"/> to use default behaviors.
        /// </param>
        public LiveProfilerProxyFactory(LiveProfilerProxyOptions options = null)
        {
            _options = options;
            _constructorInvoker = BuildWrapperType();
        }

        /// <summary>
        ///     Profiles the specified target object.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public TTarget Profile(TTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return _constructorInvoker(target);
        }

        private static Func<TTarget, TTarget> BuildWrapperType()
        {
            var builder = GetTypeBuilder();

            DefineConstructors(builder);

            DefineMethods(builder);

            var proxyType = builder.CreateType();

            var constructorInvoker = new DynamicMethod(
                proxyType.FullName + "_ConstructorInvoker", 
                typeof(TTarget), 
                new [] {typeof(TTarget)});

            var ilGenerator = constructorInvoker.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Newobj, builder.GetConstructor(new [] {typeof(TTarget)}));
            ilGenerator.Emit(OpCodes.Ret);

            return (Func<TTarget, TTarget>) constructorInvoker.CreateDelegate(typeof(Func<TTarget, TTarget>));
        }

        private static void DefineConstructors(TypeBuilder builder)
        {
            // Interface: single public constructor accepting the wrapped object as
            // the constructor.
            var constructorBuilder = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                new[] {typeof(TTarget)});

            var ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Stfld, builder.GetField(InnerObjectFieldName));
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineMethods(TypeBuilder builder)
        {
            foreach (var method in typeof(TTarget).GetMethods(
                BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public))
            {
                DefineProxyMethod(method, builder, true);
            }

            foreach (var method in typeof(TTarget).GetProperties(
                BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public).SelectMany(p => p.GetAccessors()))
            {
                DefineProxyMethod(method, builder, false);
            }
        }

        private static void DefineProxyMethod(MethodInfo method, TypeBuilder builder, bool doProfile)
        {
            var parameters = method.GetParameters().ToArray();

            var proxyMethod = builder.DefineMethod(
                method.Name,
                method.Attributes,
                method.CallingConvention,
                method.ReturnType,
                method.ReturnParameter?.GetRequiredCustomModifiers(),
                method.ReturnParameter?.GetOptionalCustomModifiers(),
                parameters.Select(p => p.ParameterType).ToArray(),
                parameters.Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                parameters.Select(p => p.GetOptionalCustomModifiers()).ToArray());

            var ilGenerator = proxyMethod.GetILGenerator();

            // Load inner object onto stack
            ilGenerator.Emit(OpCodes.Ldfld, builder.GetField(InnerObjectFieldName, BindingFlags.Instance | BindingFlags.NonPublic));

            // Load parameters onto stack
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                ilGenerator.Emit(
                    (parameter.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out
                        ? OpCodes.Ldarga
                        : OpCodes.Ldarg, 
                    i);
            }

            // Call method
            ilGenerator.Emit(OpCodes.Call, method);

            ilGenerator.Emit(OpCodes.Ret);
        }

        private static TypeBuilder GetTypeBuilder()
        {
            Type parent;
            Type[] interfaces;

            if (typeof(TTarget).IsInterface)
            {
                interfaces = new[] {typeof(TTarget)};
                parent = null;
            }
            else
            {
                throw new InvalidOperationException($"Type {typeof(TTarget)} is not an interface and cannot be proxied.");
            }

            var typeSignature = typeof(TTarget).FullName + "_LiveProfilerProxy";
            var an = new AssemblyName(typeSignature);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            var tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    parent,
                    interfaces);

            tb.DefineField(InnerObjectFieldName, typeof(TTarget), FieldAttributes.Private | FieldAttributes.InitOnly);

            return tb;
        }

        private class ContinueWithProvider
        {
            private readonly ITiming _timing;
            private readonly MethodOptions _options;

            public ContinueWithProvider(ITiming timing, MethodOptions options)
            {
                _timing = timing;
                _options = options;
            }

            public void Handle(Task task)
            {
                DoHandle(task, null);
            }

            public void Handle<T>(Task<T> task)
            {
                object result = !task.IsFaulted ? (object)task.Result : null;

                DoHandle(task, result);
            }

            private void DoHandle(Task task, object result)
            {
                var exception = task.Exception?.InnerExceptions.Count == 1 ? task.Exception.InnerException : task.Exception;

                if (_options?.CustomizeTimingEnd != null)
                {
                    var endOptions = new MethodTimingEndOptions(_timing);

                    _options.CustomizeTimingEnd(result, exception, endOptions);
                }

                if (_timing.State == TransactionState.Inflight)
                {
                    if (task.IsFaulted)
                    {
                        _timing.Failure(new
                        {
                            Exception = exception
                        });
                    }
                    else
                    {
                        _timing.Success();
                    }
                }
            }
        }
    }
}
