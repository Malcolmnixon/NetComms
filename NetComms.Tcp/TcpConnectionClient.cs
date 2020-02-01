using System;
using System.Net;
using System.Net.Sockets;

namespace NetComms.Tcp
{
    /// <summary>
    /// TCP client connection class
    /// </summary>
    internal sealed class TcpConnectionClient : TcpConnection
    {
        /// <summary>
        /// Server port to connect to
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the TcpConnectionClient class
        /// </summary>
        /// <param name="address">Server address</param>
        /// <param name="port">Server port</param>
        public TcpConnectionClient(IPAddress address, int port) : base(address, 0x10000000)
        {
            Logger.Log($"TcpConnectionClient.TcpConnectionClient({address}, {port})");

            _port = port;
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        public override void Start()
        {
            Logger.Log("TcpConnectionClient.Start - starting connection");


            // Fail if already connected
            if (Socket != null)
                throw new InvalidOperationException("Already connected");

            try
            {
                // Create the TCP socket
                Socket = new Socket(Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the server
                Socket.Connect(Address, _port);

                // Call the base to start processing
                base.Start();
            }
            catch
            {
                // Dispose of the socket
                Socket?.Dispose();
                Socket = null;

                // Rethrow the exception
                throw;
            }
        }
    }
}
