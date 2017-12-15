using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Protocols;

namespace waxbill.clientdemo
{
    class Program
    {
        public static Int32 completeCount = 0;

        public static byte[] datas = System.Text.Encoding.UTF8.GetBytes("thisistfdsafdsa45f6dsa45f6ds4a56fd4sa56f4d5s6a4f56dsa45f6ds4a56fd4sa56f4d5sa64fd5s6a4f56dsa4estr");

        private const Int32 ClientCount = 4;
        private const Int32 SendCount = 10000000;
        

        static void Main(string[] args)
        {

            for (int i = 0; i < ClientCount; i++)
            {
                ThreadPool.QueueUserWorkItem((state) => {

                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.162"), 12308));

                    for (int s = 0; s < SendCount; s++)
                    {
                       socket.Send(datas);                        
                    }
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();

                    if (Interlocked.Increment(ref completeCount) >= ClientCount)
                    {
                        Console.WriteLine("complete");
                    }
                });
            }
            
            
            new System.Threading.ManualResetEvent(false).WaitOne();
        }

        
        
        private static void Client_OnReceive(SocketSession session, Packet collection)
        {
            session.Send(datas);
        }

        
        private static void Client_OnDisconnected(SocketSession session, CloseReason reason)
        {
            Console.WriteLine("disconnected"+Interlocked.Decrement(ref connectionid));
        }


        static Int32 connectionid = 0;
        private static void Client_OnConnection(SocketSession session)
        {
            Console.WriteLine("connection:"+ Interlocked.Increment(ref connectionid));
            session.Send(datas);
        }
    }
}
