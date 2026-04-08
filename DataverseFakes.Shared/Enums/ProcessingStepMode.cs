namespace FakeXrmEasy
{
    /// <summary>
    /// Defines the execution mode for a plugin step in the Dynamics 365 plugin pipeline.
    /// </summary>
    public enum ProcessingStepMode
    {
        /// <summary>
        /// Synchronous execution mode. The plugin executes immediately as part of the main operation.
        /// The calling code waits for the plugin to complete before continuing.
        /// </summary>
        Synchronous = 0,

        /// <summary>
        /// Asynchronous execution mode. The plugin is queued for execution by the Async Service.
        /// The calling code continues without waiting for the plugin to complete.
        /// </summary>
        Asynchronous = 1
    }
}