using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Proxying
{
    /// <summary>
    ///     Defines behavioral options for <see cref="LiveProfilerProxyFactory{TTarget}"/>, when generating the proxy type.
    /// </summary>
    public class LiveProfilerProxyOptions
    {
        /// <summary>
        ///     Gets or sets the delegate that customizes the methods on the resulting proxy type.
        ///     This is invoked for both classes and interfaces.
        /// </summary>
        /// <remarks>
        ///     By default, all methods on an interface are profiled. 
        ///     Properties are not profiled. This delegate allows the behavior to be changed. It also allows
        ///     the customization of the profiling behavior down to the level of individual method calls.
        /// </remarks>
        /// <returns>
        ///     A delegate taking one parameter and returns an object. The parameter is the <see cref="MethodInfo"/>
        ///     that is being examined. The return value is a <see cref="MethodOptions"/> object
        ///     with which to customize proxy method behavior. Specify a <see langword="null"/> delegate 
        ///     if the default behaviors are desired for all methods. The delegate should return <see langword="null"/> 
        ///     use the default behaviors for a specific method.
        /// </returns>
        public Func<MethodInfo, MethodOptions> CustomizeMethods { get; set; }
    }
}
