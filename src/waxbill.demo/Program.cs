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
    
    class Program
    {
        private static SegPool pool = new SegPool();
        unsafe static void Main(string[] args)
        {
            TCPServer<MServerSession> server = new TCPServer<MServerSession>(new RealProtocol(), "0.0.0.0", 12308);
            server.Start();

            Console.WriteLine("close!~");
            Console.ReadKey();

            //Thread[] all = new Thread[20];
            //for (int i = 0; i < 20; i++)
            //{
            //    Thread t = new Thread(Inc);
            //    t.Start(null);
            //    all[i] = t;
            //}

            
            //Console.Read();
            return;
        }

        public static void Inc(object state)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < 100000; i++)
            {
                ArraySegment<byte> data;
                if (pool.TryGet(out data))
                {
                    //Console.WriteLine("success:" + pool.IdleCount);
                    pool.Release(data);
                }
            }

            watch.Stop();

            Console.WriteLine("time:" + watch.ElapsedMilliseconds+",idle:"+pool.IdleCount);
            



        }

    }
}
