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
            Trace.SetMessageWriter(ZTImage.Log.Trace.Error, ZTImage.Log.Trace.Info);
            
            Int32 first0 = GC.CollectionCount(0);
            Int32 first1 = GC.CollectionCount(1);
            Int32 first2 = GC.CollectionCount(2);

            TerminatorTest.Start(7888);
            //ByteTest.Start(7888);
            Int32 last0 = first0;
            Int32 last1 = first1;
            Int32 last2 = first2;
            while (true)
            {
                Console.ReadKey();

                Int32 ol0 = GC.CollectionCount(0);
                Int32 ol1 = GC.CollectionCount(1);
                Int32 ol2 = GC.CollectionCount(2);

                Trace.Info(string.Format("to first---->0代:{0},1代:{1},2代:{2}", ol0 - first0, ol1 - first1, ol2 - first2));
                Trace.Info(string.Format("to last----->0代:{0},1代:{1},2代:{2}", ol0 - last0, ol1 - last1, ol2 - last2));
                last0 = ol0;
                last1 = ol1;
                last2 = ol2;
            }
            
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
