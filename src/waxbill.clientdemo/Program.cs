using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            TCPClient[] client = new TCPClient[50];
            for (int i = 0; i < 50; i++)
            {
                client[i]=new TCPClient(new RealProtocol());
                client[i].Connection("127.0.0.1", 12308);
                client[i].OnConnection += Client_OnConnection;
                client[i].OnDisconnected += Client_OnDisconnected;
                client[i].OnReceive += Client_OnReceive;
            }
            


            Thread.Sleep(15000);
            for (int i = 0; i < 50; i++)
            {
                client[i].Disconnect();
            }
            
            Console.ReadKey();

        }

        public static byte[] datas = System.Text.Encoding.UTF8.GetBytes("this is tfdsafdsa45f6dsa45f6ds4a56fd4sa56f4d5s6a4f56dsa45f6ds4a56fd4sa56f4d5sa64fd5s6a4f56dsa4estr");
        private static void Client_OnReceive(SocketSession session, Utils.Packet collection)
        {
            session.Send(datas);
        }

        private static void Client_OnDisconnected(SocketSession session, CloseReason reason)
        {
            Console.WriteLine("disconnected");
        }

        private static void Client_OnConnection(SocketSession session)
        {
            Console.WriteLine("connection");
            session.Send(datas);
        }
    }
}
