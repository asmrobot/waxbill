using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill
{
    public delegate void OnConnectionEvent(SocketSession session);

    public delegate void OnDisconnectedEvent(SocketSession session, CloseReason reason);

    public delegate void OnSendedEvent(SocketSession session, IList<UVIntrop.uv_buf_t> packet, bool result);

    public delegate void OnReceiveEvent(SocketSession session, IPacket collection);
}
