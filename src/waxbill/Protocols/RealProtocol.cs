using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill.Protocols
{
    /// <summary>
    /// 收到的全转发
    /// </summary>
    public class RealProtocol:IProtocol
    {
        public bool TryToPacket(ref Packet packet, IntPtr memory, int len, out int readlen)
        {
            readlen = len;
            packet.Write(memory, len);
            return true;

        }
    }
}
