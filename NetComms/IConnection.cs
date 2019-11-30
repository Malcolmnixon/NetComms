using System;
using System.Net;

namespace NetComms
{
    /// <summary>
    /// Network connection
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// User data to associate with this connection
        /// </summary>
        object AssociatedData { get; set; }

        /// <summary>
        /// Gets the address of this connection
        /// </summary>
        IPAddress Address { get; }

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
        /// Send a notification over the connection
        /// </summary>
        /// <param name="notification">Notification data</param>
        void SendNotification(byte[] notification);

        /// <summary>
        /// Send a transaction
        /// </summary>
        /// <param name="command">Transaction command</param>
        /// <param name="handler">Handler for transaction response</param>
        void SendTransaction(byte[] command, Action<byte[]> handler);

        /// <summary>
        /// Start the connection
        /// </summary>
        void Start();
    }
}
