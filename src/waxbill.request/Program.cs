﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using waxbill.Sessions;
using waxbill.Pools;
using waxbill.Packets;

namespace waxbill.request
{
    class Program
    {
        static void Main(string[] args)
        {
            WaxbillSend();
            Console.ReadKey();
        }


        public static void OriginSend()
        {
            byte[] datas = new byte[1000];
            TcpClient client = new TcpClient();
            client.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.0.162"), 7888));
            
            NetworkStream stream = client.GetStream();
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                while (true)
                {
                    writer.Write(datas);
                    System.Threading.Thread.Sleep(30);
                }
            }
        }


        public static void WaxbillSend()
        {
            byte[] datas = new byte[1000];
            TCPClient client = new TCPClient(waxbill.Protocols.RealtimeProtocol.Define);
            client.OnConnected += new Action<SessionBase>((session) => {
                Console.WriteLine("connected");
            });


            client.OnDisconnected += new Action<SessionBase,CloseReason>((session,reason) => {
                Console.WriteLine("disconnected:"+reason.ToString());
            });


            client.OnSended += new Action<SessionBase,SendingQueue,Boolean>((session,queue,result) => {
                Console.WriteLine("send:"+result.ToString());
            });



            client.OnReceived += new Action<SessionBase,Packet>((session,packet) => {
                Console.WriteLine("receive");
            });


            client.Connect("127.0.0.1", 7888);
            while (true)
            {
                client.Send(datas);
                System.Threading.Thread.Sleep(30);
            }

            

        }
    }
}
