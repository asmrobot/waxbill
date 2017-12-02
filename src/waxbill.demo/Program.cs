using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Exceptions;
using waxbill.Libuv;
using waxbill.Libuv.Collections;

namespace waxbill.demo
{
    
    class Program
    {
        
        
        static UVLoopHandle loop;
        unsafe static void Main(string[] args)
        {


            //byte[] b = new byte[1024];
            //GCHandle handle = GCHandle.Alloc(b, GCHandleType.Pinned);
            //IntPtr ptr = handle.AddrOfPinnedObject();

            //byte[] data = System.Text.Encoding.UTF8.GetBytes("ABCabcdefghijklmnopq");
            //Buffer.BlockCopy(data, 0, b, 0, data.Length);


            //for (int i = 0; i < 15; i++)
            //{
            //    Console.WriteLine(*((byte*)ptr + i));
            //}

            loop = new UVLoopHandle();
            loop.Init();

            UVTCPHandle tcp = new UVTCPHandle();
            tcp.Init(loop);

            try
            {
                tcp.Bind("0.0.0.0", 12308);
            }
            catch (UVException ex)
            {
                throw ex;
            }

            tcp.Listen(50, (stream, status, ex, state) =>
            {
                UVTCPHandle client = new UVTCPHandle();
                client.Init(loop);

                try
                {
                    stream.Accept(client);

                    Console.WriteLine("远程地址为：" + client.RemoteEndPoint.ToString());

                    client.ReadStart();
                }
                catch
                {
                    Console.WriteLine("accept error");
                    client.Dispose();
                }


            }, loop);




            loop.Start();
            Console.WriteLine("ok");
            Console.ReadKey();

        }
    }
}
