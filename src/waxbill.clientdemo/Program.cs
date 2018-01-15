using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Protocols;

namespace waxbill.clientdemo
{
    class Program
    {
        public static Int32 completeCount = 0;

        
        public static byte[] datas = new byte[] { 0x0d,0x0a,0x00,0x00,0x00,0x0f,0x01,0x02,0x03,0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x0d, 0x0a, 0x00, 0x00, 0x00, 0x0f, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };

        private const Int32 ClientCount = 4;
        private const Int32 SendCount = 10000000;
        

        static void Main(string[] args)
        {
            waxbill.TCPClient client = new TCPClient(new waxbill.Protocols.RealtimeProtocol());
            client.OnConnection += Client_OnConnection1;
            client.OnDisconnected += Client_OnDisconnected1;
            client.OnSended += Client_OnSended;
            client.OnReceive += Client_OnReceive1;
            client.Connection("47.95.5.5", 80);
            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.162"), 2333));
            
            //socket.Send(datas);

            



            //for (int i = 0; i < ClientCount; i++)
            //{
            //    ThreadPool.QueueUserWorkItem((state) => {

            //        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //        socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.0.162"), 2333));

            //        for (int s = 0; s < SendCount; s++)
            //        {
            //           socket.Send(datas);                        
            //        }
            //        socket.Shutdown(SocketShutdown.Both);
            //        socket.Close();

            //        if (Interlocked.Increment(ref completeCount) >= ClientCount)
            //        {
            //            Console.WriteLine("complete");
            //        }
            //    });
            //}

            Console.WriteLine("complete");
            new System.Threading.ManualResetEvent(false).WaitOne();
        }

        private static void Client_OnReceive1(SocketSession session, Packet collection)
        {
            byte[] data = collection.Read();
            string txt = System.Text.Encoding.UTF8.GetString(data);
            Console.WriteLine("receive"+txt);
        }

        private static void Client_OnSended(SocketSession session, IList<Libuv.UVIntrop.uv_buf_t> packet, bool result)
        {
            Console.WriteLine("sended");
        }

        private static void Client_OnDisconnected1(SocketSession session, CloseReason reason)
        {
            Console.WriteLine("disconnection");
        }

        private static void Client_OnConnection1(SocketSession session)
        {
            string HttpContent = @"GET http://www.cnblogs.com/xing901022/p/8260362.html HTTP/1.1
Host: www.cnblogs.com
Connection: keep-alive
Cache-Control: max-age=0
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36
Upgrade-Insecure-Requests: 1
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
Accept-Encoding: gzip, deflate
Accept-Language: zh-CN,zh;q=0.8
Cookie: pgv_pvi=7221736448; __gads=ID=ae773916ffa37073:T=1500729087:S=ALNI_Mbsqu692v_kvy9ln25GdvQlkTH2iA; UM_distinctid=15d6d1d0f004b-09a9beecd1bdcc-5393662-1fa400-15d6d1d0f01413; CNZZDATA1260386081=1253174744-1500774304-https%253A%252F%252Fwww.cnblogs.com%252F%7C1500774304; CNZZDATA1000228000=1465240195-1501159745-null%7C1501159745; CNZZDATA1261256959=2020531323-1503209328-null%7C1503209328; _pk_id.4.1edd=f457f856751b67b3.1503800628.3.1504180258.1504180258.; CNZZDATA1257151657=1408806613-1504396861-null%7C1504396861; CNZZDATA1259029673=769241473-1504925483-null%7C1504925483; CNZZDATA1254128672=1866741790-1505285260-null%7C1505285260; sc_is_visitor_unique=rx11247317.1505736177.C5704C7A44094FC476A904249117F42F.2.2.2.2.2.2.2.2.2; CNZZDATA1262435696=1103225973-1504185963-https%253A%252F%252Fwww.cnblogs.com%252F%7C1506348086; CNZZDATA1261058399=54743942-1506853614-null%7C1506853614; __utmz=226521935.1509448046.5.4.utmcsr=zzk.cnblogs.com|utmccn=(referral)|utmcmd=referral|utmcct=/s; CNZZDATA1264405927=1458231262-1508151623-null%7C1510668699; CNZZDATA3685059=cnzz_eid%3D1570514254-1511181180-%26ntime%3D1511186620; AJSTAT_ok_times=8; CNZZDATA1259286380=834457718-1503917865-null%7C1512948783; CNZZDATA2364173=cnzz_eid%3D88710487-1502547872-http%253A%252F%252Fwww.cnblogs.com%252F%26ntime%3D1513937829; __utma=226521935.1075941213.1500707916.1509448046.1514093578.6; CNZZDATA1000228226=564337070-1514616297-https%253A%252F%252Fwww.cnblogs.com%252F%7C1514616297; CNZZDATA1271555009=554802623-1514890362-https%253A%252F%252Fwww.cnblogs.com%252F%7C1514890362; CNZZDATA1260206164=498119767-1515108018-https%253A%252F%252Fwww.cnblogs.com%252F%7C1515108018; CNZZDATA1000342940=172427378-1515238053-https%253A%252F%252Fwww.baidu.com%252F%7C1515238053; _gat=1; _ga=GA1.2.564075727.1500687415; _gid=GA1.2.1329213995.1515502250
If-Modified-Since: Wed, 10 Jan 2018 12:03:41 GMT

";
            byte[] HttpData = System.Text.Encoding.UTF8.GetBytes(HttpContent);

            session.Send(HttpData);


            Console.WriteLine("connection");
        }

        private static void Client_OnReceive(SocketSession session, Packet collection)
        {
            session.Send(datas);
        }

        
        private static void Client_OnDisconnected(SocketSession session, CloseReason reason)
        {
            Console.WriteLine("disconnected"+Interlocked.Decrement(ref connectionid));
        }
        
        static Int32 connectionid = 0;
        private static void Client_OnConnection(SocketSession session)
        {
            Console.WriteLine("connection:"+ Interlocked.Increment(ref connectionid));
            session.Send(datas);
        }
    }
}
