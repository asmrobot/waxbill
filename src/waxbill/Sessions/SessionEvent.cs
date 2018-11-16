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
    public delegate void OnConnectionEvent(SessionBase session);

    public delegate void OnDisconnectedEvent(SessionBase session, CloseReason exception);

    public delegate void OnSendedEvent(SessionBase session, SendingQueue datas, bool result);

    public delegate void OnReceiveEvent(SessionBase session, Packet packet);
}
