using System.Net;

namespace NetComms.Tcp
{
    /// <summary>
    /// TCP network communications provider
    /// </summary>
    public sealed class TcpProvider : IProvider
    {
        /// <summary>
        /// Initializes a new instance of the TcpProvider class
        /// </summary>
        /// <param name="port">TCP port associated with connections</param>
        public TcpProvider(int port)
        {
            Port = port;
        }

        /// <summary>
        /// Gets the TCP port associated with connections
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Create a new client connection
        /// </summary>
        /// <param name="address">Server address</param>
        /// <returns>New connection</returns>
        public IConnection CreateClient(IPAddress address)
        {
            return new TcpConnectionClient(address, Port);
        }

        /// <summary>
        /// Create a new server
        /// </summary>
        /// <returns>New server</returns>
        public IServer CreateServer()
        {
            return new TcpServer(Port);
        }
    }
}
