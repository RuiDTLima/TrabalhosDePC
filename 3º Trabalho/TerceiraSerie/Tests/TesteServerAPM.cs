using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerAPM;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Tests {
    [TestClass]
    public class TesteServerAPM {
        [TestMethod]
        public void TestGET() {
            Logger log = new Logger();
            new Listener(log).Run();
            TcpListener server = new TcpListener(IPAddress.Loopback, 8080);
            server.Start();

            TcpClient socket = server.AcceptTcpClient();
            Stream stream = socket.GetStream();

            Handler handler = new Handler(stream, log);
            handler.Run("SET first 1");
            handler.Run("GET first");
            byte[] response = new byte[1024];
            int bytesRead = stream.Read(response, 0, response.Length);

            string resp = response.ToString();

            Assert.AreEqual(resp, "1");
        }
    }
}