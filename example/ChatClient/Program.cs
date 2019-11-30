using System;
using System.Net;
using System.Text;
using NetComms.Tcp;

namespace ChatClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Select address
            var address = args.Length > 0 ? args[0] : "127.0.0.1";

            // Create a TCP communications provider
            var provider = new TcpProvider(52341);

            // Create the connection
            var connection = provider.CreateClient(IPAddress.Parse(address));

            // Hook connection dropped
            connection.ConnectionDropped += (s, e) =>
            {
                Console.WriteLine("Connection Dropped");
            };

            // Hook transactions
            connection.Transaction += (s, e) =>
            {
                e.Transaction.SendResponse(new byte[0]);
            };

            // Hook notifications
            connection.Notification += (s, e) =>
            {
                var text = Encoding.ASCII.GetString(e.Notification);
                var message = $"{text}";
                Console.WriteLine(message);
            };

            // Start the connection
            connection.Start();

            // Loop sending notifications
            for (;;)
            {
                // Get the next line
                var message = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(message))
                    continue;

                // Break on quit
                if (message == "quit")
                    break;

                // Send the notification
                var messageBytes = Encoding.ASCII.GetBytes(message);
                connection.SendNotification(messageBytes);
            }
        }
    }
}
