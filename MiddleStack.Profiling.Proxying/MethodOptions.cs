using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Proxying
{
    /// <summary>
    ///     Customizes the behaviors of the creation of methods on the proxy class. 
    /// </summary>
    public class MethodOptions
    {
        /// <summary>
        ///     Gets or sets whether the proxy method should profile the underlying method.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if the new method profiles the underlying method.
        ///     <see langword="false"/> if the new method simply invokes the underlying method
        ///     without profiling it.
        /// </returns>
        public bool ProfileMethod { get; set; }

        /// <summary>
        ///     Gets or sets the category of the profiler step that will be created 
        ///     for this method, if <see cref="ProfileMethod"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// 
        /// </value>
        public string Category { get; set; }

        /// <summary>
        ///     Gets or sets a delegate that customizes how the proxy method creates a step object. 
        ///     This delegate is only called if <see cref="ProfileMethod"/> is <see langword="true"/>.
        /// </summary>
        /// <returns>
        ///     <para>A delegate that accepts one parameter and returns a value. The first parameter is an array providing the 
        ///     parameters passed to the method, excluding ref and out parameters. The return value
        ///     is a <see cref="MethodTimingStartOptions"/> object, with which the resulting step can be customized.</para>
        ///     <para>The delegate should return <see langword="null"/> to use the default behavior on a specific invocation
        ///     of this method.</para>
        ///     <para>Specify <see langword="null"/> on this property to use default behaviors on all invocations of this method.</para>
        /// </returns>
        public Func<Object[], MethodTimingStartOptions> CustomizeTimingStart { get; set; }

        /// <summary>
        ///     Gets or sets a delegate that customizes how the proxy method ends the step object.
        ///     Only called if a step object was previously created for the method.
        /// </summary>
        /// <returns>
        ///     <para>A delegate that accepts three parameters and returns a value. The first parameter is the return
        ///     value of the parameter (<see langword="null"/> if the method does not return a value or if an exception has occurred). 
        ///     The second parameter is an exception object, if the proxied method has thrown an exception. The third parameter is a 
        ///     <see cref="MethodTimingEndOptions"/> object with which the step's result object can be customized.</para>
        ///     <para><see langword="null"/> to use default behaviors.</para>
        /// </returns>
        public Action<Object, Exception, MethodTimingEndOptions> CustomizeTimingEnd { get; set; }
    }
}
