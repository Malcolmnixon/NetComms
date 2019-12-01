using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetComms.Tcp.Test
{
    [TestClass]
    public class LoopBackTest
    {
        [TestMethod]
        public void TestClientNotification()
        {
            // Create server and client
            var provider = new TcpProvider(41249);
            using var server = provider.CreateServer();
            using var client = provider.CreateClient(IPAddress.Loopback);

            // Bind server to record client notifications
            var notifications = new List<byte[]>();
            server.Notification += (s, e) => { notifications.Add(e.Notification); };

            // Start server and client
            server.Start();
            client.Start();

            // Sleep 1 second for server to accept client
            Thread.Sleep(1000);

            // Send three messages to server
            client.SendNotification(new byte[] {1, 2, 3});
            client.SendNotification(new byte[] {4, 5, 6});
            client.SendNotification(new byte[] {7, 8, 9});

            // Sleep 1 second for messages to arrive
            Thread.Sleep(1000);

            // Shut down server and client
            server.Dispose();
            client.Dispose();

            Assert.AreEqual(notifications.Count, 3);
            Assert.IsTrue(notifications[0].SequenceEqual(new byte[] {1, 2, 3}));
            Assert.IsTrue(notifications[1].SequenceEqual(new byte[] {4, 5, 6}));
            Assert.IsTrue(notifications[2].SequenceEqual(new byte[] {7, 8, 9}));
        }

        [TestMethod]
        public void TestServerNotification()
        {
            // Create server and client
            var provider = new TcpProvider(41249);
            using var server = provider.CreateServer();
            using var client = provider.CreateClient(IPAddress.Loopback);

            // Bind client to record server notifications
            var notifications = new List<byte[]>();
            client.Notification += (s, e) => { notifications.Add(e.Notification); };

            // Start server and client
            server.Start();
            client.Start();

            // Sleep 1 second for server to accept client
            Thread.Sleep(1000);

            // Send three messages from server
            server.SendNotification(new byte[] { 1, 2, 3 });
            server.SendNotification(new byte[] { 4, 5, 6 });
            server.SendNotification(new byte[] { 7, 8, 9 });

            // Sleep 1 second for messages to arrive
            Thread.Sleep(1000);

            // Shut down server and client
            server.Dispose();
            client.Dispose();

            Assert.AreEqual(notifications.Count, 3);
            Assert.IsTrue(notifications[0].SequenceEqual(new byte[] { 1, 2, 3 }));
            Assert.IsTrue(notifications[1].SequenceEqual(new byte[] { 4, 5, 6 }));
            Assert.IsTrue(notifications[2].SequenceEqual(new byte[] { 7, 8, 9 }));
        }
    }
}
