using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetComms.Udp
{
    /// <summary>
    /// UDP network server class
    /// </summary>
    internal class UdpServer : IServer
    {
        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// UDP port
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Cancellation
        /// </summary>
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// Dictionary of active connections
        /// </summary>
        private readonly Dictionary<IPEndPoint, UdpConnectionServer> _connections = new Dictionary<IPEndPoint, UdpConnectionServer>();

        /// <summary>
        /// Connection thread
        /// </summary>
        private Thread _thread;

        /// <summary>
        /// UDP socket
        /// </summary>
        private Socket _socket;

        /// <summary>
        /// Initializes a new instance of the UdpServer class
        /// </summary>
        /// <param name="port">Server port</param>
        public UdpServer(int port)
        {
            _port = port;
        }

        /// <summary>
        /// New connection event
        /// </summary>
        public event EventHandler<ConnectionEventArgs> NewConnection;

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

            _socket?.Dispose();
            _socket = null;
        }

        public void Start()
        {
            // Fail if already started
            if (_thread != null)
                throw new InvalidOperationException("Already started");

            // Construct the socket
            _socket = new Socket(SocketType.Dgram, ProtocolType.Udp)
            {
                DualMode = true,
                ReceiveBufferSize = 262144
            };

            // Bind to the port on any address
            _socket.Bind(new IPEndPoint(IPAddress.IPv6Any, _port));

            // Start processing the socket
            _thread = new Thread(ProcessServer);
            _thread.Start();
        }

        public void SendNotification(byte[] notification)
        {
            // Get all connections
            IList<UdpConnectionServer> connections;
            lock (_lock)
            {
                connections = _connections.Values.ToList();
            }

            // Send to each connection
            foreach (var connection in connections)
            {
                try
                {
                    connection.SendNotification(notification);
                }
                catch (SocketException)
                {
                }
            }
        }

        private void ProcessServer()
        {
            // Loop handling incoming connections
            while (!_cancel.IsCancellationRequested)
            {
                // Wait for incoming connection
                if (!_socket.Poll(1000, SelectMode.SelectRead))
                    continue;

                // Read the next packet
                var packet = new byte[32768];
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0) as EndPoint;
                var packetLen = _socket.ReceiveFrom(packet, ref remoteEndPoint);

                // Get the remote end-point
                var remoteIpEndPoint = (IPEndPoint) remoteEndPoint;

                // Process the connection
                lock (_lock)
                {
                    if (!_connections.TryGetValue(remoteIpEndPoint, out var connection))
                    {
                        // Create and save the new connection
                        connection = new UdpConnectionServer(remoteIpEndPoint, _socket);
                        _connections.Add(remoteIpEndPoint, connection);

                        // Subscribe to events
                        connection.ConnectionDropped += OnConnectionDropped;
                        connection.Notification += OnNotification;
                        connection.Transaction += OnTransaction;

                        // Report the new connection
                        NewConnection?.Invoke(this, new ConnectionEventArgs(connection));
                    }

                    // Have the connection process the received packet
                    connection.ProcessPacket(packet, packetLen);
                }
            }
        }

        private void OnConnectionDropped(object sender, ConnectionEventArgs e)
        {
            e.Connection.ConnectionDropped -= OnConnectionDropped;
            e.Connection.Notification -= OnNotification;
            e.Connection.Transaction -= OnTransaction;
            ConnectionDropped?.Invoke(this, e);
        }

        private void OnNotification(object sender, NotificationEventArgs e)
        {
            Notification?.Invoke(this, e);
        }

        private void OnTransaction(object sender, TransactionEventArgs e)
        {
            Transaction?.Invoke(this, e);
        }
    }
}
