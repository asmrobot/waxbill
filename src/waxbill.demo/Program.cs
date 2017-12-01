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
        static void Main(string[] args)
        {
            loop = new UVLoopHandle();
            loop.Init();

            UVTCPHandle tcp = new UVTCPHandle();
            tcp.Init(loop);

            try
            {
                tcp.Bind("0.0.0.0", 12308);
            }
            catch(UVException ex)
            {
                throw ex;
            }

            tcp.Listen(50,(stream,status,ex,state)=> {
                UVTCPHandle client = new UVTCPHandle();
                client.Init(loop);
                
                try
                {
                    stream.Accept(client);

                    Console.WriteLine("远程地址为："+client.RemoteEndPoint.ToString());
                }
                catch
                {
                    Console.WriteLine("accept error");
                    client.Dispose();
                }
                
                

                
            },loop);




            loop.Start();
            Console.WriteLine("ok");
            Console.ReadKey();

        }
    }
}
