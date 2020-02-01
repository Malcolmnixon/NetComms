using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetComms.Tcp
{
    /// <summary>
    /// TCP connection class
    /// </summary>
    internal abstract class TcpConnection : IConnection
    {
        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Cancellation
        /// </summary>
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// Rotating queue of tags
        /// </summary>
        private readonly Queue<int> _tagQueue = new Queue<int>();
        
        /// <summary>
        /// Dictionary of transaction callbacks
        /// </summary>
        private readonly Dictionary<int, Action<byte[]>> _transactions = new Dictionary<int, Action<byte[]>>();

        /// <summary>
        /// Next tag to allocate
        /// </summary>
        private int _tagNext;

        /// <summary>
        /// Connection thread
        /// </summary>
        private Thread _thread;

        /// <summary>
        /// Initializes a new instance of the TcpConnection class
        /// </summary>
        /// <param name="address">Associated address</param>
        /// <param name="tagBase">Base of transaction tags</param>
        protected TcpConnection(IPAddress address, int tagBase)
        {
            Address = address;

            // Populate 10 rotating transaction tags (more will be allocated as needed)
            _tagNext = tagBase;
            for (var i = 0; i < 10; ++i)
                _tagQueue.Enqueue(_tagNext++);
        }

        /// <summary>
        /// Gets the associated socket
        /// </summary>
        public Socket Socket { get; protected set; }

        /// <summary>
        /// Gets or sets the associated data
        /// </summary>
        public object AssociatedData { get; set; }

        /// <summary>
        /// Gets the associated address
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// Connection dropped event
        /// </summary>
        public event EventHandler<ConnectionEventArgs> ConnectionDropped;

        /// <summary>
        /// Notification received event
        /// </summary>
        public event EventHandler<NotificationEventArgs> Notification;

        /// <summary>
        /// Transaction received event
        /// </summary>
        public event EventHandler<TransactionEventArgs> Transaction;

        public void Dispose()
        {
            // Request cancellation
            _cancel.Cancel();

            // Join the thread
            _thread?.Join();
            _thread = null;

            // Drop the connection
            Socket?.Dispose();
            Socket = null;
        }

        public void SendNotification(byte[] notification)
        {
            // Allocate the packet
            var packet = new List<byte>(8 + notification.Length);

            // Add the length
            packet.AddRange(BitConverter.GetBytes(notification.Length));

            // Add the transaction number (0 = none/notification)
            packet.AddRange(BitConverter.GetBytes(0));

            // Add the data
            packet.AddRange(notification);

            // Get the packet data
            var packetData = packet.ToArray();

            // Send the packet
            Logger.Log($"TcpConnection.SendNotification - sending {packetData.Length}");
            Socket.Send(packetData);
        }

        public void SendTransaction(byte[] command, Action<byte[]> handler)
        {
            // Create the transaction
            int tag;
            lock (_lock)
            {
                // Allocate the tag
                tag = _tagQueue.Count == 0 ? _tagNext++ : _tagQueue.Dequeue();

                // Save the response handler
                _transactions[tag] = handler;
            }

            // Allocate the packet
            var packet = new List<byte>(8 + command.Length);

            // Add the length
            packet.AddRange(BitConverter.GetBytes(command.Length));

            // Add the transaction number
            packet.AddRange(BitConverter.GetBytes(tag));

            // Add the data
            packet.AddRange(command);

            // Get the packet data
            var packetData = packet.ToArray();

            // Send the packet
            Socket.Send(packetData);
        }

        public void SendResponse(int tag, byte[] response)
        {
            // Allocate the packet
            var packet = new List<byte>(8 + response.Length);

            // Add the length
            packet.AddRange(BitConverter.GetBytes(response.Length));

            // Add the transaction number
            packet.AddRange(BitConverter.GetBytes(tag));

            // Add the data
            packet.AddRange(response);

            // Get the packet data
            var packetData = packet.ToArray();

            // Send the packet
            Socket.Send(packetData);
        }

        public virtual void Start()
        {
            // Fail if already started
            if (_thread != null)
                throw new InvalidOperationException("Already started");

            // Start processing the connection
            _thread = new Thread(ProcessConnection);
            _thread.Start();
        }

        private void ProcessConnection()
        {
            try
            {
                // Read buffer
                var buffer = new byte[4];

                // Loop until asked to cancel
                while (!_cancel.IsCancellationRequested)
                {
                    // Poll for incoming data
                    if (!Socket.Poll(1000000, SelectMode.SelectRead))
                        continue;

                    // Read the length
                    if (Socket.Read(buffer, 0, 4) != 4)
                        break;

                    // Decode the length
                    var len = BitConverter.ToInt32(buffer, 0);
                    if (len < 0 || len > 32768)
                        break;

                    // Read the transaction tag
                    if (Socket.Read(buffer, 0, 4) != 4)
                        break;

                    // Decode the tag
                    var tag = BitConverter.ToInt32(buffer, 0);

                    // Read the transaction message
                    var message = new byte[len];
                    if (Socket.Read(message, 0, len) != len)
                        break;

                    // Handle notifications
                    if (tag == 0)
                    {
                        Notification?.Invoke(this, new NotificationEventArgs(this, message));
                        continue;
                    }

                    // Test if it's one of our transactions
                    Action<byte[]> handler;
                    lock (_lock)
                    {
                        if (_transactions.TryGetValue(tag, out handler))
                        {
                            _transactions.Remove(tag);
                            _tagQueue.Enqueue(tag);
                        }
                    }

                    // Invoke the handler for this action
                    if (handler != null)
                    {
                        handler.Invoke(message);
                        continue;
                    }

                    // Must be a remote transaction
                    Transaction?.Invoke(
                        this,
                        new TransactionEventArgs(
                            this,
                            new TcpTransaction(this, tag, message)));
                }
            }
            catch (SocketException ex)
            {
                Logger.Log($"TcpConnection.ProcessConnection - error {ex}", ex);
            }

            // Report connection dropped
            ConnectionDropped?.Invoke(this, new ConnectionEventArgs(this));
        }
    }

    /// <summary>
    /// Socket extension method class
    /// </summary>
    internal static class SocketExtensions
    {
        /// <summary>
        /// Read from the socket
        /// </summary>
        /// <param name="socket">Socket to read from</param>
        /// <param name="buffer">Read buffer</param>
        /// <param name="offset">Read offset</param>
        /// <param name="count">Read count</param>
        /// <returns>Count of bytes received</returns>
        internal static int Read(this Socket socket, byte[] buffer, int offset, int count)
        {
            var pos = 0;
            while (pos < count)
            {
                var nr = socket.Receive(buffer, offset + pos, count - pos, SocketFlags.None);
                if (nr <= 0)
                    break;

                pos += nr;
            }

            return pos;
        }
    }
}
