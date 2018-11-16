namespace ZTImage.Net
{
    using System;
    using System.Runtime.CompilerServices;
    using ZTImage.Net.Utils;

    public delegate void OnSendedEvent(SocketSession session, SendingQueue packet, bool result);
}

