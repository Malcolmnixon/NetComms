using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetComms.Tcp
{
    /// <summary>
    /// TCP network server class
    /// </summary>
    internal class TcpServer : IServer
    {
        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// TCP port
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Cancellation
        /// </summary>
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// Set of active connections
        /// </summary>
        private readonly HashSet<TcpConnection> _connections = new HashSet<TcpConnection>();

        /// <summary>
        /// Connection thread
        /// </summary>
        private Thread _thread;

        /// <summary>
        /// Listener socket
        /// </summary>
        private Socket _socket;

        /// <summary>
        /// Initializes a new instance of the TcpServer class
        /// </summary>
        /// <param name="port">Server port</param>
        public TcpServer(int port)
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
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true
            };

            // Bind to the port on any address
            _socket.Bind(new IPEndPoint(IPAddress.IPv6Any, _port));

            // Listen with a backlog of 32
            _socket.Listen(32);

            // Start processing the connection
            _thread = new Thread(ProcessServer);
            _thread.Start();
        }

        public void SendNotification(byte[] notification)
        {
            // Get all connections
            IList<TcpConnection> connections;
            lock (_lock)
            {
                connections = _connections.ToList();
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

                // Accept the next connection
                var socket = _socket.Accept();

                // Create the connection
                var connection = new TcpConnectionServer(
                    ((IPEndPoint)socket.RemoteEndPoint).Address,
                    socket);

                // Add to the set of connections
                lock (_lock)
                {
                    _connections.Add(connection);
                }

                // Subscribe to events
                connection.ConnectionDropped += OnConnectionDropped;
                connection.Notification += OnNotification;
                connection.Transaction += OnTransaction;

                // Report the new connection
                NewConnection?.Invoke(this, new ConnectionEventArgs(connection));

                // Start the connection
                connection.Start();
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
