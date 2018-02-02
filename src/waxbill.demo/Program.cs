﻿using System;
using waxbill.Protocols;
using System.Runtime.InteropServices;
using ZTImage.Log;
using System.Net.Sockets;
using System.Net;
using waxbill.Sessions;
using waxbill.Libuv;
using System.Text;

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



            ////todo: receive
            //TCPServer<MServerSession> server = new TCPServer<MServerSession>(RealtimeProtocol.Define);
            ////TCPServer<MServerSession> server = new TCPServer<MServerSession>(new BeginEndMarkProtocol((byte)'{',(byte)'}'));
            ////TCPServer<MServerSession> server = new TCPServer<MServerSession>(new ZTProtocol());
            //server.Start("0.0.0.0", 2333);
            //Trace.Info("server is start");

            //Socket s = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            //s.SendTimeout = 1;
            //s.ReceiveTimeout = 1;
            //s.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2333));
            //s.Send(new byte[20000000]);
            //s.Receive(new byte[200000000], SocketFlags.None);

            //Socket[] clients = new Socket[200];
            //for (int i = 0; i < 200; i++)
            //{
            //    clients[i] = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //    clients[i].Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2333));
            //    Trace.Info("connect,index:" + i.ToString());
            //}


            //for (int i = 0; i < 200; i++)
            //{
            //    System.Threading.ThreadPool.QueueUserWorkItem((l) =>
            //    {
            //        Int32 index = (Int32)l;

            //        byte[] recs = new byte[1024];
            //        clients[index].Send(System.Text.Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyz"));
            //        Trace.Info("send,index:" + index);
            //        clients[index].Receive(recs);
            //        Trace.Info("receive,index:" + index);
            //    }, i);

            //}


            //todo：send
            //for (int i = 0; i < 200; i++)
            //{
            //    TCPClient client = new TCPClient(waxbill.Protocols.RealtimeProtocol.Define);
            //    client.OnConnection += Client_OnConnection; ;
            //    client.OnDisconnected += Client_OnDisconnected;
            //    client.OnReceive += Client_OnReceive;
            //    client.OnSended += Client_OnSended;
            //    client.Connection("127.0.0.1", 2333);
            //}


            for (int i = 0; i < 200; i++)
            {
                try
                {
                    TCPClient.SendOnly("127.0.0.1", 2333, datas, 0, datas.Length);
                    Trace.Info(i.ToString() + ":send ok!~");
                }
                catch (Exception ex)
                {
                    Trace.Error(ex.Message);
                }
            }


            //System.Threading.ManualResetEvent mre = new System.Threading.ManualResetEvent(false);
            //TCPClient client = new TCPClient();
            //client.OnConnection += (c, session) =>
            //{
            //    session.Send(System.Text.Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyz"));
            //};
            //client.OnDisconnected += (c, session, res) =>
            //{
            //    Trace.Info("disconnected");
            //    mre.Set();
            //};
            //client.OnReceive += (c, s, p) =>
            //{
            //    Trace.Info("receive");
            //    mre.Set();
            //};

            //client.Connection("127.0.0.1", 2333);
            //mre.WaitOne();

            //client.Disconnect();

            //client.Connection("127.0.0.1", 2333);
            //mre.WaitOne();


            Trace.Info("run complete,"+datas.Length);
            Console.ReadKey();
        }


        private static string tostring(byte[] b)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                builder.Append(b[i] + ",");
            }
            return builder.ToString().TrimEnd(',');
        }

        private unsafe static string tostring(UVIntrop.PlatformBuf buf)
        {
            byte* b = (byte*)(buf.Buffer);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < buf.Count.ToInt32(); i++)
            {
                builder.Append(b[i] + ",");
            }
            return builder.ToString();
        }

        private static void Client_OnSended(TCPClient client,SessionBase session, System.Collections.Generic.IList<UVIntrop.uv_buf_t> packet, bool result)
        {
            ZTImage.Log.Trace.Info("Client_OnSended,connectid:"+session.ConnectionID.ToString());
        }


        private static void Client_OnReceive(TCPClient client, SessionBase session, Packets.Packet collection)
        {
            ZTImage.Log.Trace.Info("Client_OnReceive,sessionid:"+session.ConnectionID);
            //session.Send(System.Text.Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyz"));
            //session.Close(CloseReason.Shutdown);
        }

        private static void Client_OnDisconnected(TCPClient client, SessionBase session, Exception ex)
        {
            ZTImage.Log.Trace.Info("Client_OnDisconnected");
        }

        private static void Client_OnConnection(TCPClient client, SessionBase session)
        {
            ZTImage.Log.Trace.Info("Client_OnConnection,connectionid:"+session.ConnectionID.ToString());
            session.Send(System.Text.Encoding.UTF8.GetBytes("abcdefghijklmnopqrstuvwxyz"));
        }
        
    }
}
