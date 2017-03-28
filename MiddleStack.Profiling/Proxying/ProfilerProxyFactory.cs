using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling.Proxying
{
    /// <summary>
    ///     Builds proxies whose virtual methods are proxied by <see cref="LiveProfiler"/>.
    /// </summary>
    public static class ProfilerProxyFactory<TTarget>
        where TTarget: class
    {

        static ProfilerProxyFactory()
        {
            
        }
    }
}
