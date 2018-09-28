using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Exceptions;
using ZTImage.Libuv;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Sessions
{
    public class ClientSession : SessionBase
    {
        private TCPClient mClient;
        public ClientSession(TCPClient client)
        {
            this.mClient = client;
        }

        protected override void OnConnected()
        {
            try
            {
                this.mClient.RaiseOnConnectionEvent(this);
            }
            catch
            { }
        }

        protected override void OnDisconnected(CloseReason reason)
        {
            try
            {
                this.mClient.RaiseOnDisconnectedEvent(this, null);
                if (mClient != null)
                {
                    mClient.Disconnect();
                }
            }
            catch
            { }
        }

        protected override void OnSended(PlatformBuf packet, bool result)
        {
            try
            {
                this.mClient.RaiseOnSendedEvent(this, packet, result);
            }
            catch (Exception ex)
            {
                Trace.Error("execute sended callback error", ex);

            }
        }

        protected override void OnReceived(Packet packet)
        {
            try
            {
                this.mClient.RaiseOnReceiveEvent(this, packet);
            }
            catch (Exception ex)
            {
                Trace.Error(ex.Message, ex);
            }
        }
        
    }
}
