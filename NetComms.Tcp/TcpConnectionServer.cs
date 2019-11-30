using System.Net;
using System.Net.Sockets;

namespace NetComms.Tcp
{
    internal sealed class TcpConnectionServer : TcpConnection
    {
        public TcpConnectionServer(IPAddress address, Socket connection) : base(address, 0x20000000)
        {
            Socket = connection;
        }
    }
}
