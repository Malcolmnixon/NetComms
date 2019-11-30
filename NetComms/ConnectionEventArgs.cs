using System;

namespace NetComms
{
    /// <summary>
    /// Network connection event arguments
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the ConnectionEventArgs class
        /// </summary>
        /// <param name="connection">Connection</param>
        public ConnectionEventArgs(IConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Gets the connection
        /// </summary>
        public IConnection Connection { get; }
    }
}
