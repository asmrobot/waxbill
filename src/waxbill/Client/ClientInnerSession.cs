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
    internal class ClientInnerSession : SessionBase
    {
        private Action<SessionBase> connectedCallback;
        private Action<SessionBase, CloseReason> disconnectedCallback;
        private Action<SessionBase, SendingQueue, Boolean> sendedCallback;
        private Action<SessionBase, Packet> receivedCallback;

        public ClientInnerSession(Action<SessionBase> connectedCallback, Action<SessionBase, CloseReason> disconnectedCallback, Action<SessionBase,SendingQueue, Boolean> sendedCallback, Action<SessionBase,Packet> receivedCallback)
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
