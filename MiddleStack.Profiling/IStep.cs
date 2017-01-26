using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Represents a step in a profiled transaction. When this object is disposed, 
    ///     the step is marked as complete, along with all of its outstanding child steps.
    /// </summary>
    public interface IStep: IDisposable
    {
    }
}
