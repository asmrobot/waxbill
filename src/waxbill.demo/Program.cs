using System;
using waxbill.Protocols;
using System.Runtime.InteropServices;
namespace waxbill.demo
{
    
    class Program
    {
        
        static void Main(string[] args)
        {
            Trace.SetTrace(new MTrace());
            //TCPServer<MServerSession> server = new TCPServer<MServerSession>(new RealtimeProtocol());
            //TCPServer<MServerSession> server = new TCPServer<MServerSession>(new BeginEndMarkProtocol((byte)'{',(byte)'}'));
            TCPServer<MServerSession> server = new TCPServer<MServerSession>(new ZTProtocol());
            server.Start("0.0.0.0", 2333);

            Console.WriteLine("server is start");
            Console.ReadKey();
        }
        

    }
}
