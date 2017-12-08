using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Protocols;

namespace waxbill.clientdemo
{
    class Program
    {
        public class MListener : ITraceListener
        {
            public void Debug(string message)
            {
                Console.WriteLine(message);
            }

            public void Error(string message)
            {
                Console.WriteLine(message);
            }

            public void Error(string message, Exception ex)
            {
                Console.WriteLine(message);
            }

            public void Info(string message)
            {
                Console.WriteLine(message);
            }
        }

        static void Main(string[] args)
        {

            Trace.SetTrace(new MListener());
            TCPClient client = new TCPClient(new RealProtocol());
            client.Connection("127.0.0.1", 12308);
            client.OnConnection += Client_OnConnection;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnReceive += Client_OnReceive;
            
            Console.ReadKey();

        }

        private static void Client_OnReceive(SocketSession session, Utils.Packet collection)
        {
            Console.WriteLine("receive");
        }

        private static void Client_OnDisconnected(SocketSession session, CloseReason reason)
        {
            Console.WriteLine("disconnected");
        }

        private static void Client_OnConnection(SocketSession session)
        {
            Console.WriteLine("connection");
            session.Send(System.Text.Encoding.UTF8.GetBytes("this is testr\r\n"));
        }
    }
}
