using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Utils;
using ZTImage.Log;

namespace waxbill.demo
{
    public class MServerSession : SocketSession
    {
        protected override void ConnectedCallback()
        {
            Trace.Info("connection!远程地址为：" + this.RemoteEndPoint.ToString());
        }

        protected override void DisconnectedCallback(CloseReason reason)
        {
            Trace.Info("disconnection");
        }

        protected override void ReceiveCallback(Packet packet)
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

        private unsafe string tostring(UVIntrop.PlatformBuf buf)
        {
            byte* b = (byte*)(buf.Buffer);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < buf.Count.ToInt32(); i++)
            {
                builder.Append(b[i] + ",");
            }
            return builder.ToString();
        }

        protected override void SendedCallback(IList<UVIntrop.uv_buf_t> packet, bool result)
        {
            for (int i = 0; i < packet.Count; i++)
            {
                var buf = packet[i].ToPlatformBuf();
                Trace.Info("sended:" + tostring(buf));
            }
        }
    }
}
