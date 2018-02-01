using System;
using waxbill.Protocols;
using System.Runtime.InteropServices;
using ZTImage.Log;
using System.Net.Sockets;
using System.Net;
using waxbill.Sessions;
using waxbill.Libuv;

namespace waxbill.demo
{
    
    class Program
    {
        public static byte[] datas = new byte[] { 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };

        static Int32 counter = 0;
        static void wait_for_while(UVIdleHandle handle)
        {
            counter++;
            if (counter % 100000 == 0)
            {
                //counter = 0;
                Console.WriteLine(counter);
            }
            if (counter > 1000000)
            {
                
                handle.Stop();
            }
        }

        static void Main(string[] args)
        {
            Trace.EnableListener(ZTImage.Log.NLog.Instance);

            //UVLoopHandle loop = new UVLoopHandle();
            //UVIdleHandle idle = new UVIdleHandle(loop);

            //idle.Start(wait_for_while);            

            //Trace.Info("entry async start");
            //loop.AsyncStart(null);
            //Trace.Info("entry async end");








            //todo: receive
            TCPServer<MServerSession> server = new TCPServer<MServerSession>(RealtimeProtocol.Define);
            //TCPServer<MServerSession> server = new TCPServer<MServerSession>(new BeginEndMarkProtocol((byte)'{',(byte)'}'));
            //TCPServer<MServerSession> server = new TCPServer<MServerSession>(new ZTProtocol());
            server.Start("0.0.0.0", 2333);
            Trace.Info("server is start");









            ////todo：send
            //for (int i = 0; i < 20; i++)
            //{
            //    TCPClient client = new TCPClient(waxbill.Protocols.RealtimeProtocol.Define);
            //    client.OnConnection += Client_OnConnection;
            //    client.OnDisconnected += Client_OnDisconnected;
            //    client.OnReceive += Client_OnReceive;
            //    client.OnSended += Client_OnSended;
            //    client.Connection("127.0.0.1", 2333);
            //}

            //ZTImage.Log.Trace.Info("run complete");

            Console.ReadKey();
        }

        private static void Client_OnSended(ServerSession session, System.Collections.Generic.IList<Libuv.UVIntrop.uv_buf_t> packet, bool result)
        {
            ZTImage.Log.Trace.Info("Client_OnSended");
        }
        

        private static void Client_OnReceive(ServerSession session, Packets.Packet collection)
        {
            ZTImage.Log.Trace.Info("Client_OnReceive");
        }

        private static void Client_OnDisconnected(ServerSession session, CloseReason reason)
        {
            ZTImage.Log.Trace.Info("Client_OnDisconnected");
        }

        private static void Client_OnConnection(ServerSession session)
        {
            ZTImage.Log.Trace.Info("Client_OnConnection");
            session.Send(System.Text.Encoding.UTF8.GetBytes("abcdefghi"));
        }
    }
}
