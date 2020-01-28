using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetComms.Udp
{
    /// <summary>
    /// UDP client connection class
    /// </summary>
    internal sealed class UdpConnectionClient : UdpConnection
    {
        /// <summary>
        /// Server port to connect to
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Cancellation
        /// </summary>
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        /// <summary>
        /// Connection thread
        /// </summary>
        private Thread _thread;

        /// <summary>
        /// Initializes a new instance of the UdpConnectionClient class
        /// </summary>
        /// <param name="endPoint">Server end-point</param>
        public UdpConnectionClient(IPEndPoint endPoint) : base(endPoint, 0x10000000)
        {
            _port = endPoint.Port;
        }

        public override void Dispose()
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


        /// <summary>
        /// Connect to the server
        /// </summary>
        public override void Start()
        {
            // Fail if already connected
            if (Socket != null)
                throw new InvalidOperationException("Already connected");

            try
            {
                // Create the TCP socket
                Socket = new Socket(Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                // Connect to the server
                Socket.Connect(Address, _port);

                // Send a ping
                SendPing();

                // Start processing the connection
                _thread = new Thread(ProcessConnection);
                _thread.Start();
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

        private void ProcessConnection()
        {
            // Loop handling incoming connections
            while (!_cancel.IsCancellationRequested)
            {
                // Wait for incoming connection
                if (!Socket.Poll(1000, SelectMode.SelectRead))
                    continue;

                // Read the next packet
                var packet = new byte[32768];
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0) as EndPoint;
                var packetLen = Socket.ReceiveFrom(packet, ref remoteEndPoint);

                // Process the packet
                ProcessPacket(packet, packetLen);
            }
        }
    }
}
