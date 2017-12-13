using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;

namespace waxbill.Protocols
{
    public class HttpProtocol : IProtocol
    {
        public bool TryToPacket(ref Packet packet, IntPtr datas, int count, out int giveupCount)
        {
            throw new NotImplementedException();
        }
    }
}
