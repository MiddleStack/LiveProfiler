namespace MiddleStack.Profiling.Proxying
{
    /// <summary>
    ///     Customizes the behaviors of the proxy when a 
    ///     step object created for a method is being ended.
    /// </summary>
    public class MethodTimingEndOptions
    {
        internal MethodTimingEndOptions(ITiming timing)
        {
            Timing = timing;
        }

        /// <summary>
        ///     Gets the <see cref="ITiming"/> object being ended. 
        ///     <see cref="ITiming.Success"/> and <see cref="ITiming.Failure"/> 
        ///     method can be invoked on this method to override the default behaviors
        ///     of the proxy method.
        /// </summary>
        /// <returns>
        ///     <para>The <see cref="ITiming"/> object being ended.</para>
        /// </returns>
        public ITiming Timing { get; }
    }
}