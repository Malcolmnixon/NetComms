using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NetComms.Udp
{
    /// <summary>
    /// UDP connection class
    /// </summary>
    internal abstract class UdpConnection : IConnection
    {
        /// <summary>
        /// Remote end-point
        /// </summary>
        private readonly IPEndPoint _endPoint;

        /// <summary>
        /// Keep-alive probes
        /// </summary>
        private readonly int _probes;

        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object _lock = new object();

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
        /// Count of missed probes
        /// </summary>
        private int _probesMissed;

        /// <summary>
        /// Initializes a new instance of the TcpConnection class
        /// </summary>
        /// <param name="endPoint">Remote end-point</param>
        /// <param name="probes">Count of keep-alive probes</param>
        /// <param name="tagBase">Base of transaction tags</param>
        protected UdpConnection(IPEndPoint endPoint, int probes, int tagBase)
        {
            // Save the end-point
            _endPoint = endPoint;
            _probes = probes;

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
        public IPAddress Address => _endPoint.Address;

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

        public abstract void Dispose();

        public void SendPing()
        {
            // Send a zero-byte packet
            Socket.SendTo(new byte[0], _endPoint);
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
            Logger.Log($"UdpConnection.SendNotification - sending {packetData.Length} to {_endPoint}");
            Socket.SendTo(packetData, _endPoint);
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
            Socket.SendTo(packetData, _endPoint);
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
            Socket.SendTo(packetData, _endPoint);
        }

        internal bool ProcessProbeTick()
        {
            // Detect connection dropped by missed probes
            if (_probesMissed++ > _probes)
            {
                // Report connection dropped
                ConnectionDropped?.Invoke(this, new ConnectionEventArgs(this));
                return true;
            }

            // Send a ping
            SendPing();
            return false;
        }

        internal void ProcessPacket(byte[] packet, int packetLen)
        {
            Logger.Log($"UdpConnection.ProcessPacket({packetLen})");

            // Any incoming packet clears the probes
            _probesMissed = 0;

            // Decode the length
            var len = BitConverter.ToInt32(packet, 0);
            if (len != packetLen - 8)
                return;

            // Decode the tag
            var tag = BitConverter.ToInt32(packet, 4);

            // Copy the message to its own buffer
            var message = new byte[len];
            Buffer.BlockCopy(packet, 8, message, 0, len);

            // Handle notifications
            if (tag == 0)
            {
                Notification?.Invoke(this, new NotificationEventArgs(this, message));
                return;
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
                return;
            }

            // Must be a remote transaction
            Transaction?.Invoke(
                this,
                new TransactionEventArgs(
                    this,
                    new UdpTransaction(this, tag, message)));
        }

        public abstract void Start();
    }
}
