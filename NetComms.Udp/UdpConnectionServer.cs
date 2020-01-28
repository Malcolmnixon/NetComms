using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetComms.Udp
{
    internal sealed class UdpConnectionServer : UdpConnection
    {
        public UdpConnectionServer(IPEndPoint endPoint, Socket socket) : base(endPoint, 0x20000000)
        {
            Socket = socket;
        }

        public override void Dispose()
        {
            // TODO: Consider how to dispose of this connection
        }

        public override void Start()
        {
            // Nothing to do for server connections
        }
    }
}
