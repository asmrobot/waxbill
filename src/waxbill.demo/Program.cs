using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Exceptions;
using waxbill.Libuv;
using waxbill.Libuv.Collections;
using waxbill.Protocols;
using System.Threading;
using System.Diagnostics;

namespace waxbill.demo
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
    class Program
    {
        
        static void Main(string[] args)
        {
            Trace.SetTrace(new MListener());
            TCPServer<MServerSession> server = new TCPServer<MServerSession>(new RealtimeProtocol());
            server.Start("0.0.0.0", 2233);

            Console.WriteLine("server is start");
            Console.ReadKey();
        }
        

    }
}
