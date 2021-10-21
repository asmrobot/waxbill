using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Sessions;

namespace waxbill.Client
{
    /// <summary>
    /// TCPClient使用的Session
    /// </summary>
    internal class ClientInnerSession : Session
    {
        private Action<Session> connectedCallback;
        private Action<Session, CloseReason> disconnectedCallback;
        private Action<Session, SendingQueue, Boolean> sendedCallback;
        private Action<Session, Packet> receivedCallback;

        public ClientInnerSession(Action<Session> connectedCallback, Action<Session, CloseReason> disconnectedCallback, Action<Session,SendingQueue, Boolean> sendedCallback, Action<Session,Packet> receivedCallback)
        {
            this.connectedCallback = connectedCallback;
            this.disconnectedCallback = disconnectedCallback;
            this.sendedCallback = sendedCallback;
            this.receivedCallback = receivedCallback;

        }

        protected override void OnConnected()
        {
            if (connectedCallback != null)
            {
                connectedCallback(this);
            }
        }

        protected override void OnDisconnected(CloseReason reason)
        {
            if (disconnectedCallback != null)
            {
                disconnectedCallback(this,reason);
            }
        }

        protected override void OnReceived(Packet packet)
        {
            if (receivedCallback != null)
            {
                receivedCallback(this,packet);
            }
        }

        protected override void OnSended(SendingQueue packet, bool result)
        {
            if (sendedCallback != null)
            {
                sendedCallback(this,packet, result);
            }
        }
    }
}
