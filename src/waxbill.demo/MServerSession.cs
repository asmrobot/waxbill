using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Utils;

namespace waxbill.demo
{
    public class MServerSession : SocketSession
    {
        protected override void ConnectedCallback()
        {
            Trace.Debug("connection!远程地址为：" + this.RemoteEndPoint.ToString());
        }

        protected override void DisconnectedCallback(CloseReason reason)
        {
            Trace.Debug("disconnection");
        }

        protected override void ReceiveCallback(Packet packet)
        {
            //byte[] b=packet.Read();
            //Trace.Debug("receive:" + System.Text.Encoding.UTF8.GetString(b));

            //this.Send(b);
        }

        protected override void SendedCallback(IList<UVIntrop.uv_buf_t> packet, bool result)
        {}
    }
}
