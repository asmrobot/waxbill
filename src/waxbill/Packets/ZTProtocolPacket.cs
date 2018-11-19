using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Pools;
using waxbill.Utils;

namespace waxbill.Packets
{
    public class ZTProtocolPacket:Packet
    {
        public ZTProtocolPacket(PacketBufferPool buffer) : base(buffer)
        {}


        public Int32 ContentLength { get; set; }

    }
}
