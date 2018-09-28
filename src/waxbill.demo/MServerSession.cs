using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Sessions;
using waxbill.Utils;
using ZTImage.Log;

namespace waxbill.demo
{
    public class MServerSession : ServerSession
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

            Trace.Info("receive:" +tostring(b));
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

        private unsafe string tostring(PlatformBuf buf)
        {
            byte* b = (byte*)(buf.Buffer);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < buf.Count.ToInt32(); i++)
            {
                builder.Append(b[i] + ",");
            }
            return builder.ToString();
        }

        protected override void OnSended(PlatformBuf packet, bool result)
        {
            Trace.Info("sended:" + tostring(packet));
        }
    }
}
