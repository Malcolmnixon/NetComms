using System.Net;

namespace NetComms.Udp
{
    /// <summary>
    /// UDP network communications provider
    /// </summary>
    public sealed class UdpProvider : IProvider
    {
        /// <summary>
        /// Initializes a new instance of the UdpProvider class
        /// </summary>
        /// <param name="port">UDP port associated with connections</param>
        public UdpProvider(int port)
        {
            Port = port;
        }

        /// <summary>
        /// Gets the UDP port associated with connections
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets or sets the keep-alive probe count
        /// </summary>
        public int KeepAliveProbes { get; set; } = 5;

        /// <summary>
        /// Gets or sets the keep-alive probe interval
        /// </summary>
        public long KeepAliveInterval { get; set; } = 3000;

        /// <summary>
        /// Create a new client connection
        /// </summary>
        /// <param name="address">Server address</param>
        /// <returns>New connection</returns>
        public IConnection CreateClient(IPAddress address)
        {
            return new UdpConnectionClient(
                new IPEndPoint(address, Port), 
                KeepAliveProbes, 
                KeepAliveInterval);
        }

        /// <summary>
        /// Create a new server
        /// </summary>
        /// <returns>New server</returns>
        public IServer CreateServer()
        {
            return new UdpServer(
                Port, 
                KeepAliveProbes,
                KeepAliveInterval);
        }
    }
}
