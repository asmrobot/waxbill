using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill
{
    public delegate void OnConnectionEvent(SocketSession session);

    public delegate void OnDisconnectedEvent(SocketSession session, CloseReason reason);

    public delegate void OnSendedEvent(SocketSession session, SendingQueue packet, bool result);

    public delegate void OnReceiveEvent(SocketSession session, Packet collection);
}
