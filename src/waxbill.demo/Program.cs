using System;
using System.Runtime.InteropServices;
using ZTImage.Log;
using System.Net.Sockets;
using System.Net;
using System.Text;
using waxbill.demo.Tests;
using System.Collections.Generic;

namespace waxbill.demo
{
    
    class Program
    {
       

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ZTImage.Log.Trace.EnableListener(ZTImage.Log.NLog.Instance);

            //TerminatorTest.Start(7888);
            ByteTest.Start(7888);

            Console.WriteLine("service start");
            
            Console.ReadKey();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            int i = 0;
            i++;
            Trace.Error("has exception error");
        }
    }
}
