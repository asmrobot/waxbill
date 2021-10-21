using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Protocols;
using waxbill.Sessions;

namespace waxbill.demo.Tests
{
    public class TerminatorTest
    {
        public static void Start(Int32 port)
        {
            TCPServer<TermiatorSession> server = new TCPServer<TermiatorSession>(new TerminatorProtocol());
            server.Start("0.0.0.0", port);
            Trace.Info("server is start");
        }

        private class TermiatorSession : Session
        {
            protected override void OnConnected()
            {
                ZTImage.Log.Trace.Info(this.RemoteEndPoint.ToString()+" is connected");
            }

            protected override void OnDisconnected(CloseReason reason)
            {
                ZTImage.Log.Trace.Info(this.RemoteEndPoint.ToString()+"is disconnected");
            }

            protected override void OnReceived(Packet packet)
            {
                byte[] data = packet.Read();
                ZTImage.Log.Trace.Info("receive:"+System.Text.Encoding.ASCII.GetString(data));
                this.Send(data);
            }

            protected override void OnSended(SendingQueue packet, bool result)
            {
                ZTImage.Log.Trace.Info("send ok");
            }
        }
    }
}
