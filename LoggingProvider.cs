namespace RQLogger
{
    /// <summary>
    /// Logging wrapper.
    /// </summary>
    public static class LoggingProvider
    {
        /// <summary>
        /// Logs a message to debug diagnostics log.
        /// </summary>
        /// <param name="message">Message.</param>
        public static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}