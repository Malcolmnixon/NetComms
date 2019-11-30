using System;

namespace NetComms
{
    /// <summary>
    /// Network communications server interface
    /// </summary>
    public interface IServer : IDisposable
    {
        /// <summary>
        /// New connection event
        /// </summary>
        event EventHandler<ConnectionEventArgs> NewConnection;

        /// <summary>
        /// Connection dropped event
        /// </summary>
        event EventHandler<ConnectionEventArgs> ConnectionDropped;

        /// <summary>
        /// Notification received event
        /// </summary>
        event EventHandler<NotificationEventArgs> Notification;

        /// <summary>
        /// Transaction received event
        /// </summary>
        event EventHandler<TransactionEventArgs> Transaction;

        /// <summary>
        /// Start the server
        /// </summary>
        void Start();

        /// <summary>
        /// Send a notification over all connections
        /// </summary>
        /// <param name="notification">Notification data</param>
        void SendNotification(byte[] notification);
    }
}
