using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace waxbill.request
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] datas = new byte[1000];
            TcpClient client = new TcpClient();
            client.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.0.162"), 7888));



            NetworkStream stream=client.GetStream();
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                while (true)
                {
                    writer.Write(datas);
                    System.Threading.Thread.Sleep(30);
                }
                
            }


            Console.WriteLine("write ok");
            Console.ReadKey();
        }
    }
}
