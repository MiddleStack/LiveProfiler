namespace MiddleStack.Profiling.Proxying
{
    /// <summary>
    ///     Customizes the behaviors of the proxy method when a 
    ///     step object is being created.
    /// </summary>
    public class MethodTimingStartOptions
    {
        /// <summary>
        ///     Gets or sets whether or not to profile this method call.
        /// </summary>
        /// <returns>
        ///     <para><see langword="true"/> to profile this method and create the step object.
        ///     <see langword="false"/> to skip profiling this method.</para>
        ///     <para><see langword="true"/> by default.</para>
        /// </returns>
        public bool ProfileMethod { get; set; } = true;
        /// <summary>
        ///     Gets or sets a non-default step or transaction object for this method.
        /// </summary>
        /// <returns>
        ///     A non-default <see cref="ITiming"/> for this object. 
        ///     <see langword="null"/> if the default step or transaction object is to be created.
        /// </returns>
        public ITiming Timing { get; set; }
    }
}