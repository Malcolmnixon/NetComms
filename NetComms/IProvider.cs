using System.Net;

namespace NetComms
{
    /// <summary>
    /// Network communications provider
    /// </summary>
    public interface IProvider
    {
        /// <summary>
        /// Create a client connection (not connected yet)
        /// </summary>
        /// <param name="address">Server address</param>
        /// <returns>New connection</returns>
        IConnection CreateClient(IPAddress address);

        /// <summary>
        /// Create communications server
        /// </summary>
        /// <returns>New server</returns>
        IServer CreateServer();
    }
}
