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

        
        public static byte[] datas = new byte[] { 0x0d,0x0a,0x00,0x00,0x00,0x0f,0x01,0x02,0x03,0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };

        private const Int32 ClientCount = 4;
        private const Int32 SendCount = 10000000;
        

        static void Main(string[] args)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.162"), 2333));
            
            socket.Send(datas);



            //for (int i = 0; i < ClientCount; i++)
            //{
            //    ThreadPool.QueueUserWorkItem((state) => {

            //        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //        socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.162"), 2333));

            //        for (int s = 0; s < SendCount; s++)
            //        {
            //           socket.Send(datas);                        
            //        }
            //        socket.Shutdown(SocketShutdown.Both);
            //        socket.Close();

            //        if (Interlocked.Increment(ref completeCount) >= ClientCount)
            //        {
            //            Console.WriteLine("complete");
            //        }
            //    });
            //}


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
