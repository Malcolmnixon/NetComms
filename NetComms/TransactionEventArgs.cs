using System;

namespace NetComms
{
    /// <summary>
    /// Transaction event arguments
    /// </summary>
    public class TransactionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the TransactionEventArgs class
        /// </summary>
        /// <param name="connection">Associated connection</param>
        /// <param name="transaction">Associated transaction</param>
        public TransactionEventArgs(IConnection connection, ITransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        /// <summary>
        /// Gets the associated connection
        /// </summary>
        public IConnection Connection;

        /// <summary>
        /// Gets the transaction
        /// </summary>
        public ITransaction Transaction { get; }
    }
}
