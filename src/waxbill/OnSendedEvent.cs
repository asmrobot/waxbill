using System;
using System.Runtime.CompilerServices;
using ZTImage.Net.Utils;

namespace waxbill
{
    

    public delegate void OnSendedEvent(SocketSession session, SendingQueue packet, bool result);
}

