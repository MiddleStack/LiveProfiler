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
    ///     Dynamically generates a proxy class that profiles the virtual methods
    ///     of a base class or interface using <see cref="LiveProfiler"/>.
    /// </summary>
    /// <typeparam name="TTarget">
    ///     The type of class or interface that the generated proxies will profile.
    ///     This type needs to be a virtual class or interface, 
    ///     have at least one constructor whose visibility is protected or above,
    ///     and have at least one virtual method.
    /// </typeparam>
    public static class LiveProfilerProxyFactory<TTarget>
        where TTarget: class
    {
        private Type _wrapperType;

        static LiveProfilerProxyFactory()
        {
            _wrapperType = BuildWrapperType();
        }

        /// <summary>
        ///     Profiles the specified target object.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static TTarget Profile(TTarget target, params object[] args)
        {
            if (target != null) throw new ArgumentNullException(nameof(target));
        }

        private static Type BuildWrapperType()
        {
            var builder = GetTypeBuilder();

            DefineConstructors(builder);
        }

        private static void DefineConstructors(TypeBuilder builder)
        {
            if (typeof(TTarget).IsInterface)
            {
                
            }
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
                parent = typeof(TTarget);
                interfaces = null;
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

            return tb;
        }
    }
}
