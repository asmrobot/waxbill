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
        public bool TryToMessage(ref Packet packet, ArraySegment<byte> datas, out int readlen)
        {
            readlen = datas.Count;
            packet.Write(datas);
            return true;
        }
    }
}
