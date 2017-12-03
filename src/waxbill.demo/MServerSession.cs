using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill.demo
{
    public class MServerSession : SocketSession
    {
        protected override void ConnectedCallback()
        {
            Console.WriteLine("connection!远程地址为：" + this.TcpHandle.RemoteEndPoint.ToString());
        }

        protected override void DisconnectedCallback(CloseReason reason)
        {
            Console.WriteLine("disconnection");
        }

        protected override void ReceiveCallback(Packet packet)
        {
            Console.WriteLine("receive");
        }

        protected override void SendedCallback(SendingQueue packet, bool result)
        {
            Console.WriteLine("sended");
        }
    }
}
