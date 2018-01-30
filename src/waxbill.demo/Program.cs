﻿using System;
using waxbill.Protocols;
using System.Runtime.InteropServices;
using ZTImage.Log;
using System.Net.Sockets;
using System.Net;

namespace waxbill.demo
{
    
    class Program
    {
        public static byte[] datas = new byte[] { 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };



        static void Main(string[] args)
        {
            Trace.EnableListener(ZTImage.Log.NLog.Instance);

            TCPServer<MServerSession> server = new TCPServer<MServerSession>(new RealtimeProtocol());
            //TCPServer<MServerSession> server = new TCPServer<MServerSession>(new BeginEndMarkProtocol((byte)'{',(byte)'}'));
            //TCPServer<MServerSession> server = new TCPServer<MServerSession>(new ZTProtocol());
            server.Start("0.0.0.0", 2333);

            Trace.Info("server is start");



            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.162"), 2333));
            //socket.Send(datas);

            Console.ReadKey();
        }
        

    }
}
