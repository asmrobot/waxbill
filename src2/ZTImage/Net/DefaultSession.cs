namespace ZTImage.Net
{
    using System;
    using ZTImage.Net.Utils;

    public class DefaultSession : SocketSession
    {
        protected override void ConnectedCallback()
        {
        }

        protected override void DisconnectedCallback(CloseReason reason)
        {
        }

        protected override void ReceiveCallback(Packet packet)
        {
        }

        protected override void SendedCallback(SendingQueue packet, bool result)
        {
        }
    }
}

