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
    public delegate void OnConnectionEvent(TCPClient client,SessionBase session);

    public delegate void OnDisconnectedEvent(TCPClient client, SessionBase session, Exception exception);

    public delegate void OnSendedEvent(TCPClient client, SessionBase session, IList<UVIntrop.uv_buf_t> datas, bool result);

    public delegate void OnReceiveEvent(TCPClient client, SessionBase session, Packet packet);
}
