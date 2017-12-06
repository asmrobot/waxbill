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

namespace waxbill.demo
{
    
    class Program
    {
        unsafe static void Main(string[] args)
        {
            TCPServer<MServerSession> server = new TCPServer<MServerSession>(new TerminatorProtocol(),"0.0.0.0", 12308);
            server.Start();
            
            Console.WriteLine("close!~");
            Console.ReadKey();

        }
    }
}
