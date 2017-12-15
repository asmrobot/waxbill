using System;
using waxbill.Protocols;

namespace waxbill.demo
{
    
    class Program
    {
        static void Main(string[] args)
        {
            Trace.SetTrace(new MListener());
            TCPServer<MServerSession> server = new TCPServer<MServerSession>(new RealtimeProtocol());
            server.Start("0.0.0.0", 2333);

            Console.WriteLine("server is start");
            Console.ReadKey();
        }
        

    }
}
