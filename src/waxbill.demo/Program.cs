using System;
using waxbill.Protocols;
using System.Runtime.InteropServices;
using ZTImage.Log;
using System.Net.Sockets;
using System.Net;
using waxbill.Sessions;
using System.Text;
using waxbill.demo.Tests;

namespace waxbill.demo
{
    
    class Program
    {
       

        static void Main(string[] args)
        {
            
            ZTImage.Log.Trace.EnableListener(ZTImage.Log.NLog.Instance);
            TerminatorTest.Start(7888);
            Console.WriteLine("service start");
            
            Console.ReadKey();
        }
        
        
    }
}
