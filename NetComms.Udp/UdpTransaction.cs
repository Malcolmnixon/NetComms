using System;

namespace NetComms.Udp
{
    internal sealed class UdpTransaction : ITransaction
    {
        /// <summary>
        /// UDP connection associated with this transaction
        /// </summary>
        private readonly UdpConnection _connection;

        /// <summary>
        /// UDP transaction tag
        /// </summary>
        private readonly int _tag;

        /// <summary>
        /// Response sent flag
        /// </summary>
        private bool _sent;

        /// <summary>
        /// Initializes a new instance of the UdpTransaction class
        /// </summary>
        /// <param name="connection">Associated connection</param>
        /// <param name="tag">Transaction tag</param>
        /// <param name="command">Transaction command</param>
        public UdpTransaction(UdpConnection connection, int tag, byte[] command)
        {
            _connection = connection;
            _tag = tag;
            Command = command;
        }

        /// <summary>
        /// Gets the associated connection
        /// </summary>
        public IConnection Connection => _connection;

        /// <summary>
        /// Gets the transaction command
        /// </summary>
        public byte[] Command { get; }

        /// <summary>
        /// Send the transaction response
        /// </summary>
        /// <param name="response"></param>
        public void SendResponse(byte[] response)
        {
            // Fail if response already sent
            if (_sent)
                throw new InvalidOperationException("Response already sent");

            // Send the response
            _sent = true;
            _connection.SendResponse(_tag, response);
        }
    }
}
