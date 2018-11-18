using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Sessions;
using waxbill.Utils;
using ZTImage.Log;
using waxbill.Protocols;
using waxbill.Pools;

namespace waxbill.demo.Tests
{
    public class ByteTest
    {
        public static void Start(Int32 port)
        {
            SocketServer<ByteSession> server = new SocketServer<ByteSession>(new RealtimeProtocol());
            
            server.Start("0.0.0.0", port);
            Trace.Info("server is start");
        }
        private class ByteSession : SessionBase
        {
            protected override void OnConnected()
            {
                Trace.Info("connection!远程地址为：" + this.RemoteEndPoint.ToString());
            }


            protected override void OnDisconnected(CloseReason reason)
            {
                Trace.Info("disconnection");
            }

            protected override void OnReceived(Packet packet)
            {
                byte[] b = packet.Read();

                Trace.Info("receive:" + tostring(b));
                this.Send(b);
            }




            private string tostring(byte[] b)
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < b.Length; i++)
                {
                    builder.Append(b[i] + ",");
                }
                return builder.ToString().TrimEnd(',');
            }

            private unsafe string tostring(SendingQueue buf)
            {
                
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < buf.Count; i++)
                {
                    builder.Append(buf[i] + ",");
                }
                return builder.ToString();
            }

            protected override void OnSended(SendingQueue packet, bool result)
            {
                Trace.Info("sended:" + tostring(packet));
            }
        }
    }
}
