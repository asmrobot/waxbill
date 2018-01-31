using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Sessions;
using waxbill.Utils;

namespace waxbill
{
    public delegate void OnConnectionEvent(SessionBase session);

    public delegate void OnDisconnectedEvent(SessionBase session, CloseReason reason);

    public delegate void OnSendedEvent(SessionBase session, IList<UVIntrop.uv_buf_t> packet, bool result);

    public delegate void OnReceiveEvent(SessionBase session, Packet collection);
}
