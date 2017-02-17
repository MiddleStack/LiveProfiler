namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Provides enumerated values indicating whether a timing is 
    ///     a transaction or a step.
    /// </summary>
    public enum TimingType
    {
        /// <summary>
        ///     The timing is a transaction.
        /// </summary>
        Transaction,
        /// <summary>
        ///     The timing is a step within a transaction or another step.
        /// </summary>
        Step
    }
}