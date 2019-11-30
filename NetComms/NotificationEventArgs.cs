using System;

namespace NetComms
{
    /// <summary>
    /// Notification event arguments class
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the NotificationEventArgs class
        /// </summary>
        /// <param name="connection">Associated connection</param>
        /// <param name="notification">Notification data</param>
        public NotificationEventArgs(IConnection connection, byte[] notification)
        {
            Connection = connection;
            Notification = notification;
        }

        /// <summary>
        /// Gets the associated connection
        /// </summary>
        public IConnection Connection { get; }

        /// <summary>
        /// Gets the notification
        /// </summary>
        public byte[] Notification { get; }
    }
}
