using System;
using System.Text;
using NetComms.Tcp;

namespace ChatServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a TCP communications provider
            var provider = new TcpProvider(52341);

            // Create the server
            var server = provider.CreateServer();

            // Hook new connections
            server.NewConnection += (s, e) =>
            {
                Console.WriteLine($"New Connection: {e.Connection.Address}");
            };

            // Hook connection dropped
            server.ConnectionDropped += (s, e) =>
            {
                Console.WriteLine($"Connection Dropped: {e.Connection.Address}");
            };

            // Hook transactions
            server.Transaction += (s, e) =>
            {
                e.Transaction.SendResponse(new byte[0]);
            };

            // Hook notifications
            server.Notification += (s, e) =>
            {
                var text = Encoding.ASCII.GetString(e.Notification);
                var message = $"{e.Connection.Address}: {text}";
                var messageBytes = Encoding.ASCII.GetBytes(message);
                Console.WriteLine(message);
                server.SendNotification(messageBytes);
            };

            // Start the server
            server.Start();

            // Wait for a key
            Console.ReadKey();
        }
    }
}
