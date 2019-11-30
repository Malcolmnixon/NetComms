namespace NetComms
{
    /// <summary>
    /// Network communications transaction interface
    /// </summary>
    public interface ITransaction
    {
        /// <summary>
        /// Get the connection associated with this transaction
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Get the transaction command
        /// </summary>
        byte[] Command { get; }

        /// <summary>
        /// Send the transaction response
        /// </summary>
        /// <param name="response"></param>
        void SendResponse(byte[] response);
    }
}
