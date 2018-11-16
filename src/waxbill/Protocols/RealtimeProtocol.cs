using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Utils;

namespace waxbill.Protocols
{
    /// <summary>
    /// 收到的全转发
    /// </summary>
    public class RealtimeProtocol:IProtocol
    {
        public static readonly RealtimeProtocol Define = new RealtimeProtocol();

        public Packet CreatePacket(BufferManager buffer)
        {
            return new Packet(buffer);
        }

        public bool TryToPacket(Packet packet, ArraySegment<byte> datas,  out int giveupCount)
        {
            giveupCount = datas.Count;
            if (giveupCount <= 0)
            {
                return true;
            }
            
            packet.Write(datas);
            return true;
        }


    }
}
