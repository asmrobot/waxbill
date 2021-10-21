using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Sessions;
using waxbill.Utils;

namespace waxbill
{
    public delegate void OnConnectionEvent(Session session);

    public delegate void OnDisconnectedEvent(Session session, CloseReason exception);

    public delegate void OnSendedEvent(Session session, SendingQueue datas, bool result);

    public delegate void OnReceiveEvent(Session session, Packet packet);
}
