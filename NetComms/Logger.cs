using System;

namespace NetComms
{
    /// <summary>
    /// NetComms log event
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new LogEventArgs
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="data">Associated data</param>
        public LogEventArgs(string message, object data)
        {
            Message = message;
            Data = data;
        }

        /// <summary>
        /// Gets the log message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the associated data
        /// </summary>
        public object Data { get; }
    }

    /// <summary>
    /// NetComms logger
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Log event
        /// </summary>
        public static event EventHandler<LogEventArgs> OnLog;

        /// <summary>
        /// Report a log event
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="data">Associated data</param>
        public static void Log(string message, object data = null)
        {
            OnLog?.Invoke(message, new LogEventArgs(message, data));
        }
    }
}
